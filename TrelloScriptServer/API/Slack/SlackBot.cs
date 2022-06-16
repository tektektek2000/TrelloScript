using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;
using TrelloScriptServer.API.Command.Model;
using TrelloScriptServer.API.Trello;

namespace TrelloScriptServer.API.Slack
{
    public class SlackBot
    {
        private readonly string _url;
        private readonly HttpClient _client;
        private readonly string _channel;
        public List<Alias> Aliases { get; set; }


        public SlackBot(JToken token)
        {
            var sercurityToken = token["token"].ToString();
            _channel = token["channel"].ToString();
            _url = "https://slack.com/api/chat.postMessage";
            _client = new HttpClient();
            _client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", sercurityToken);
            Aliases = new List<Alias>();
            foreach (var it in token["aliases"])
            {
                Alias newAlias = new Alias();
                newAlias.TrelloName = it["trello"].ToString();
                newAlias.SlackName = it["slack"].ToString();
                Aliases.Add(newAlias);
            }
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

        public async Task WriteMessage(string message)
        {
            var postObject = new { channel = _channel, text = message, mrkdwn = true };
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

        public void Message(string message)
        {
            WriteMessage(message);
        }

        public string getSlackAlias(string trelloName)
        {
            foreach(var it in Aliases)
            {
                if(it.TrelloName == trelloName)
                {
                    return it.SlackName;
                }
            }
            return trelloName;
        }
    }
}
