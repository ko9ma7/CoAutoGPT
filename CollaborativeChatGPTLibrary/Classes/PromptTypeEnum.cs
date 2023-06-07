using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CollaborativeChatGPTLibrary.Classes
{
    public enum PromptTypeEnum
    {
        USER,
        PROMPT_GENERATOR,
        CRITIC,
        EXPERT,
        EXPERT_DEFINE,
        MEMORY_MANAGER,
        PROBLEM_BREAKDOWN,
        EXPERT_CAPTURE,
        EXECUTE_TASK_PROMPT,
        IS_REVISED_TRANSFORM,
        EXPAND_PROMPT,
        EXPAND_SECTION,
        EXPERT_ANSWER_QUESTION_PROMPT,
        IS_DELETE_STATEMENT,
        PROMPT_COMPLEXITY_ESTIMATE,
        PROMPT_STEP_COUNT_ESTIMATE,
        IS_DONE
    }
}
