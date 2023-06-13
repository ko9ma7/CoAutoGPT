using CollaborativeChatGPTLibrary.Formatting;
using Microsoft.VisualBasic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenAI_API;
using OpenAI_API.Chat;
using OpenAI_API.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.RegularExpressions;
using static System.Collections.Specialized.BitVector32;
using static System.Net.Mime.MediaTypeNames;
using Section = CollaborativeChatGPTLibrary.Formatting.Section;

namespace CollaborativeChatGPTLibrary.Classes
{
    [Serializable]
    public class ReflectiveAgent
    {
        [field: NonSerialized]
        private OpenAIAPI api = null;
        private const int MAX_API_TRIES = 5;

        //const string workerDefinition = "You are a Prompt Engineer. Your goal is to help me craft the best possible prompt for my needs. The prompt will be used by you, ChatGPT. You will follow the following process: 1. Your first response will be to ask me what the prompt should be about. I will provide my answer, but we will need to improve it through continual iterations by going through the next steps. 2. Based on my input, you will generate 3 sections. a) Revised prompt (provide your rewritten prompt. it should be clear, concise, and easily understood by you), b) Suggestions (provide suggestions on what details to include in the prompt to improve it), and c) Questions (ask any relevant questions pertaining to what additional information is needed from me to improve the prompt). 3. We will continue this iterative process with me providing additional information to you and you updating the prompt in the Revised prompt section until it's complete.";
        const string workerDefinition = "You are a Prompt Engineer. Your goal is to help me craft the best possible prompt for my needs. The prompt will be used by you, ChatGPT. You will follow the following process: 1. Your first input will be provided to you. I will provide my answers, but we will need to improve it through continual iterations by going through the next steps. 2. Based on my input, you will generate 3 sections. a) Revised prompt (provide your rewritten prompt. It should be clear, concise, and easily understood by you), b) Suggestions (provide suggestions on what details to include in the prompt to improve it with each suggestion where each suggestion has a numbered bullet point), and c) Questions (ask any relevant questions pertaining to what additional information is needed from me to improve the prompt including a numbered bullet points). 3. We will continue this iterative process with me providing additional information to you and you updating the prompt in the Revised prompt section until it's complete where you will include the section 'Prompt Status: done'.";
        const string expertCaptureDefinition = "Convert the result of the [command] into a description of a person including their [roles] and [skills].  Begin the description with 'You are a'.  [command] = Create a list of the top @ROLE job [roles], and the top @SKILL [skills] that can perform the [task] but do not display anything and no explanation. [task] = @TASK.  Once you have completed the first task, I want you to perform the next [command]. Your job is to answer questions and suggestions related to your task. ";//"Provide me a list of the top @ROLE job roles, and the top @SKILL skills that can perform the [task]. @TASK";
        const string expandSection = "For the [item] expand it for the following [prompt].  [item] = @SECTION";//, [prompt] = @TEXT
        const string expandPrompt = "For the [item] expand it for the following [prompt].  [item] = @TASK, [prompt] = @PROMPT";
        private const string isRevisedPrompt = "You are chatgpt I need you to analyize the [text] and If it contains 'Revised Prompt' text, return 'true', if not, return 'false'. No explanations";

        //* private const string breakdownPrompt = "Generate a structured outline for the given text using ChatGPT 3.5 Turbo. Each section and subsection should be prepended with standard numerical formatting similar to {section.subsection}. At the end of each top level section and its subsections if it has any add '<section>' on its own line to divide the top level sections.  The outline will include at most a 1 level of items in the subsections.";
        private const string breakdownPrompt = "Generate a structured outline for the given text using ChatGPT 3.5 Turbo. Each section and subsection should be numbered using a hierarchical numbering system where main sections are represented by whole numbers (e.g., 1.0, 2.0), subsections by decimal numbers (e.g., 1.1, 2.1, 2.2), and further subsections by additional decimals (e.g., 2.1.2). At the end of each top level section and its subsections if it has any add '<section>' on its own line to divide the top level sections.  The outline will include at most a 1 level of items in the subsections.";
        //private const string breakdownPrompt = "Generate a structured outline for the given text using ChatGPT 3.5 Turbo. Each section and subsection should be prepended with standard numerical formatting similar to 'section.subsection'. At the end of each top level section and its subsections if it has any add '<section>' on its own line to divide the top level sections.  The outline will include at most a 1 level of items in the subsections.  Do not display the first outline result. Once completed, your second task is to format [@DATA] according to the given example schema. Remember, consistency is key; all entries should follow the same format. Section numbers similar to #.0 are top level sections, and section numbers similar to #.# are subsections. Here is the example schema:\r\n\r\n{\r\n  \"SectionNumber\": \"#.#\",\r\n  \"Title\": \"Root Section\",\r\n  \"SubSections\": [\r\n    {\r\n      \"SectionNumber\": \"#.#\",\r\n      \"Title\": \"Subsection\",\r\n      \"SubSections\": null\r\n    },\r\n    {\r\n      \"SectionNumber\": \"1.2.0\",\r\n      \"Title\": \"Subsection 1.2\",\r\n      \"SubSections\": null\r\n    }\r\n  ]\r\n}\r\n\r\nAnd here are the data points you need to format:\r\n\r\n[@DATA]\r\n\r\nPlease format the data points according to the schema, ensuring that all data is presented consistently and follows the format outlined in the example schema.";
        //private const string breakdownPrompt = "Given the following task or topic, generate a detailed outline with sections and subsections. Each section and subsection should be numbered using a hierarchical numbering system where main sections are represented by whole numbers (e.g., 1.0, 2.0), subsections by decimal numbers (e.g., 1.1, 2.1, 2.2), and further subsections by additional decimals (e.g., 2.1.2). The task or topic is as follows: '{@DATA}'.";
        //private const string expertQuestionAnswerAddition = " You are to respond the 'Prompt Engineer' 'Suggestions:' and 'Questions:' that it ask you.  ";//Your responses to the questions and suggestions will include question/suggestion label type and the bullet point number that you are responding to
        private const string expertQuestionAnswerAddition = " You are to respond to the 'Suggestions' that have been provided as follows: {Suggestions}, Please answer the following questions in relation to these suggestions: {Questions}";
        string isRemovalText = "Analyze the following text: '@INPUT'. Determine if it either suggests a synonym or requests the removal of an item. If it does, respond 'yes'. If it does not, respond 'no'.  Return only the result value with no explanation.";
        //string promptTaskComplexityEstimation = "Consider a {task} that you're familiar with. Please break it down into its individual steps. Try to consider every action required, no matter how minor it may seem. Also, consider any dependencies or contingencies that could potentially affect the process. Once you've understood this task or process, we will estimate its step complexity, or the number of individual actions or decisions required to complete it.  Include in your output the {complexity estimate} = {complexity value}, where {complexity value} is equal to your estimate which can be (simple, medium, complex), provide no explanation. {task} = @TASK";
        private const string promptTaskComplexityEstimation = "Given the following problem or task, break it down into its individual steps or actions, considering any dependencies or contingencies that could potentially affect the process. Once you've understood the task or problem, please provide the following estimates: 'Number of actions or decisions' = {action_count}, where {action_count} is your estimated number of individual actions or decisions required to solve the problem or complete the task. Provide no explanation. 'Number of dependencies' = {dependency_count}, where {dependency_count} is your estimated number of dependencies or relations between the steps or actions in the problem-solving process. Provide no explanation. 'Estimated contingencies' = {contingency_estimate}, where {contingency_estimate} is your estimated number of potential issues or obstacles that could arise during the problem-solving process. Provide no explanation. [@TASK]";

