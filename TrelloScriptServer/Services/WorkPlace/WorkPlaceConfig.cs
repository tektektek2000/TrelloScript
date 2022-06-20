using Newtonsoft.Json.Linq;
using TrelloScriptServer.API.Slack;
using TrelloScriptServer.API.Trello.API;
using TrelloScriptServer.API.Trello.Config;
using TrelloScriptServer.Services.Command;

namespace TrelloScriptServer.Services.WorkPlace
{
    public class WorkPlaceConfig
    {
        public string name { get; set; }
        public CommandConfig commandConfig { get; set; }
        public List<ServiceConfig> services { get; set; }

        public T getServiceConfig<T>() where T : ServiceConfig
        {
            foreach(var it in services)
            {
                T service = it as T;
                if(service != null) { return service; }
            }
            return null;
        }
    }
}
