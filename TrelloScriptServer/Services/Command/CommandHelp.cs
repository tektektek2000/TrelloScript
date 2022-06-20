namespace TrelloScriptServer.Services.Command
{
    public class CommandHelp
    {
        public string commandName { get; set; }
        public string description { get; set; }
        public ParameterHelp[] parameters { get; set; }
    }

    public class ParameterHelp
    {
        public bool opional { get; set; }
        public string description { get; set; }
        public string[] potentialValues { get; set; }
    }
}
