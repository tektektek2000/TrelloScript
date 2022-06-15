using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Xml.Linq;
using System.Net.Http.Json;
using Newtonsoft.Json.Linq;
using System.IO;

namespace TrelloScriptServer.API.Trello
{

    class FailedRestRequestException : Exception
    {

    }

    class TrelloAPI
    {
        private string Token = "";
        private string Key = "";
        private HttpClient client;

        public TrelloAPI(string jsonConfigPath)
        {
            var config = JObject.Parse(File.ReadAllText(jsonConfigPath));
            Key = config["key"].ToString();
            Token = config["token"].ToString();
            client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public TrelloAPI(string key, string token)
        {
            Key = key;
            Token = token;
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

        public List<TrelloBoard> getBoards()
        {
            List<TrelloBoard> ret = new List<TrelloBoard>();
            var httpRequest = new HttpRequestMessage(HttpMethod.Get, "https://api.trello.com/1/members/me/boards?" + "key=" + Key + "&token=" + Token);
            HttpResponseMessage response = client.SendAsync(httpRequest).Result;
            if (response.IsSuccessStatusCode)
            {
                // Parse the response body.
                string dataObjects = response.Content.ReadAsStringAsync().Result;
                var details = JArray.Parse(dataObjects);
                for (int i = 0; i < details.Count; i++)
                {
                    TrelloBoard newBoard = new TrelloBoard();
                    newBoard.id = details[i]["id"].ToString();
                    newBoard.name = details[i]["name"].ToString();
                    newBoard.desc = details[i]["desc"].ToString();
                    //Console.WriteLine("Board[" + i + "]: id=" + newBoard.id + " name=" + newBoard.name + " desc=" + newBoard.desc);
                    ret.Add(newBoard);
                }
                return ret;
            }
            else
            {
                Logger.WriteLine(httpRequest.ToString());
                PrintIncoming(response);
                throw new FailedRestRequestException();
            }
        }

        public List<TrelloList> getLists(TrelloBoard board)
        {
            List<TrelloList> ret = new List<TrelloList>();
            var httpRequest = new HttpRequestMessage(HttpMethod.Get, "https://api.trello.com/1/boards/" + board.id + "/lists?" + "key=" + Key + "&token=" + Token);
            HttpResponseMessage response = client.SendAsync(httpRequest).Result;
            if (response.IsSuccessStatusCode)
            {
                // Parse the response body.
                string dataObjects = response.Content.ReadAsStringAsync().Result;
                var details = JArray.Parse(dataObjects);
                for (int i = 0; i < details.Count; i++)
                {
                    TrelloList newList = new TrelloList();
                    newList.id = details[i]["id"].ToString();
                    newList.name = details[i]["name"].ToString();
                    //newBoard.pos = Int16.Parse(details[i]["pos"].ToString());
                    newList.parentBoard = board;
                    //Console.WriteLine("Board[" + i + "]: id=" + newList.id + " name=" + newList.name + " pos=" + newList.pos);
                    ret.Add(newList);
                }
                return ret;
            }
            else
            {
                Logger.WriteLine(httpRequest.ToString());
                PrintIncoming(response);
                throw new FailedRestRequestException();
            }
        }

        public List<TrelloCard> getCards(TrelloList list)
        {
            List<TrelloCard> ret = new List<TrelloCard>();
            var httpRequest = new HttpRequestMessage(HttpMethod.Get, "https://api.trello.com/1/lists/" + list.id + "/cards?" + "key=" + Key + "&token=" + Token);
            HttpResponseMessage response = client.SendAsync(httpRequest).Result;
            if (response.IsSuccessStatusCode)
            {
                // Parse the response body.
                string dataObjects = response.Content.ReadAsStringAsync().Result;
                var details = JArray.Parse(dataObjects);
                for (int i = 0; i < details.Count; i++)
                {
                    TrelloCard newCard = new TrelloCard();
                    newCard.id = details[i]["id"].ToString();
                    newCard.name = details[i]["name"].ToString();
                    newCard.desc = details[i]["desc"].ToString();
                    newCard.parentList = list;
                    //Console.WriteLine("Board[" + i + "]: id=" + newCard.id + " name=" + newCard.name + " desc=" + newCard.desc);
                    ret.Add(newCard);
                }
                return ret;
            }
            else
            {
                Logger.WriteLine(httpRequest.ToString());
                PrintIncoming(response);
                throw new FailedRestRequestException();
            }
        }

        public void updateCards(TrelloCard card)
        {
            List<TrelloCard> ret = new List<TrelloCard>();
            var httpRequest = new HttpRequestMessage(HttpMethod.Put, "https://api.trello.com/1/cards/" + card.id + "?" + "name=" + card.name + "&desc=" + card.desc
                + "&key=" + Key + "&token=" + Token);
            HttpResponseMessage response = client.SendAsync(httpRequest).Result;
            if (response.IsSuccessStatusCode)
            {

            }
            else
            {
                Logger.WriteLine(httpRequest.ToString());
                PrintIncoming(response);
                throw new FailedRestRequestException();
            }
        }

        ~TrelloAPI()
        {
            client.Dispose();
        }
    }
}