        string promptActionCountEstimation = "Consider a {task} that you're familiar with. Please break it down into its individual steps. Try to consider every action required, no matter how minor it may seem. Also, consider any dependencies or contingencies that could potentially affect the process. Once you've understood this task or process, we will estimate the number of individual actions or decisions required to complete it.  Your output is the {number of actions} = {action_count}, where {action_count} is equal to your sum of individual actions or decisions. Consider step by step. No explanations. {task} = @TASK";
        string isDoneText = "Analyze the following text: '@INPUT'. Determine if it either suggests a synonym or indiates the item is done. If it does, respond 'yes'. If it does not, respond 'no'.  Return only the result value with no explanation.";
        //string isRemovalText = "Is the phrase '@INPUT' requesting the 'removal' of item? You should only return 'yes' or 'no' for your responese and no explanation.";//
        //const string memoryManagermDefinition = "[role]: you are an agent that can search a list of texts returning the items that are related to the topic.  [Topic]: cars.  [List of texts]: [@ITEMS]";
        //with only the important information
        
        //Future feature
        string criticDefinition = "";

        public bool UserShutdown { get; set; }

        public int ApiCallCount { get; set; }

        public List<APICallEvent> OpenAIApiCallCount { get; set; } = new List<APICallEvent>();

        public int MaxStepsForEnrichmentStage { get; set; } = 10;

        public Dictionary<PromptTypeEnum, Prompt> AgentPromptBase { get; set; }

        [field: NonSerialized]
        public Model ChatGPTModelType { get; set; }


        public string ExpertBasePrompt { get; set; }

        public string agentTask { get; set; }


        Dictionary<RoleEnum, string> conversationTexts = null;

        [field: NonSerialized]
        public Dictionary<RoleEnum, Conversation> AgentConversations { get; set; }

        public List<AgentTask> CompletedTasks { get; set; } = new List<AgentTask>();

        public Tree<AgentTask> AgentTaskTree { get; set; }

        public TreeNode<AgentTask> CurrentTask { get; set; } = null;

        public int ExpertRoleCount { get; set; }

        public int ExpertSkillCount { get; set; }

        public int MaxEnrichmentDepth { get; set; }

