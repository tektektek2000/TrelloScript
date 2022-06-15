using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using TrelloScriptServer.API.Trello;
using TrelloScriptServer.Interpreter;

namespace TrelloScriptServer.Interpreter
{
    class TrelloInterpreter
    {
        List<TrelloBoard> targetBoards;
        List<TrelloBoard> slackTargetBoards;
        TrelloAPI api;
        Thread updateThread;
        DateTime lastSlackUpdate = DateTime.MinValue;
        CancellationTokenSource cancellationToken;
        int SleepTimer = 2000;
        object SleepTimerLock = new object();
        int Verbosity = 0;
        object VerbosityLock = new object();
        TimeSpan SlackUpdateInterval = new TimeSpan(48, 0, 0);
        object SlackUpdateIntervalLock = new object();
        object refreshLock = new object();

        public bool isRunning{
            get 
            {
                return updateThread.IsAlive;
            }
        }

        public TrelloInterpreter(TrelloAPI API)
        {
            api = API;
            //RefreshBoards();
        }

        private void RefreshBoards()
        {
            List<TrelloBoard> trelloBoards = api.getBoards();
            targetBoards = new List<TrelloBoard>();
            slackTargetBoards = new List<TrelloBoard>();
            foreach (var it in trelloBoards)
            {
                if (it.desc.Contains("@ScriptTarget") || it.desc.Contains("@SlackTarget"))
                {
                    it.lists = api.getLists(it);
                    foreach (var list in it.lists)
                    {
                        list.cards = api.getCards(list);
                    }
                    if (it.desc.Contains("@ScriptTarget"))
                    {
                        targetBoards.Add(it);
                    }
                    if (it.desc.Contains("@SlackTarget"))
                    {
                        slackTargetBoards.Add(it);
                    }
                }
            }
            if (Verbosity == 3)
            {
                Console.WriteLine("Checked " + trelloBoards.Count + " boards, out of which " + targetBoards.Count + " were Script Targets");
            }
            else if (Verbosity >= 4)
            {
                Console.WriteLine("Checked Boards:");
                foreach (var it in trelloBoards)
                {
                    Console.WriteLine("ID=" + it.id + " BoardName=" + it.name + " desc=" + it.desc);
                }
                Console.WriteLine("Target Boards:");
                foreach (var it in targetBoards)
                {
                    Console.WriteLine("ID=" + it.id + " BoardName=" + it.name + " desc=" + it.desc);
                }
            }
        }

        public void Update()
        {
            lock (refreshLock)
            {
                RefreshBoards();
                int updatedCards = 0;
                int checkedCards = 0;
                foreach (var board in targetBoards)
                {
                    foreach (var list in board.lists)
                    {
                        foreach (var card in list.cards)
                        {
                            checkedCards++;
                            if (card.desc.Contains("@TestScript"))
                            {
                                card.name = "Test succesfull";
                                card.desc = "@SecondTestScript";
                                api.updateCards(card);
                                updatedCards++;
                            }
                            else if (card.desc.Contains("@SecondTestScript"))
                            {
                                card.name = "Test succesfull again";
                                card.desc = "@TestScript";
                                api.updateCards(card);
                                updatedCards++;
                            }
                            else if (card.desc.Contains("@TimeCardHun"))
                            {
                                TimeInterval meetingLength = new TimeInterval(0, 0);
                                TimeInterval usedMeetingLength = new TimeInterval(0, 0);
                                Regex r = new Regex("\\(([0-9]+):([0-9]+)-([0-9]+):([0-9]+)\\)");
                                MatchCollection matches = r.Matches(card.parentList.name);
                                for (int i = 0; i < matches.Count; i++)
                                {
                                    var captures = matches[i].Groups;
                                    if (captures.Count >= 4)
                                    {
                                        meetingLength.hours += float.Parse(captures[3].Value) - Int16.Parse(captures[1].Value);
                                        meetingLength.minutes += float.Parse(captures[4].Value) - Int16.Parse(captures[2].Value);
                                    }
                                }
                                foreach (var it in card.parentList.cards)
                                {
                                    if (it != card)
                                    {
                                        r = new Regex("\\[ *([0-9]+\\.*[0-9]*) *óra *\\]*");
                                        matches = r.Matches(it.name);
                                        for (int i = 0; i < matches.Count; i++)
                                        {
                                            var captures = matches[i].Groups;
                                            if (captures.Count > 1)
                                            {
                                                usedMeetingLength.hours += float.Parse(captures[1].Value);
                                            }
                                        }
                                        r = new Regex("\\[* *([0-9]+\\.*[0-9]*) *perc *\\]");
                                        matches = r.Matches(it.name);
                                        for (int i = 0; i < matches.Count; i++)
                                        {
                                            var captures = matches[i].Groups;
                                            if (captures.Count > 1)
                                            {
                                                usedMeetingLength.minutes += float.Parse(captures[1].Value);
                                            }
                                        }
                                    }
                                }
                                usedMeetingLength *= 1.1f;
                                usedMeetingLength.minutes = ((float)Math.Round(usedMeetingLength.minutes / 5.0)) * 5;
                                string name = "Használt gyűlés hossza: [" + usedMeetingLength.hours + " óra " + usedMeetingLength.minutes + " perc] "
                                    + "Gyűlés hossza: [" + meetingLength.hours + " óra " + meetingLength.minutes + " perc]";
                                card.name = name;
                                api.updateCards(card);
                                updatedCards++;
                            }
                        }
                    }
                }
                if (Verbosity >= 3)
                {
                    Console.WriteLine("Checked " + checkedCards + " cards, out of which " + updatedCards + " were script targets and were updated");
                }
            }
        }

