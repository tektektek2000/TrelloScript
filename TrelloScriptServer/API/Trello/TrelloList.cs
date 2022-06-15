using System;
using System.Collections.Generic;
using System.Text;

namespace TrelloScriptServer.API.Trello
{
    class TrelloList
    {
        public TrelloBoard parentBoard { get; set; }
        public string id { get; set; }
        public string name { get; set; }
        public int pos { get; set; }
        public List<TrelloCard> cards { get; set; }
    }
}
