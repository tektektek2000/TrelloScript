using TrelloScriptServer.API.Trello.Config;

namespace TrelloScriptServer.Services.Trello
{
    public class TrelloConfig : ServiceConfig
    {
        public TrelloAPIConfig apiConfig { get; set; }
        public int verbosity { get; set; }
        public int sleepTime { get; set; }

        public override Service InstatitiateService()
        {
            return new TrelloService(this);
        }
    }
}
