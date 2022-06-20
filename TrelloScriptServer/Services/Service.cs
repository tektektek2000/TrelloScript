using TrelloScriptServer.Services.Command;
using TrelloScriptServer.Services.WorkPlace;

namespace TrelloScriptServer.Services
{
    public abstract class Service
    {
        public abstract CommandResult Start();
        public abstract CommandResult Stop();
        public abstract void Init(WorkPlaceService workPlaceService);
        public abstract bool Status();
        public abstract string getServiceName();
        public abstract CommandResult RunCommand(string command, string[] parameters);
        public abstract List<CommandHelp> Help();
    }

    public class InvalidConfigException : Exception
    {

    }
}
