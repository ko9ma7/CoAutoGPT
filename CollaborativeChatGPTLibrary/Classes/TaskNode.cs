using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CollaborativeChatGPTLibrary.Classes
{
    [Serializable]
    public class TaskNode
    {
        public TaskNode(AgentTask value)
        {
            Value = value;
        }

        public AgentTask Value { get; set; }

        public TaskNode Next { get; set; }

    }
}
