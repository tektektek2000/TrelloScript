using TrelloScriptServer.API.Slack;

namespace TrelloScriptServer.Services.Slack
{
    public class SlackConfig : ServiceConfig
    {
        public SlackAPIConfig apiConfig { get; set; }
        public int sleepTime { get; set; }
        public bool autoExpiredCards { get; set; }
        public string autoExpiredCardsChannel { get; set; }
        public int autoExpiredCardsHour { get; set; }
        public int autoExpiredCardsRepeatEveryHour { get; set; }
        public bool autoExpiredCardsDMEnabled { get; set; }
        public List<Alias> aliases { get; set; }

        public override Service InstatitiateService()
        {
            return new SlackService(this);
        }
    }
}
