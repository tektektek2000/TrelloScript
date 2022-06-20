using System.Diagnostics;
using TrelloScriptServer.API.Slack;
using TrelloScriptServer.API.Trello.API;
using TrelloScriptServer.API.Trello.Model;
using TrelloScriptServer.Services.Command;
using TrelloScriptServer.Services.Trello;
using TrelloScriptServer.Services.WorkPlace;

namespace TrelloScriptServer.Services.Slack
{
    public class SlackService : Service
    {
        SlackConfig _config;
        public SlackConfig config
        {
            get { return _config; }
            set { InitConfig(value); }
        }
        SlackAPI slackAPI;
        Thread updateThread;
        CancellationTokenSource cancellationToken;
        //autoExpiredCardsThings
        DateTime lastSlackUpdate = DateTime.MinValue;
        TimeSpan SlackUpdateInterval = new TimeSpan(24, 0, 0);
        TrelloService trelloService;
        WorkPlaceService workPlace;

        public bool isRunning
        {
            get
            {
                if(updateThread == null)
                {
                    return false;
                }
                return updateThread.IsAlive;
            }
        }

        private void InitConfig(SlackConfig newConfig)
        {
            _config = newConfig;
            slackAPI = new SlackAPI(newConfig.apiConfig);
            if (config.autoExpiredCards && workPlace.getService<TrelloService>() != null)
            {
                trelloService = workPlace.getService<TrelloService>();
                lastSlackUpdate = DateTime.Now;
                SlackUpdateInterval = new TimeSpan(config.autoExpiredCardsRepeatEveryHour,0,0);
                if(DateTime.Now.Hour < config.autoExpiredCardsHour)
                {
                    lastSlackUpdate = lastSlackUpdate - new TimeSpan(1, 0, 0, 0);
                }
                lastSlackUpdate = new DateTime(lastSlackUpdate.Year, lastSlackUpdate.Month, lastSlackUpdate.Day, this.config.autoExpiredCardsHour, 0, 0);
            }
            else if(config.autoExpiredCards)
            {
                throw new InvalidDataException("Feature needs an unpresent service");
            }
        }

        public SlackService(SlackConfig config)
        {
            _config = config;
        }

        string CardToSlackMessage(TrelloCard card, bool printList = false, bool printBoard = false)
        {
            string message = "";
            message += "_" + card.name
                            + "_\n   • URL: " + card.url;
            if (printBoard) { message += "\n   • Board: " + card.parentList.parentBoard.name; }
            if (printList) { message += "\n   • List: " + card.parentList.name; }
            if (card.members.Count >= 1)
            {
                message += "\n   • Members: <@" + getSlackAlias(card.members[0].userName) + ">";
                if (card.members.Count > 1)
                {
                    for (int i = 1; i < card.members.Count; i++)
                    {
                        message += ", <@" + getSlackAlias(card.members[i].userName) + ">";
                    }
                }
            }
            message += "\n   • Due date: "
                + card.due.Value.Year + (card.due.Value.Month < 10 ? "/0" : "/")
                + card.due.Value.Month + (card.due.Value.Day < 10 ? "/0" : "/")
                + card.due.Value.Day + (card.due.Value.Hour < 10 ? " 0" : " ")
                + card.due.Value.Hour + (card.due.Value.Minute < 10 ? ":0" : ":")
                + card.due.Value.Minute + "\n";
            return message;
        }

