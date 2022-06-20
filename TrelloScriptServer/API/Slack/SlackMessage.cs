namespace TrelloScriptServer.API.Slack
{
    public class SlackMessage
    {
        public string type { get; set; }
        public string user { get; set; }
        public string text { get; set; }
        public string ts { get; set; }
    }
}
