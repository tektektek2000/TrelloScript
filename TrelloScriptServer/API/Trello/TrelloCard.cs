using System;
using System.Collections.Generic;
using System.Text;

namespace TrelloScriptServer.API.Trello
{
    class TrelloCard
    {
        public TrelloList parentList { get; set; }
        public string id { get; set; }
        public string name { get; set; }
        public string desc { get; set; }
        public string? url { get; set; }
        public DateTime? due { get; set; }
        public bool? dueComplete { get; set; }
        public List<TrelloMember> members { get; set; }
    }
}
