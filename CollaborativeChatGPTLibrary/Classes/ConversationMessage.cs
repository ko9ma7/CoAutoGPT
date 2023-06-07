using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CollaborativeChatGPTLibrary.Classes
{
    [Serializable]
    public class ConversationMessage
    {
        public SpeakerEnum Speaker { get; set; }

        public string Message { get; set; }
    }
}
