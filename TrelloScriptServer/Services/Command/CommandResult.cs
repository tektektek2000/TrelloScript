namespace TrelloScriptServer.Services.Command
{
    public class CommandResult
    {
        public bool Successfull { get; set; }
        public string? Message { get; set; }
        public string? Body { get; set; }
        public List<string>? Warnings { get; set; }

        public static CommandResult Failure(string Message, List<string>? Warnings = null)
        {
            return new CommandResult { Successfull = false, Message = Message, Warnings = Warnings };
        }

        public static CommandResult Success(string Message, List<string>? Warnings = null)
        {
            return new CommandResult { Successfull = true, Message = Message, Warnings = Warnings };
        }
    }
}
