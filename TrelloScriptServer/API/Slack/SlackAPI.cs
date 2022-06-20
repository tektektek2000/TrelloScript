using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;

namespace TrelloScriptServer.API.Slack
{
    public class SlackAPI
    {
        private readonly string _url;
        private readonly HttpClient _client;
        private readonly SlackAPIConfig _slackConfig;


        public SlackAPI(SlackAPIConfig config)
        {
            _slackConfig = config;
            _url = "https://slack.com/api/chat.postMessage";
            _client = new HttpClient();
            _client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", config.token);
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

        private async Task AsyncMessage(string channel, string message)
        {
            var postObject = new { channel = channel, text = message, mrkdwn = true };
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

        public void Message(string channel, string message)
        {
            _ = AsyncMessage(channel, message);
        }
    }
}