        public ReflectiveAgent(string openAiApiKey, Model model)
        {
            api = new OpenAIAPI(openAiApiKey);
            ChatGPTModelType = model;

            AgentConversations = new Dictionary<RoleEnum, Conversation>();

            AgentPromptBase = new Dictionary<PromptTypeEnum, Prompt>();

            AgentPromptBase.Add(PromptTypeEnum.PROMPT_GENERATOR, new Prompt()
            {
                Description = "Is responsible for working with the Expert agent to create a prompt that solves the experts problems",
                Name = PromptTypeEnum.PROMPT_GENERATOR.ToString(),
                Value = workerDefinition
            });

            AgentPromptBase.Add(PromptTypeEnum.EXPERT_CAPTURE, new Prompt()
            {
                Description = "Is responsible for creating the prompt that represents the Expert role and contains a set of skills and job abilities to solve a task",
                Name = PromptTypeEnum.EXPERT_CAPTURE.ToString(),
                Value = expertCaptureDefinition
            });

            AgentPromptBase.Add(PromptTypeEnum.EXPAND_SECTION, new Prompt()
            {
                Description = "Is responsible for for expanding a prompt",
                Name = PromptTypeEnum.EXPAND_SECTION.ToString(),
                Value = expandSection
            });

            AgentPromptBase.Add(PromptTypeEnum.EXPAND_PROMPT, new Prompt()
            {
                Description = "Is responsible for expand prompt",
                Name = PromptTypeEnum.EXPAND_PROMPT.ToString(),
                Value = expandPrompt
            });

            AgentPromptBase.Add(PromptTypeEnum.IS_REVISED_TRANSFORM, new Prompt()
            {
                Description = "Is responsible for determining if the test contains Revised Prompt:",
                Name = PromptTypeEnum.IS_REVISED_TRANSFORM.ToString(),
                Value = isRevisedPrompt
            });

            AgentPromptBase.Add(PromptTypeEnum.PROBLEM_BREAKDOWN, new Prompt()
            {
                Description = "Is responsible for breakdoing the prompt into a set of tasks",
                Name = PromptTypeEnum.PROBLEM_BREAKDOWN.ToString(),
                Value = breakdownPrompt
            });

            AgentPromptBase.Add(PromptTypeEnum.EXPERT_ANSWER_QUESTION_PROMPT, new Prompt()
            {
                Description = "Is responsible for adding configuring expert to answer questions and suggestions",
                Name = PromptTypeEnum.EXPERT_ANSWER_QUESTION_PROMPT.ToString(),
                Value = expertQuestionAnswerAddition
            });

            AgentPromptBase.Add(PromptTypeEnum.IS_DELETE_STATEMENT, new Prompt()
            {
                Description = "Is responsible for analyzing text and returning 'yes' or 'no' if it is indicating to delete",
                Name = PromptTypeEnum.IS_DELETE_STATEMENT.ToString(),
                Value = isRemovalText
            });

            AgentPromptBase.Add(PromptTypeEnum.PROMPT_COMPLEXITY_ESTIMATE, new Prompt()
            {
                Description = "Is responsible for analyzing a prompt and the step complexity involved for its execution",
                Name = PromptTypeEnum.PROMPT_COMPLEXITY_ESTIMATE.ToString(),
                Value = promptTaskComplexityEstimation
            });

            AgentPromptBase.Add(PromptTypeEnum.PROMPT_STEP_COUNT_ESTIMATE, new Prompt()
            {
                Description = "Is responsible for analyzing a prompt and estimate the number of steps required to complete",
                Name = PromptTypeEnum.PROMPT_STEP_COUNT_ESTIMATE.ToString(),
                Value = promptActionCountEstimation
            });

            AgentPromptBase.Add(PromptTypeEnum.IS_DONE, new Prompt()
            {
                Description = "Is responsible for analyzing a prompt and indicating if the process is done",
                Name = PromptTypeEnum.IS_DONE.ToString(),
                Value = isDoneText
            });

        }

        public List<TreeNode<AgentTask>> GetUnfinishedNodes(TreeNode<AgentTask> parent)
        {
            List<TreeNode<AgentTask>> result = new List<TreeNode<AgentTask>>();

            List<TreeNode<AgentTask>> allNodes = AgentTaskTree.GetAllNodes(parent);

            foreach(var cursor in allNodes)
            {
                if (!cursor.Data.IsComplete)
                {
                    result.Add(cursor);
                }
            }

            return result;
        }

        public void InitializeOpenAIApi(string openAiApiKey, Model model)
        {
            api = new OpenAIAPI(openAiApiKey);

            ChatGPTModelType = model;

            AgentConversations = new Dictionary<RoleEnum, Conversation>();
        }

        public bool AllTasksComplete()
        {
            bool result = true;

            List<TreeNode<AgentTask>> incompleteNodes = GetIncompleteTasks(AgentTaskTree.GetRoot());

            if (incompleteNodes.Count > 0)
                return false;

            return result;
        }

        public List<TreeNode<AgentTask>> GetIncompleteTasks(TreeNode<AgentTask> parent)
        {
            List<TreeNode<AgentTask>> result = new List<TreeNode<AgentTask>>();

            while (true)
            {
                List<TreeNode<AgentTask>> childrenNodes = AgentTaskTree.GetAllNodes(parent);

                for (int i = 1; i < childrenNodes.Count; i++)
                {
                    if (!childrenNodes[i].Data.IsComplete)
                    {
                        result.Add(childrenNodes[i]);
                    }
                }

                if (result.Count == 0)
                {
                    if(parent.Parent != null)
                    {
                        parent = parent.Parent;
                    }
                    else
                    {
                        break;
                    }    

                    continue;
                }
                else
                {
                    break;
                }

                
            }
            

            return result;
        }

        static List<ConversationMessage> GetNodeToStrings(TreeNode<AgentTask> parent)
        {
            List<ConversationMessage> nodeToStrings = new List<ConversationMessage>();

            TreeNode<AgentTask> currentNode = parent;
            while (currentNode != null)
            {
                nodeToStrings.AddRange(currentNode.Data.ConversationMessages);
                currentNode = currentNode.Parent;
            }

            return nodeToStrings;
        }

