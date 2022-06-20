using System;
using System.Collections.Generic;
using System.Text;

namespace TrelloScriptServer.API.Trello.Model
{
    class TrelloBoard
    {
        public string id { get; set; }
        public string name { get; set; }
        public string desc { get; set; }

        public List<TrelloList> lists { get; set; }
        public List<TrelloMember> members { get; set; }
    }
}
