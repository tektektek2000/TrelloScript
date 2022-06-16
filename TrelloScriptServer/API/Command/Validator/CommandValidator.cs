using Newtonsoft.Json.Linq;

namespace TrelloScriptServer.API.Command.Validator
{
    public class CommandValidator
    {
        private string SecurityToken;

        public CommandValidator(JToken config)
        {
            SecurityToken = config["token"].ToString();
        }

        public bool Validate(string token)
        {
            return SecurityToken == token;
        }
    }
}
