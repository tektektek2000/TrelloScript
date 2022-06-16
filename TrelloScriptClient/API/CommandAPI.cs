using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Xml.Linq;
using System.Net.Http.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using TrelloScriptClient.API.Command.Model;

namespace TrelloScriptClient.API
{

    class FailedRestRequestException : Exception
    {

    }

    class CommandAPI
    {
        private string Token = "";
        private string Address = "";
        private string WorkPlace = "";
        private HttpClient client;

        public CommandAPI(string jsonConfigPath)
        {
            var config = JObject.Parse(File.ReadAllText(jsonConfigPath));
            Address = config["address"].ToString();
            Token = config["token"].ToString();
            WorkPlace = config["workplace"].ToString();
            client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
        }

        static void PrintOutGoing(HttpContent content)
        {
            string s = "<Outgoing message on Web API>\n"
                + content.ReadAsStringAsync().Result
                + "\n<Message end>";
            Logger.WriteLine(s);
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

        public CommandResult runCommand(string command, string parameters)
        {
            var httpRequest = new HttpRequestMessage(HttpMethod.Get, Address + "/api/WorkPlace/" + WorkPlace + "/run/" + command + "?" + "token=" + Token + "&parameters=" + parameters);
            HttpResponseMessage response = client.SendAsync(httpRequest).Result;
            if (response.IsSuccessStatusCode)
            {
                // Parse the response body.
                string dataObjects = response.Content.ReadAsStringAsync().Result;
                var details = JObject.Parse(dataObjects);
                CommandResult res = CommandResult.Failure("");
                res = details.ToObject<CommandResult>();
                return res;
            }
            else
            {
                Logger.WriteLine(httpRequest.ToString());
                PrintIncoming(response);
                throw new FailedRestRequestException();
            }
        }

        ~CommandAPI()
        {
            client.Dispose();
        }
    }
}