        public void UpdateAutoExpiredCards()
        {

            Dictionary<TrelloBoard, List<TrelloCard>> autoExpiredCards = new Dictionary<TrelloBoard, List<TrelloCard>>();
            Dictionary<TrelloBoard, List<TrelloCard>> autoSoonToBeExpiredCards = new Dictionary<TrelloBoard, List<TrelloCard>>();
            Dictionary<TrelloMember, List<TrelloCard>> dmCards = new Dictionary<TrelloMember, List<TrelloCard>>();
            trelloService.getExpiredCards(out autoExpiredCards, out autoSoonToBeExpiredCards);
            foreach (TrelloBoard board in autoExpiredCards.Keys)
            {
                List<TrelloCard> expiredCards = autoExpiredCards[board];
                List<TrelloCard> soonToBeExpiredCards = autoSoonToBeExpiredCards[board];
                string message = "*Board - " + board.name + "*\n";
                if (expiredCards.Count > 0)
                {
                    message += "*Expired cards:*\n";
                    foreach (var card in expiredCards)
                    {
                        foreach(TrelloMember trelloMember in card.members)
                        {
                            if (!dmCards.ContainsKey(trelloMember)) { dmCards.Add(trelloMember, new List<TrelloCard>()); }
                            dmCards[trelloMember].Add(card);
                        }
                        message += CardToSlackMessage(card, true, false);
                    }
                    message += "\n";
                }
                if (soonToBeExpiredCards.Count > 0)
                {
                    message += "\n*Soon to be expired cards (Expires in less than 48 hours):*\n";
                    foreach (var card in soonToBeExpiredCards)
                    {
                        message += CardToSlackMessage(card);
                    }
                }
                if (expiredCards.Count == 0 && soonToBeExpiredCards.Count == 0)
                {
                    message += "\nNothing to show! Good Job!\n";
                }
                slackAPI.Message(config.autoExpiredCardsChannel,message);
            }
            foreach(TrelloMember member in dmCards.Keys)
            {
                var cards = dmCards[member];
                string message = "Hi!\nThere are some expired cards assigned to you:\n\n";
                foreach (var card in cards)
                {
                    message += CardToSlackMessage(card, true, true);
                }
                slackAPI.Message(getSlackAlias(member.userName), message);
            }
        }

        private void UpdateCycle(object obj)
        {
            CancellationToken cancellation = (CancellationToken)obj;
            bool isRunning = true;
            ulong id = 1;
            while (isRunning)
            {
                try
                {
                    isRunning = !cancellation.IsCancellationRequested;
                    if (DateTime.Now - lastSlackUpdate >= SlackUpdateInterval)
                    {
                        UpdateAutoExpiredCards();
                        lastSlackUpdate = DateTime.Now;
                        lastSlackUpdate = new DateTime(lastSlackUpdate.Year, lastSlackUpdate.Month, lastSlackUpdate.Day, this.config.autoExpiredCardsHour, 0, 0);
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
                Thread.Sleep(config.sleepTime);
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

        public string getSlackAlias(string trelloName)
        {
            if (config.aliases != null)
            {
                foreach (var it in config.aliases)
                {
                    if (it.trello == trelloName)
                    {
                        return it.slack;
                    }
                }
            }
            return trelloName;
        }

        public bool hasSlackAlias(string trelloName)
        {
            if (config.aliases != null)
            {
                foreach (var it in config.aliases)
                {
                    if (it.trello == trelloName)
                    {
                        return true;
                    }
                }
            }
            return false;
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
            workPlace = workPlaceService;
            InitConfig(_config);
        }

        public override bool Status()
        {
            return isRunning;
        }

        public override string getServiceName()
        {
            return "slack";
        }

        public override CommandResult RunCommand(string command, string[] parameters)
        {
            if (command == "message" && parameters.Length > 1)
            {
                string msg = parameters[0];
                for (int i = 1; i < parameters.Length; i++)
                {
                    msg += " " + parameters[i];
                }
                slackAPI.Message(config.autoExpiredCardsChannel, msg);
                return CommandResult.Success("Success");
            }
            else if (command == "expiredCards")
            {
                if (trelloService == null)
                {
                    return CommandResult.Failure("There is no Trello Service registered");
                }
                else if (!trelloService.Status())
                {
                    return CommandResult.Failure("Trello Service is not running");
                }
                UpdateAutoExpiredCards();
                return CommandResult.Success("Success");
            }
            else
            {
                return CommandResult.Failure("Invalid command");
            }
        }

        public override List<CommandHelp> Help()
        {
            List<CommandHelp> help = new List<CommandHelp>();
            return help;
        }
    }
}
