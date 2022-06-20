using Newtonsoft.Json.Linq;

namespace TrelloScriptServer.Services.Command
{
    public class CommandValidator
    {
        private string token;

        public CommandValidator(string Token)
        {
            token = Token;
        }

        public bool Validate(string Token)
        {
            return token == Token;
        }
    }
}
