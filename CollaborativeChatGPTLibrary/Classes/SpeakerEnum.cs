using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CollaborativeChatGPTLibrary.Classes
{
    [Serializable]
    public enum SpeakerEnum
    {
        //"system", "user", or "assistant"
        user,
        assistant,
        system,
    }
}
