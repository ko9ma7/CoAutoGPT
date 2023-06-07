using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CollaborativeChatGPTLibrary.Classes
{
    [Serializable]
    public class APICallEvent
    {
        public string Prompt { get; set; }

        public List<string> Context { get; set; }

        public DateTime Time { get; set; }
    }
}