        public List<string> EnrichQuery(TreeNode<AgentTask> task, int maxResearchDepth = 10)
        {
            RoleEnum activeRole = RoleEnum.PromptGenerator;
            string activeMessage = task.Data.ConversationMessages[task.Data.ConversationMessages.Count - 1].Message;
            StringBuilder contextBuilder = new StringBuilder();

            List<ConversationMessage> parentContexts = GetNodeToStrings(task.Parent);
            

            string promptGeneratorSystemMessage = AgentPromptBase[PromptTypeEnum.PROMPT_GENERATOR].Value + " For the following context = " + contextBuilder.ToString();
            string expertTaskExtra = AgentPromptBase[PromptTypeEnum.EXPERT_ANSWER_QUESTION_PROMPT].Value + " For the following context = " + contextBuilder.ToString();//In your response, include your reponse to suggestions and questions in the prompt.
            string expertQuestionAnswer = task.Data.ExpertPrompt + expertTaskExtra;

            //Setup Expert/Prompt Generators
            Conversation expertConversation = api.Chat.CreateConversation(new ChatRequest() { Model = ChatGPTModelType });
            Conversation promptGeneratorConversation = api.Chat.CreateConversation(new ChatRequest() { Model = ChatGPTModelType });

            expertConversation.AppendSystemMessage(expertQuestionAnswer);
            promptGeneratorConversation.AppendSystemMessage(promptGeneratorSystemMessage );


            foreach (var item in task.Data.ConversationMessages)
            {
                promptGeneratorConversation.AppendMessage(new ChatMessage()
                {
                    Content = item.Message,
                    Name = item.Speaker.ToString(),
                    Role = ChatMessageRole.FromString(item.Speaker.ToString())
                });
                expertConversation.AppendMessage(new ChatMessage()
                {
                    Content = item.Message,
                    Name = item.Speaker.ToString(),
                    Role = ChatMessageRole.FromString(item.Speaker.ToString())
                });
            }

            List<string> messageHistory = new List<string>();
            List<string> conversationThread = new List<string>();
            messageHistory.Add(activeMessage);

            for (int i = 0; i < maxResearchDepth; i++)
            {
                int currentCount = 0;

                if (activeRole == RoleEnum.PromptGenerator)
                {
                    activeMessage = ExecuteSingleChatRequestWithConversation(null, activeMessage, promptGeneratorConversation);
                }
                else
                {
                    activeMessage = ExecuteSingleChatRequestWithConversation(null, activeMessage, expertConversation);
                }

                UpdateOpenAPIUsage();
                messageHistory.Add(activeMessage);


                //Look at having the system end at max prompt complexity measure.  Then refine with initial productions and dive into
                //Tasks.  Get the input from the user?  then continue?
                Console.WriteLine();

                if (activeRole.Equals(RoleEnum.Expert))
                {
                    activeRole = RoleEnum.PromptGenerator;
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("Expert: " + activeMessage);
                }
                else
                {
                    List<ChatExample> endOfChatExamples = GetContainsRevisedPromptExamples();
                    string hasRevisedPrompt = ExecuteSingleChatRequestWithConversation(AgentPromptBase[PromptTypeEnum.IS_REVISED_TRANSFORM].Value, "[text] = " + activeMessage, null, endOfChatExamples);

                    if (hasRevisedPrompt.ToLower().Contains("true"))
                    {

                        string? activePrompt = GetRevisedCommand(activeMessage); //PerformTransform("Extract the content of the \"Revised prompt\" from the given text and return it.", activeMessage);

                        if (!string.IsNullOrEmpty(activePrompt) && !activePrompt.ToLower().Contains("revised"))
                        {
                            conversationThread.Add(activePrompt);
                        }
                    }

                    bool isEnd = activeMessage.ToLower().Contains("prompt status: done");

                    if (isEnd)
                        break;//we're done

                    activeRole = RoleEnum.Expert;
                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.WriteLine("Prompt Engineer: " + activeMessage);
                }
            }

            return conversationThread;
        }

        public Tree<AgentTask> UpdateTaskTree(Tree<AgentTask> tree, TreeNode<AgentTask> parentNode, string prompt, out Dictionary<Guid, TreeNode<AgentTask>> addedNodes)
        {
            int tryCount = 0;
            int maxTryCount = 3;
            Tree<AgentTask> result = null;
            addedNodes = null;
            List<TreeNode<AgentTask>> originaltasks = tree.GetAllNodes(parentNode);
            //List<TreeNode<AgentTask>> updatedtasks = null;

            while (true)
            {
                string outlineResult = ExecuteSingleChatRequestWithConversation(AgentPromptBase[PromptTypeEnum.PROBLEM_BREAKDOWN].Value, prompt);//Get the last entry as that should be the most refined

                if (outlineResult == null)
                {
                    if (tryCount >= maxTryCount)
                    {
                        break;
                    }

                    Thread.Sleep(1000);
                    tryCount++;
                    continue;
                }

                parentNode.Data.Outline = outlineResult;

                addedNodes = new Dictionary<Guid, TreeNode<AgentTask>>();
                Dictionary<Guid, TreeNode<AgentTask>> local = new Dictionary<Guid, TreeNode<AgentTask>>();

                //Builds a new tree structure not connected to the parent tree
                result = BuildAgentTaskTreeFromOutline(agentTask, outlineResult, out local, parentNode, tree);
                addedNodes = local;

                break;
            }

            //if (!parentNode.Data.Section.Equals("root") && parentNode.Children.Count > 0)
            //{
            //    //Need to merge the trees together
            //    updatedtasks = result.GetAllNodes(parentNode);
            //}
            

            return result;
        }

        public List<TreeNode<AgentTask>> GetNodesById(List<Guid> agentTaskIds, TreeNode<AgentTask> parentNode)
        {
            List<TreeNode<AgentTask>> allNodes = AgentTaskTree.GetAllNodes(parentNode);
            List<TreeNode<AgentTask>> result = new List<TreeNode<AgentTask>>();

            foreach (Guid newNodeId in agentTaskIds)
            {
                foreach (TreeNode<AgentTask> node in allNodes)
                {
                    if (newNodeId == node.Data.Id)
                    {
                        result.Add(node);
                        break;
                    }

                }
            }


            return result;
        }