        public void UpdateSlackBot()
        {
            lock (refreshLock)
            {
                RefreshBoards();
                List<TrelloCard> expiredCards = new List<TrelloCard>();
                List<TrelloCard> soonToBeExpiredCards = new List<TrelloCard>();
                var now = DateTime.Now;
                var soon = new TimeSpan(48, 0, 0);
                foreach (var board in slackTargetBoards)
                {
                    foreach (var list in board.lists)
                    {
                        foreach (var card in list.cards)
                        {
                            if (card.due.HasValue && card.dueComplete.HasValue && !card.dueComplete.Value)
                            {
                                if (card.due.Value - now < TimeSpan.Zero)
                                {
                                    expiredCards.Add(card);
                                }
                                else if (card.due.Value - now < soon)
                                {
                                    soonToBeExpiredCards.Add(card);
                                }
                            }
                        }
                    }
                }
                string message = "*_Expired cards:_*\n";
                foreach (var card in expiredCards)
                {
                    message += "\n*" + card.name + "*\n   • URL: " + card.url + "\n   • Expired date: " + card.due.Value.Year + (card.due.Value.Month < 10 ? "/0" : "/")
                        + card.due.Value.Month + (card.due.Value.Day < 10 ? "/0" : "/")
                        + card.due.Value.Day + (card.due.Value.Hour < 10 ? " 0" : " ")
                        + card.due.Value.Hour + (card.due.Value.Minute < 10 ? ":0" : ":")
                        + card.due.Value.Minute;
                }
                message += "\n\n*_Soon to be expired cards (Expires in less than 48 hours):_*\n";
                foreach (var card in soonToBeExpiredCards)
                {
                    message += "\n*" + card.name + "*\n   • URL: " + card.url + "\n   • Expired date: " + card.due.Value.Year + (card.due.Value.Month < 10 ? "/0" : "/")
                        + card.due.Value.Month + (card.due.Value.Day < 10 ? "/0" : "/")
                        + card.due.Value.Day + (card.due.Value.Hour < 10 ? " 0" : " ")
                        + card.due.Value.Hour + (card.due.Value.Minute < 10 ? ":0" : ":")
                        + card.due.Value.Minute;
                }
                SlackBot.Message(message);
            }
        }

        private void UpdateCycle(object obj)
        {
            CancellationToken cancellation = (CancellationToken)obj;
            bool isRunning = true;
            Stopwatch stopWatch = new Stopwatch();
            UInt64 id = 1;
            while (isRunning)
            {
                lock (VerbosityLock)
                {
                    if (Verbosity > 1) { Console.WriteLine("--------" + id + "--------"); }
                    stopWatch.Start();
                    if(Verbosity > 0) { Console.WriteLine("Update cycle start"); }
                    try
                    {
                        isRunning = !cancellation.IsCancellationRequested;
                        Update();
                        if(DateTime.Now - lastSlackUpdate >= SlackUpdateInterval)
                        {
                            lastSlackUpdate = DateTime.Now;
                            UpdateSlackBot();
                        }
                        stopWatch.Stop();
                        if (Verbosity > 0)
                        {
                            Console.WriteLine("Update cycle ended in " + stopWatch.ElapsedMilliseconds + " ms");
                        }
                    }
                    catch (FailedRestRequestException)
                    {
                        Console.WriteLine("Unexpected API failure aborting cycle");
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                        Console.WriteLine(e.StackTrace);
                    }
                    lock (SleepTimerLock)
                    {
                        if (Verbosity > 0) { Console.WriteLine("Sleeping " + SleepTimer + " ms"); }
                        Thread.Sleep(SleepTimer);
                    }
                }
                stopWatch.Reset();
                id++;
            }
        }

        public Thread StartUpdateThread()
        {
            if(updateThread == null || !updateThread.IsAlive)
            {
                cancellationToken = new CancellationTokenSource();
                updateThread = new Thread(new ParameterizedThreadStart(UpdateCycle));
                updateThread.Start(cancellationToken.Token);
            }
            return updateThread;
        }

        public void StopThread()
        {
            if (updateThread.IsAlive)
            {
                cancellationToken.Cancel();
            }
            updateThread.Join();
        }

        public void setSleep(int newSleep)
        {
            lock (SleepTimerLock)
            {
                SleepTimer = newSleep;
            }
        }

        public int getSleep()
        {
            int ret;
            lock (SleepTimerLock)
            {
                ret = SleepTimer;
            }
            return ret;
        }

        public void setSlackUpdateInterval(TimeSpan newInterval)
        {
            lock (SlackUpdateIntervalLock)
            {
                SlackUpdateInterval = newInterval;
            }
        }

        public TimeSpan getSlackUpdateInterval()
        {
            TimeSpan ret;
            lock (SlackUpdateIntervalLock)
            {
                ret = SlackUpdateInterval;
            }
            return ret;
        }

        public void setVerbosity(int newVerbosity)
        {
            lock (VerbosityLock)
            {
                Verbosity = newVerbosity;
            }
        }
    }
}
