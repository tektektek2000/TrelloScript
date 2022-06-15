namespace TrelloScriptServer.API.Command.Model
{
    public class CommandResult
    {
        public bool Successfull { get; set; }
        public string? Message { get; set; }
        public string? Body { get; set; }

        public static CommandResult Failure(string Message)
        {
            return new CommandResult { Successfull = false, Message = Message };
        }

        public static CommandResult Success(string Message)
        {
            return new CommandResult { Successfull = true, Message = Message };
        }
    }
}