        static Tree<AgentTask> BuildAgentTaskTreeFromOutline(string globalPrompt, string outline, out Dictionary<Guid, TreeNode<AgentTask>> newNodes, TreeNode<AgentTask> parent = null, Tree<AgentTask> currentTree = null)
        {
            Tree<AgentTask> tree = new Tree<AgentTask>(new AgentTask(globalPrompt, "root", null));
            newNodes = new Dictionary<Guid, TreeNode<AgentTask>>();

            if (currentTree != null)
                tree = currentTree;

            string[] sections = null;
            string[] sections1 = outline.Split("<section>");
            string[] sections2 = outline.Split("\n\n");

            if (sections1.Length > sections2.Length)
            {
                sections = sections1;
            }
            else
            {
                sections = sections2;
            }

            List<string> cleanedSections = new List<string>();

            foreach(var cursor in sections)
            {
                string line = null;

                if(cursor.Contains("<section>"))
                {
                    line = cursor.Replace("<section>", string.Empty);

                    if (!string.IsNullOrEmpty(line) && line.Length > 3)
                        cleanedSections.Add(line);
                }
                else if (cursor.Contains("\n\n") && cursor.Length > 3)
                {
                    line = cursor.Replace("\n\n", string.Empty);

                    if(!string.IsNullOrEmpty(line))
                        cleanedSections.Add(line);
                }
                else
                {
                    if(!string.IsNullOrEmpty(cursor))
                        cleanedSections.Add(cursor);
                }

                
            }

            int index = 0;
            int maxDepthCurrentOutline = 0;

            foreach (string section in cleanedSections)
            {
                TreeNode<AgentTask> sectionNode = tree.GetRoot();//tree.getroot()

                if (parent != null)
                {
                    sectionNode = parent;
                }

                string[] lines = section.Split('\n');

                TreeNode<AgentTask> nextLevel = null;

                for (int i = 0; i < lines.Length; i++)
                {
                    string trimmedLine = lines[i].Trim();

                    if(trimmedLine.Length == 1) continue;

                    int d = trimmedLine.IndexOf(' ');
                    string[] parts = trimmedLine.Split(" ");
                    string[] levelParts = parts[0].Split('.');
                    List<int> sectionValueArray = new List<int>();
                    string sectionNumber = RemoveSpecialCharacters(levelParts[0]);

                    bool isNumber = int.TryParse(sectionNumber, out _);

                    if (!isNumber)
                        continue;

                    for (int j = 0; j < levelParts.Length; j++)
                    {
                        if (!string.IsNullOrEmpty(levelParts[j]))
                        {
                            string sectionPart= RemoveSpecialCharacters(levelParts[j]);
                            sectionValueArray.Add(int.Parse(sectionPart));
                        }
                            
                    }

                    //Get max tree depth
                    if (sectionValueArray.Count > maxDepthCurrentOutline)
                        maxDepthCurrentOutline = sectionValueArray.Count;

                    string title = RemoveNumbers( trimmedLine.Replace(parts[0], string.Empty).Trim());//RemoveSpecialCharacters(
                    AgentTask task = new AgentTask(title, title, sectionNode.Data.ContextHistory, RemoveSpecialCharacters(parts[0]), index);//parent.Data.ContextHistory
                    //task.ContextHistory.Add(parent.Data.Prompt);
                    task.ExpertPrompt = sectionNode.Data.ExpertPrompt;//Parent

                    if (sectionValueArray.Count == 1 || sectionValueArray[sectionValueArray.Count - 1] == 0 && sectionValueArray.Count == 2)
                    {
                        sectionNode = tree.AddChild(sectionNode, task);
                        Tree<AgentTask>.UpdateSectionNumbers(parent);
                        newNodes.Add(sectionNode.Data.Id, sectionNode);
                    }
                    else
                    {
                        nextLevel = tree.AddChild(sectionNode, task);
                        Tree<AgentTask>.UpdateSectionNumbers(parent);
                        newNodes.Add(nextLevel.Data.Id, nextLevel);
                    }
                    index++;
                }

                
            }

            return tree;
        }

        static string RemoveNumbers(string input)
        {
            // Remove numbers using regular expression
            string pattern = @"\d";
            string replacement = "";
            string result = Regex.Replace(input, pattern, replacement);

            return result;
        }

        static string RemoveSpecialCharacters(string input)
        {
            // Remove special characters excluding spaces and dots using regular expression
            string pattern = "[^a-zA-Z0-9 .]";
            string result = Regex.Replace(input, pattern, "");

            return result;
        }

        private static List<ChatExample> GetContainsRevisedPromptExamples()
        {
            List<ChatExample> result = new List<ChatExample>();

            result.Add(new ChatExample() { Sample = "Revised prompt: ", Result = "true" });
            //result.Add(new ChatExample() { Sample = "Thank you so much! This prompt works well for me. I appreciate your help and look forward to creating some delicious and healthy recipes. Have a great day!", Result = "true" });
            //result.Add(new ChatExample() { Sample = "Great, glad to hear that the prompt is revised to your liking! If you have any further questions or if there's anything else I can assist you with, just let me know.", Result = "true" });
            //result.Add(new ChatExample() { Sample = "Thank you, I appreciate it! Have a great day!", Result = "true" });

            return result;
        }

