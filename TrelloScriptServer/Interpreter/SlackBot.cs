using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;
using TrelloScriptServer.API.Command.Model;
using TrelloScriptServer.API.Trello;

namespace TrelloScriptServer.Interpreter
{
    public class SlackBot
    {
        private readonly string _url;
        private readonly HttpClient _client;
        private readonly string _channel;

        private static SlackBot? _instance = null;

        private SlackBot(string token, string channel)
        {
            _channel = channel;
            _url = "https://slack.com/api/chat.postMessage";
            _client = new HttpClient();
            _client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }

        static void PrintIncoming(HttpResponseMessage response)
        {
            string s = "<Incoming message on Web API>\n"
                + (int)response.StatusCode
                + " ("
                + response.ReasonPhrase
                + " )\n"
                + response.Content.ReadAsStringAsync().Result
                + "\n<Message end>";
            Logger.WriteLine(s);
        }

        public static void Init(string pathToConfig, string channel)
        {
            if (_instance == null)
            {
                var config = JObject.Parse(File.ReadAllText(pathToConfig));
                string SecurityToken = config["token"].ToString();
                _instance = new SlackBot(SecurityToken,channel);
            }
        }

        public async Task WriteMessage(string message)
        {
            var postObject = new { channel = _channel, text = message };
            var json = JsonConvert.SerializeObject(postObject);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _client.PostAsync(_url, content);

            if (response.IsSuccessStatusCode)
            {
                
            }
            else
            {
                Logger.WriteLine(_url + "\n" + content.ToString());
                PrintIncoming(response);
            }
        }

        public static void Message(string message)
        {
            if (_instance != null)
            {
                _instance.WriteMessage(message);
            }
        }
    }
}
