using Newtonsoft.Json.Linq;

namespace TrelloScriptServer.API.Command.Validator
{
    public class CommandValidator
    {
        private static CommandValidator? Instance = null;
        private string SecurityToken;

        private CommandValidator(string pathToConfig)
        {
            var config = JObject.Parse(File.ReadAllText(pathToConfig));
            SecurityToken = config["token"].ToString();
        }

        public static void Init(string pathToConfig)
        {
            Instance = new CommandValidator(pathToConfig);
        }

        public static bool Validate(string token)
        {
            if(Instance == null) { return false; }
            return Instance.SecurityToken == token;
        }
    }
}