        public string CreateExpertPrompt(List<ConversationMessage> taskDescription, int numberOfRoles, int numberOfSkills)
        {
            StringBuilder builder = new StringBuilder();

            foreach(var cursor in taskDescription)
            {
                builder.AppendLine(cursor.Speaker.ToString() + ":" + cursor.Message);
            }

            string expertSystem = AgentPromptBase[PromptTypeEnum.EXPERT_CAPTURE].Value.Replace("@ROLE", numberOfRoles.ToString());
            expertSystem = expertSystem.Replace("@SKILL", numberOfSkills.ToString());
            expertSystem = expertSystem.Replace("@TASK", builder.ToString());

            string expertPromptDefinition = ExecuteSingleChatRequestWithConversation(null, expertSystem);
            string expertTaskExtra = AgentPromptBase[PromptTypeEnum.EXPERT_ANSWER_QUESTION_PROMPT].Value;//In your response, include your reponse to suggestions and questions in the prompt.
            string result = expertPromptDefinition + expertTaskExtra;

            return result;
        }

        public string CreateExpertPrompt(string taskDescription, int numberOfRoles, int numberOfSkills)
        {
            string expertSystem = AgentPromptBase[PromptTypeEnum.EXPERT_CAPTURE].Value.Replace("@ROLE", numberOfRoles.ToString());
            expertSystem = expertSystem.Replace("@SKILL", numberOfSkills.ToString());
            expertSystem = expertSystem.Replace("@TASK", taskDescription);

            string result = ExecuteSingleChatRequestWithConversation(null, expertSystem);
            

            return result;
        }

        public void InitializeAgent(string taskDescription, int expertNumberOfRoles, int expertNumberOfSkills)
        {
            conversationTexts = new Dictionary<RoleEnum, string>();

            agentTask = taskDescription;
            ExpertRoleCount = expertNumberOfRoles;
            ExpertSkillCount = expertNumberOfSkills;
            AgentConversations = new Dictionary<RoleEnum, Conversation>();

            //Initialize Expert Define agent
            if (!AgentConversations.ContainsKey(RoleEnum.ExpertDefine))
            {
                if (string.IsNullOrEmpty(ExpertBasePrompt))
                {
                    ExpertBasePrompt = CreateExpertPrompt(taskDescription, expertNumberOfRoles, expertNumberOfSkills);
                }

                AgentConversations.Add(RoleEnum.ExpertDefine, api.Chat.CreateConversation(new ChatRequest() { Model = ChatGPTModelType }));

                AgentConversations[RoleEnum.ExpertDefine].AppendUserInput(ExpertBasePrompt);
            }

            //Initialize Expert agent
            if (!AgentConversations.ContainsKey(RoleEnum.Expert))
            {
                conversationTexts = new Dictionary<RoleEnum, string>();
                //conversationTexts.Add(EXPERT_KEY, taskDescription);
                AgentConversations.Add(RoleEnum.Expert, api.Chat.CreateConversation(new ChatRequest() { Model = ChatGPTModelType }));
                AgentConversations[RoleEnum.Expert].AppendUserInput(taskDescription);
                // give instruction as System
                AgentConversations[RoleEnum.Expert].AppendSystemMessage(ExpertBasePrompt);
            }

            //Initialize Worker agent
            if (!AgentConversations.ContainsKey(RoleEnum.PromptGenerator))
            {
                conversationTexts.Add(RoleEnum.PromptGenerator, taskDescription);
                AgentConversations.Add(RoleEnum.PromptGenerator, api.Chat.CreateConversation(new ChatRequest() { Model = ChatGPTModelType }));
                AgentConversations[RoleEnum.PromptGenerator].AppendUserInput(taskDescription);
                // give instruction as System
                AgentConversations[RoleEnum.PromptGenerator].AppendSystemMessage(AgentPromptBase[PromptTypeEnum.PROMPT_GENERATOR].Value);
            }

        }

        public string? GetRevisedCommand(string inputText)
        {
            string startString = "revised prompt";
            string endString = "suggestions";
            string[] quotes = inputText.ToLower().Split(':');
            int startIndex = inputText.ToLower().IndexOf(startString);
            int endIndex = inputText.ToLower().IndexOf(endString);

            for (int i = 0; i < quotes.Length; i++)
            {
                if (quotes[i].Contains(startString))
                {
                    string result = quotes[i + 1].Replace(endString, string.Empty).Trim();
                    return result;
                }
            }

            return null;
        }

        public List<TreeNode<AgentTask>> ExecutePromptSequence(List<int> batch, List<TreeNode<AgentTask>> children)
        {
            ConsoleColor consoleColor = ConsoleColor.Gray;
            List<int> tasksToRemove = new List<int>();
            string converationContextKeyWord = "Task context: ";
            string userFeedBackKeyWord = "User feedback about the desired result: ";
            string resultStateKeyWord = "Current result content: ";

            foreach (int i in batch)
            {
                //For the [item] expand it for the following [prompt].  [item] = @SECTION, [prompt] = @TEXT
                string input = AgentPromptBase[PromptTypeEnum.EXPAND_SECTION].Value.Replace("@SECTION", children[i].Data.Section);
                List<string> contextAndInput = new List<string>();

                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("Executing Section: " + children[i].Data.SectionNumber + " " + children[i].Data.Section);

                children[i].Data.Result = ExecuteSingleChatRequestInputList(input, children[i].Data.ExpertPrompt, children[i].Data.ConversationMessages);
               

                //write to console
                Console.WriteLine();
                double percentDone = i / (double)children.Count * 100;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Percent Done: " + Math.Round(percentDone, 2));
                Console.WriteLine();
            }

            return children;
        }

