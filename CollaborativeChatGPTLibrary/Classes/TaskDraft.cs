using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CollaborativeChatGPTLibrary.Classes
{
    public class TaskDraft
    {
        public List<AgentTask> TaskAgents { get; set; }

        public string Draft { get; set; }
    }
}
