using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CollaborativeChatGPTLibrary.Classes
{
    public class MemoryItem
    {
        public Guid Id { get; set; }

        public int TokenCount { get; set; }
        public string Text { get; set; }
        public List<string> Keywords { get; set; }
        public float ImportanceScore { get; set; }
    }
}