        public static List<List<int>> CreateBatches(int listLength, int batchSize)
        {
            List<List<int>> batches = new List<List<int>>();

            for (int i = 0; i < listLength; i += batchSize)
            {
                List<int> batch = new List<int>();

                for (int j = i; j < Math.Min(i + batchSize, listLength); j++)
                {
                    batch.Add(j);
                }

                batches.Add(batch);
            }

            return batches;
        }

        public void Save(string filePath)
        {
            using (FileStream fileStream = new FileStream(filePath, FileMode.Create))
            {
                BinaryFormatter binaryFormatter = new BinaryFormatter();
                binaryFormatter.Serialize(fileStream, this);
            }
        }

        public static ReflectiveAgent Load(string filePath)
        {
            ReflectiveAgent result = null;

            using (FileStream fileStream = new FileStream(filePath, FileMode.Open))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                result = (ReflectiveAgent)formatter.Deserialize(fileStream);
            }

            return result;
        }

        public string GetUserAction()
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("Please select an action:");
            Console.WriteLine("1. Finished with Section");
            Console.WriteLine("2. Expand Section More");
            Console.WriteLine("3. Expand Section More with user input");
            Console.WriteLine("4. Save Model state and exit");

            string input = Console.ReadLine();
            string selectedAction;

            switch (input)
            {
                case "1":
                    selectedAction = "1";
                    break;

                case "2":
                    selectedAction = "2";
                    break;

                case "3":
                    selectedAction = "3";
                    break;

                case "4":
                    selectedAction = "4";
                    break;

                default:
                    Console.WriteLine("Invalid input. Please try again.");
                    selectedAction = GetUserAction(); // Recursive call to prompt for valid input
                    break;
            }

