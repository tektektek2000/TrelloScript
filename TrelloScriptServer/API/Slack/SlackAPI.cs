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
            _url = "https://slack.com/api/";
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

        private async Task AsyncMessage(string channelID, string message)
        {
            var postObject = new { channel = channelID, text = message, mrkdwn = true };
            var json = JsonConvert.SerializeObject(postObject);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _client.PostAsync(_url + "chat.postMessage", content);

            if (response.IsSuccessStatusCode)
            {

            }
            else
            {
                Logger.WriteLine(_url + "\n" + content.ToString());
                PrintIncoming(response);
            }
        }

        public void Message(string channelID, string message)
        {
            _ = AsyncMessage(channelID, message);
        }

        public List<SlackMessage> getMessages(string channelID)
        {
            List<SlackMessage> messages = new List<SlackMessage>();
            var response = _client.GetAsync(_url + "conversations.history" + "?channel=" + channelID).Result;
            if (response.IsSuccessStatusCode)
            {
                string dataObjects = response.Content.ReadAsStringAsync().Result;
                var details = JObject.Parse(dataObjects);
                if (details["ok"].ToObject<Boolean>())
                {
                    foreach (var it in details["messages"])
                    {
                        messages.Add(it.ToObject<SlackMessage>());
                    }
                }
            }
            else
            {
                Logger.WriteLine(_url + "\n");
                PrintIncoming(response);
            }
            return messages;
        }

        public SlackDMChannel getDMChannel(string userID)
        {
            SlackDMChannel channel = new SlackDMChannel();
            var response = _client.GetAsync(_url + "conversations.list" + "?types=im").Result;
            if (response.IsSuccessStatusCode)
            {
                string dataObjects = response.Content.ReadAsStringAsync().Result;
                var details = JObject.Parse(dataObjects);
                if (details["ok"].ToObject<Boolean>())
                {
                    foreach (var it in details["channels"])
                    {
                        var c = it.ToObject<SlackDMChannel>();
                        if(c.user == userID)
                        {
                            channel = c;
                        }
                    }
                }
            }
            else
            {
                Logger.WriteLine(_url + "\n");
                PrintIncoming(response);
            }
            return channel;
        }

        public void DeleteMessage(string channelID, SlackMessage message)
        {
            var content = new StringContent("", Encoding.UTF8, "application/json");
            var response = _client.GetAsync(_url + "chat.delete" + "?channel=" + channelID + "&ts=" + message.ts).Result;
            if (response.IsSuccessStatusCode)
            {
                
            }
            else
            {
                Logger.WriteLine(_url + "\n" + content.ToString());
                PrintIncoming(response);
            }
        }
    }
}
