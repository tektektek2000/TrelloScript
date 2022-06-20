namespace TrelloScriptServer.Services
{
    public abstract class ServiceConfig
    {
        public string type { get; set; }

        public abstract Service InstatitiateService();
    }
}
