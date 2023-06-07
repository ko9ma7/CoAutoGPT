using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CollaborativeChatGPTLibrary.Classes
{
    [Serializable]
    public class Prompt
    {
        public Guid Id { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public string Value { get; set; }

        public Prompt()
        {
            Id = Guid.NewGuid();
        }


    }
}
