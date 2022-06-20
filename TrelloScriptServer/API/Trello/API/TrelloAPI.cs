using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Xml.Linq;
using System.Net.Http.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using TrelloScriptServer.API.Trello.Model;
using TrelloScriptServer.API.Trello.Config;

namespace TrelloScriptServer.API.Trello.API
{

    class FailedRestRequestException : Exception
    {

    }

    class TrelloAPI
    {
        private TrelloAPIConfig config;
        private HttpClient client;
        List<TrelloMember> bufferedMembers = new List<TrelloMember>();
        DateTime updatedMembersLastTime = DateTime.Now;
        object membersLock = new object();

        public TrelloAPI(TrelloAPIConfig Config)
        {
            config = Config;
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

        public TrelloMember getMember(string id)
        {
            lock (membersLock)
            {
                if (DateTime.Now - updatedMembersLastTime > new TimeSpan(0, 20, 0))
                {
                    bufferedMembers.Clear();
                    updatedMembersLastTime = DateTime.Now;
                }
                foreach (var it in bufferedMembers)
                {
                    if (id == it.id) { return it; }
                }
            }
            var httpRequest = new HttpRequestMessage(HttpMethod.Get, "https://api.trello.com/1/members/" + id + "?" + "key=" + config.key + "&token=" + config.token);
            HttpResponseMessage response = client.SendAsync(httpRequest).Result;
            if (response.IsSuccessStatusCode)
            {
                string dataObjects = response.Content.ReadAsStringAsync().Result;
                var details = JObject.Parse(dataObjects);
                TrelloMember newMember = new TrelloMember();
                newMember.id = details["id"].ToString();
                newMember.userName = details["username"].ToString();
                newMember.FullName = details["fullName"].ToString();
                lock (membersLock)
                {
                    bufferedMembers.Add(newMember);
                }
                return newMember;
            }
            else
            {
                Logger.WriteLine(httpRequest.ToString());
                PrintIncoming(response);
                throw new FailedRestRequestException();
            }
        }

        public List<TrelloBoard> getBoards()
        {
            List<TrelloBoard> ret = new List<TrelloBoard>();
            var httpRequest = new HttpRequestMessage(HttpMethod.Get, "https://api.trello.com/1/members/me/boards?" + "key=" + config.key + "&token=" + config.token);
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
                    if (details[i]["memberships"] != null && details[i]["memberships"].ToString() != "")
                    {
                        newBoard.members = new List<TrelloMember>();
                        foreach (var it in details[i]["memberships"])
                        {
                            var member = getMember(it["idMember"].ToString());
                            newBoard.members.Add(member);
                        }
                    }
                    //Console.WriteLine("Board[" + i + "]: id=" + newBoard.id + " name=" + newBoard.name + " desc=" + newBoard.desc);
                    ret.Add(newBoard);
                }
                Thread.Sleep(120);
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
            var httpRequest = new HttpRequestMessage(HttpMethod.Get, "https://api.trello.com/1/boards/" + board.id + "/lists?" + "key=" + config.key + "&token=" + config.token);
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
                Thread.Sleep(120);
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
            var httpRequest = new HttpRequestMessage(HttpMethod.Get, "https://api.trello.com/1/lists/" + list.id + "/cards?" + "key=" + config.key + "&token=" + config.token);
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
                    newCard.url = "https://trello.com/c/" + newCard.id;
                    if (details[i]["due"] != null && details[i]["due"].ToString() != "")
                    {
                        newCard.due = details[i]["due"].ToObject<DateTime>();
                        newCard.dueComplete = details[i]["dueComplete"].ToObject<bool>();
                    }
                    if (details[i]["idMembers"] != null && details[i]["idMembers"].ToString() != "")
                    {
                        newCard.members = new List<TrelloMember>();
                        foreach (var it in details[i]["idMembers"])
                        {
                            foreach (var it2 in list.parentBoard.members)
                            {
                                if (it2.id == it.ToString())
                                {
                                    newCard.members.Add(it2);
                                }
                            }
                        }
                    }
                    ret.Add(newCard);
                }
                Thread.Sleep(120);
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
                + "&key=" + config.key + "&token=" + config.token);
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
