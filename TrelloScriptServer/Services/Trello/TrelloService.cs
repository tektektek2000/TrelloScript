using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using TrelloScriptServer.API.Slack;
using TrelloScriptServer.API.Trello.API;
using TrelloScriptServer.API.Trello.Model;
using TrelloScriptServer.Services.Command;
using TrelloScriptServer.Services.WorkPlace;

namespace TrelloScriptServer.Services.Trello
{
    class TrelloService : Service
    {
        List<TrelloBoard> targetBoards;
        List<TrelloBoard> slackTargetBoards;
        private TrelloConfig _config;
        public TrelloConfig config 
        {
            get { return _config; }
            set { _config = value; api = new TrelloAPI(this.config.apiConfig); } 
        }
        TrelloAPI api;
        Thread updateThread;
        CancellationTokenSource cancellationToken;
        object refreshLock = new object();

        public bool isRunning
        {
            get
            {
                if (updateThread == null)
                {
                    return false;
                }
                return updateThread.IsAlive;
            }
        }

        public TrelloService(TrelloConfig config)
        {
            this.config = config;
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
            if (config.verbosity == 3)
            {
                Console.WriteLine("Checked " + trelloBoards.Count + " boards, out of which " + targetBoards.Count + " were Script Targets");
            }
            else if (config.verbosity >= 4)
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
                                        meetingLength.hours += float.Parse(captures[3].Value) - short.Parse(captures[1].Value);
                                        meetingLength.minutes += float.Parse(captures[4].Value) - short.Parse(captures[2].Value);
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
                                usedMeetingLength.minutes = (float)Math.Round(usedMeetingLength.minutes / 5.0) * 5;
                                string name = "Használt gyűlés hossza: [" + usedMeetingLength.hours + " óra " + usedMeetingLength.minutes + " perc] "
                                    + "Gyűlés hossza: [" + meetingLength.hours + " óra " + meetingLength.minutes + " perc]";
                                card.name = name;
                                api.updateCards(card);
                                updatedCards++;
                            }
                        }
                    }
                }
                if (config.verbosity >= 3)
                {
                    Console.WriteLine("Checked " + checkedCards + " cards, out of which " + updatedCards + " were script targets and were updated");
                }
            }
        }

        public void getExpiredCards(out Dictionary<TrelloBoard, List<TrelloCard>> expired, out Dictionary<TrelloBoard, List<TrelloCard>> soonToBeExpired)
        {
            expired = new Dictionary<TrelloBoard, List<TrelloCard>>();
            soonToBeExpired = new Dictionary<TrelloBoard, List<TrelloCard>>();
            lock (refreshLock)
            {
                RefreshBoards();
                var now = DateTime.Now;
                var soon = new TimeSpan(48, 0, 0);
                foreach (var board in slackTargetBoards)
                {
                    expired.Add(board, new List<TrelloCard>());
                    soonToBeExpired.Add(board, new List<TrelloCard>());
                    List<TrelloCard> expiredCards = expired[board];
                    List<TrelloCard> soonToBeExpiredCards = soonToBeExpired[board];
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
            }
        }

        private void UpdateCycle(object obj)
        {
            CancellationToken cancellation = (CancellationToken)obj;
            bool isRunning = true;
            Stopwatch stopWatch = new Stopwatch();
            ulong id = 1;
            while (isRunning)
            {
                if (config.verbosity > 1) { Console.WriteLine("--------" + id + "--------"); }
                stopWatch.Start();
                if (config.verbosity > 0) { Console.WriteLine("Update cycle start"); }
                try
                {
                    isRunning = !cancellation.IsCancellationRequested;
                    Update();
                    stopWatch.Stop();
                    if (config.verbosity > 0)
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
                if (config.verbosity > 0) { Console.WriteLine("Sleeping " + config.sleepTime + " ms"); }
                Thread.Sleep(config.sleepTime);
                stopWatch.Reset();
                id++;
            }
        }

        public Thread StartUpdateThread()
        {
            if (updateThread == null || !updateThread.IsAlive)
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

        public override CommandResult Start()
        {
            if (!isRunning)
            {
                StartUpdateThread();
                return CommandResult.Success("Started service");
            }
            return CommandResult.Failure("Service already running");
        }

        public override CommandResult Stop()
        {
            if (isRunning)
            {
                StopThread();
                return CommandResult.Success("Stopped service");
            }
            return CommandResult.Failure("Service isn't running");
        }

        public override void Init(WorkPlaceService workPlaceService)
        {
            
        }

        public override bool Status()
        {
            return isRunning;
        }

        public override string getServiceName()
        {
            return "trello";
        }

        public override CommandResult RunCommand(string command, string[] parameters)
        {
            return CommandResult.Failure("Unknown command");
        }

        public override List<CommandHelp> Help()
        {
            List<CommandHelp> help = new List<CommandHelp>();
            return help;
        }
    }
}