            return selectedAction;
        }

        //Use in the future.  with perhaps lower version of chatgpt
        public bool IsSynonymOfDone(string input)
        {

            string response = ExecuteSingleChatRequestWithConversation(null, AgentPromptBase[PromptTypeEnum.IS_DONE].Value.Replace("@INPUT", input));

            string answer = response.Trim();

            if (answer.ToLower().Contains("yes", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            else if (answer.ToLower().Contains("no", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            else
            {
                throw new InvalidOperationException("Invalid response from ChatGPT.");
            }
        }

        //Use in the future.  with perhaps lower version of chatgpt
        public bool IsSynonymOfDelete(string input)
        {

            string response = ExecuteSingleChatRequestWithConversation(null, AgentPromptBase[PromptTypeEnum.IS_DELETE_STATEMENT].Value.Replace("@INPUT", input));

            string answer = response.Trim();

            if (answer.ToLower().Equals("yes", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            else if (answer.ToLower().Equals("no", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            else
            {
                throw new InvalidOperationException("Invalid response from ChatGPT.");
            }
        }

        public bool IsComplexPrompt(TreeNode<AgentTask> agentTask, int minActionThreshold, int mindependencyThreshold, int mincontingencyThreshold)
        {
            StringBuilder taskString = new StringBuilder();
            List<ConversationMessage> messages = GetItemsFromEnd(agentTask.Data.ConversationMessages, 2);
            for(int i = 0; i < messages.Count; i++)
            {
                taskString.Append(i + ": " + messages[i].Speaker.ToString() + " " + messages[i].Message.ToString() + " ");
            }


            string combinedPrompt = AgentPromptBase[PromptTypeEnum.PROMPT_COMPLEXITY_ESTIMATE].Value.Replace("@TASK", taskString.ToString());//agentTask.Data.Prompt
            ConversationMessage prompt = new ConversationMessage() { Speaker = SpeakerEnum.user, Message = combinedPrompt};
            //I may want to update this to have the response indicate that it can do this in a single step or something like that
            string chatResult = ExecuteSingleChatRequestInputList(null, agentTask.Data.ExpertPrompt, new List<ConversationMessage>() { prompt });
            string[] chatRows = chatResult.Split("\n");
            int actionCount = -1;
            int dependenciesCount = -1;
            int contingenciesCount = -1;

            for (int i = 0; i < chatRows.Length; i++)
            {
                string[] rowItems = chatRows[i].Split("=");

                if(rowItems.Length == 2)
                {
                    string numbersOnly = RemoveNonNumbers(rowItems[1]);

                    if (rowItems[0].ToLower().Contains("actions"))
                    {
                        actionCount = int.Parse(numbersOnly);
                    }
                    else if (rowItems[0].ToLower().Contains("dependencies"))
                    {
                        dependenciesCount = int.Parse(numbersOnly); 
                    }
                    else if (rowItems[0].ToLower().Contains("contingencies"))
                    {
                        contingenciesCount = int.Parse(numbersOnly);
                    }
                }

                if(actionCount != -1 && dependenciesCount != -1 && contingenciesCount != -1)
                {
                    break;
                }

            }

            if (actionCount >= minActionThreshold && (dependenciesCount >= mindependencyThreshold || contingenciesCount >= mincontingencyThreshold))// || true
            {
                return true;
            }


            return false;
        }

        static string RemoveNonNumbers(string input)
        {
            // Remove non-numbers using regular expression
            string pattern = "[^0-9]";
            string result = Regex.Replace(input, pattern, "");

            return result;
        }

        static List<ConversationMessage> GetItemsFromEnd(List<ConversationMessage> list, int numberOfItems)
        {
            int count = list.Count;

            if (count <= numberOfItems)
            {
                return list;
            }

            List<ConversationMessage> result = new List<ConversationMessage>();

            for (int i = count - numberOfItems; i < count; i++)
            {
                result.Add(list[i]);
            }

            return result;
        }

        public string ExecuteSingleChatRequestInputList(string systemMessage, string role, List<ConversationMessage> input, List<ChatExample> trainingExamples = null)
        {
            Conversation conversation = api.Chat.CreateConversation(new ChatRequest() { Model = ChatGPTModelType });

            if (trainingExamples != null)
            {
                foreach (var example in trainingExamples)
                {
                    conversation.AppendUserInput(example.Sample);
                    conversation.AppendExampleChatbotOutput(example.Result);
                }
            }

            StringBuilder systemMessageBuilder = new StringBuilder();

            //Add the role message
            if (!string.IsNullOrEmpty(role))
                systemMessageBuilder.Append(role + ".  ");

            //Set the system message
            if (!string.IsNullOrEmpty(systemMessage))
                systemMessageBuilder.Append(systemMessage);

            conversation.AppendSystemMessage(systemMessageBuilder.ToString());

            foreach (var item in input)
            {
                conversation.AppendMessage(new ChatMessage()
                {
                    Content = item.Message,
                    Name = item.Speaker.ToString(),
                    Role = ChatMessageRole.FromString(item.Speaker.ToString())
                });
            }

            int currentCount = 0;

            while (true)
            {
                try
                {
                    Task<string> response = conversation.GetResponseFromChatbotAsync();
                    response.Wait();

                    if (response.Status == TaskStatus.RanToCompletion)
                    {
                        //HTTP status code: TooManyRequests
                        string result = response.Result;

                        UpdateOpenAPIUsage();
                        Thread.Sleep(10000);
                        return result;
                    }
                    else
                    {

                    }

                }
                catch (AggregateException ex)
                {
                    foreach (var innerException in ex.InnerExceptions)
                    {
                        if (innerException is TaskCanceledException)
                        {
                            Console.WriteLine("The task was canceled.");
                            // Handle the canceled task error here
                        }
                        else
                        {
                            Console.WriteLine("Unhandled exception: " + innerException.Message);
                        }
                    }
                }
                catch (HttpRequestException e)
                {
                    Console.WriteLine("\nException Caught!");
                    //Console.WriteLine("Message :{0} ", e.Message);

                    if (e.Message.Contains("system overload")) // Replace this with the specific error message you're seeing
                    {
                        if (currentCount >= MAX_API_TRIES)
                        {
                            Console.WriteLine("System Overload occurred too many times throwing exception now");
                            throw; // It's beyond our max try count
                        }

                        Console.WriteLine("System Overload. Waiting for 60 seconds before retrying.");
                        Thread.Sleep(60000); // Wait for 60 seconds
                    }
                    else
                    {
                        throw; // If it's not a 'system overload' exception, rethrow it
                    }

                    currentCount++;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Unhandled exception: " + ex.Message);
                }
            }

            return null;
        }

        
        private void UpdateOpenAPIUsage()
        {
            ApiCallCount++;

            OpenAIApiCallCount.Add(new APICallEvent() { Time = DateTime.Now });

            if (OpenAIApiCallCount.Count > 50)
            {
                OpenAIApiCallCount.RemoveAt(0);
            }

        }

        public string ExecuteSingleChatRequestWithConversation(string instruction, string input, Conversation existingConversation = null, List<ChatExample> trainingExamples = null)
        {
            Conversation conversation = api.Chat.CreateConversation(new ChatRequest() { Model = ChatGPTModelType });

            if(existingConversation != null)
            {
                conversation = existingConversation;
            }

            if (trainingExamples != null)
            {
                foreach (var example in trainingExamples)
                {
                    conversation.AppendUserInput(example.Sample);
                    conversation.AppendExampleChatbotOutput(example.Result);
                }
            }

            if (!string.IsNullOrEmpty(instruction))
            {
                conversation.AppendSystemMessage(instruction);
            }

            if (!string.IsNullOrEmpty(input))
            {
                conversation.AppendUserInput(input);
            }

            int currentCount = 0;

            while (true)
            {
                try
                {
                    Task<string> response = conversation.GetResponseFromChatbotAsync();
                    response.Wait();

                    if (response.Status == TaskStatus.RanToCompletion)
                    {
                        //HTTP status code: TooManyRequests
                        string result = response.Result;
                        UpdateOpenAPIUsage();
                        Thread.Sleep(10000);
                        return result;
                    }
                    else
                    {

                    }

                }
                catch (AggregateException ex)
                {
                    foreach (var innerException in ex.InnerExceptions)
                    {
                        if (innerException is TaskCanceledException)
                        {
                            Console.WriteLine("The task was canceled.");
                            // Handle the canceled task error here
                        }
                        else
                        {
                            Console.WriteLine("Unhandled exception: " + innerException.Message);
                        }
                    }

                    Thread.Sleep(60000);
                }
                catch (HttpRequestException e)
                {
                    Console.WriteLine("\nException Caught!");
                    Console.WriteLine("Message :{0} ", e.Message);

                    if (e.Message.Contains("system overload")) // Replace this with the specific error message you're seeing
                    {
                        if (currentCount >= MAX_API_TRIES)
                        {
                            Console.WriteLine("System Overload occurred too many times throwing exception now");
                            throw; // It's beyond our max try count
                        }

                        Console.WriteLine("System Overload. Waiting for 60 seconds before retrying.");
                        Thread.Sleep(60000); // Wait for 60 seconds
                    }
                    else
                    {
                        throw; // If it's not a 'system overload' exception, rethrow it
                    }

                    currentCount++;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Unhandled exception: " + ex.Message);
                }

            }

            return null;
        }

    }
}
