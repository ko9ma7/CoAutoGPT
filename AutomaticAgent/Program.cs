using CollaborativeChatGPTLibrary;
using OpenAI_API;
using OpenAI_API.Chat;
using static System.Net.Mime.MediaTypeNames;
using System.ComponentModel;
using System.Text;
using System;
using OpenAI_API.Models;
using OpenAI_API.Moderation;
using System.Reflection;
using CollaborativeChatGPTLibrary.Classes;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Configuration;

namespace AutomaticAgent
{
    internal class Program
    {
        private static IConfiguration _configuration;
        private static string OPEN_AI_API_KEY = null;
        private static string _saveFileName = "agent_state.dat";
        
        static void Main(string[] args)
        {
            var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddUserSecrets<Program>();

           _configuration = builder.Build();

            // Now you can access your secrets
            OPEN_AI_API_KEY = _configuration["OPENAPIKEY"];

            string taskDescription = "create a 5 recipe book with only 3 chapters that includes the following conditions: 1. Need no more than 20 minutes to prepare, 2. Are low calorie, 3. Easy to prepare, 4. Cost less than $20 per meal. 5. The recipes should be a fusion of chinese and japanese food.  6. Use standard animal proteins'";
            //string taskDescription = "Im a seasoned programmer, I want to become a manager, can you recommend 5 linkedin courses for me";
            //string taskDescription = "what is the sum of 2 + 2";

            RunAgent(taskDescription);
        }

        private static void RunAgent(string taskDescription)
        {
            ReflectiveAgent agent = new ReflectiveAgent(OPEN_AI_API_KEY, Model.ChatGPTTurbo0301);
            agent.MaxEnrichmentDepth = 3;
            bool endWork = false;
            //bool loaded = false;
            int batchSize = 3;
            bool userEndedProcess = false;
            Console.ForegroundColor = ConsoleColor.White;

            //1. Set problem Description.
            agent.AgentTaskTree = new Tree<AgentTask>(new AgentTask(taskDescription, "root", null));
            agent.CurrentTask = agent.AgentTaskTree.GetRoot();
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("Initializing project expert...");
            agent.InitializeAgent(agent.CurrentTask.Data.Prompt, 3, 5);
            agent.CurrentTask.Data.ExpertPrompt = agent.ExpertBasePrompt;

            if (File.Exists(_saveFileName))//Disabled for now
            {
                ReflectiveAgent agent2 = ReflectiveAgent.Load(_saveFileName);
                agent2.InitializeOpenAIApi(OPEN_AI_API_KEY, Model.ChatGPTTurbo0301);
                agent = agent2;
                //loaded = true;
            }

            while (true)
            {
                bool isComplexTask = false;
                List<string> enrichedPromptHistory = new List<string>();

                if (!agent.UserShutdown)
                {
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine("Current Task:");
                    Console.ForegroundColor = ConsoleColor.Gray;
                    string task = agent.CurrentTask.Data.Prompt;//For the root

                    if (!string.IsNullOrEmpty(agent.CurrentTask.Data.SectionNumber))
                    {
                        task = agent.CurrentTask.Data.SectionNumber + " " + agent.CurrentTask.Data.Section;
                    }

                    Console.WriteLine(task);
                    Console.WriteLine();

                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine("Determining Task Complexity");
                    isComplexTask = agent.IsComplexPrompt(agent.CurrentTask, 20, 3, 1);//25, 10, 5This is in the 95% range of successful single response range of GPT
                    


                    if (isComplexTask)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("Task is considered complex, beginning prompt creation");

                        //2. Create Top level role
                        if (string.IsNullOrEmpty(agent.CurrentTask.Data.ExpertPrompt))
                        {
                            Console.ForegroundColor = ConsoleColor.White;
                            Console.WriteLine("Building Your Expert for the Task");
                            agent.CurrentTask.Data.ExpertPrompt = agent.CreateExpertPrompt(agent.CurrentTask.Data.ConversationMessages, 2, 3);
                            Console.WriteLine();
                        }


                        Console.WriteLine("Expert Definition:");
                        Console.ForegroundColor = ConsoleColor.Gray;
                        Console.WriteLine(agent.CurrentTask.Data.ExpertPrompt);
                        //agent.CurrentTask.Data.ExpertPrompt = agent.ExpertSystemDefinition;
                        Console.WriteLine();

                        //3. Refine problem Requirements
                        enrichedPromptHistory = agent.EnrichQuery(agent.CurrentTask, agent.MaxEnrichmentDepth);
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("Task is low complexity, generating response directly");
                        agent.CurrentTask.Data.Result = agent.ExecuteSingleChatRequestInputList(agent.CurrentTask.Data.Result, null, agent.CurrentTask.Data.ConversationMessages);
                        //agent.CurrentTask.Data.ConversationMessages.Add(new ConversationMessage() { Speaker = SpeakerEnum.assistant, Message = null });
                        agent.CurrentTask.Data.IsComplete = true;
                        //enrichedPromptHistory.Add(agent.CurrentTask.Data.Result);
                        agent.CompletedTasks.Add(agent.CurrentTask.Data);
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.WriteLine("AI Result");
                        Console.ForegroundColor = ConsoleColor.Gray;
                        Console.WriteLine(agent.CurrentTask.Data.Result);
                    }
                }

                if (isComplexTask && !agent.UserShutdown)
                {
                    //4. Breakdown the prompt into set of tasks that are stored in a data tree
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine();
                    Console.WriteLine("Building Task Tree");
                    Dictionary<Guid, TreeNode<AgentTask>> addedNodes = null;
                    List<TreeNode<AgentTask>> tasks = agent.AgentTaskTree.GetAllNodes(agent.CurrentTask);

                    agent.AgentTaskTree = agent.UpdateTaskTree(agent.AgentTaskTree, agent.CurrentTask, enrichedPromptHistory[enrichedPromptHistory.Count - 1], out addedNodes);


                    DisplayTaskList(agent, agent.CurrentTask);

                    //5. Ask for User input to confirm tasks (to remove)
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine();
                    Console.WriteLine("Please select an item by number and enter 'delete' or 'change:' + NEW_TITLE. Such as Delete item, or Change Title text.  Type 'done' or 'd' to finish.");
                    Console.WriteLine("Enter '1' for Item 1, '2' for Item 2, and '3' for Item 3.");

                    GetUserInputForTasks(tasks);

                    //6. Update the Task Outline/Breakdown based on user input
                    ApplyTaskTreeUpdates(agent, agent.CurrentTask);

                    List<TreeNode<AgentTask>> newNodeValues = agent.GetNodesById(new List<Guid>(addedNodes.Keys), agent.CurrentTask);

                    endWork = RunPromptBatchs(agent, endWork, _saveFileName, batchSize, newNodeValues);

                    //Flag if user stopped work
                    if (endWork)
                    {
                        userEndedProcess = true;
                    }
                }
                else if (agent.UserShutdown)
                {
                    agent.UserShutdown = false;

                    //if (SetNextActiveTask(agent))
                    //{
                    //    break;//Means we're done with work because no next not complete node available
                    //}

                    endWork = RunPromptBatchs(agent, endWork, _saveFileName, batchSize, agent.GetIncompleteTasks(agent.CurrentTask));

                    //Flag if user stopped work
                    if (endWork)
                    {
                        userEndedProcess = true;
                    }
                }

                Console.WriteLine();

                bool tasksComplete = agent.AllTasksComplete();

                if (tasksComplete)
                {
                    var root = agent.AgentTaskTree.GetRoot();
                    root.Data.IsComplete = true;

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("All Tasks Complete.");
                    endWork = true;
                    break;
                }
                else if(endWork)
                {
                    break;
                }

                //We select the first node in Tree that is not complete
                //this needs to use the newnodes list to display list  nee
                //DisplayTaskList(agent, agent.CurrentTask);

                //Select next taskNode to work on
                //Get the node that the user wants to work on next
     
                SetNextActiveTask(agent);
            }

            if (endWork)
            {
                agent.UserShutdown = userEndedProcess;
                Console.ForegroundColor = ConsoleColor.White;
                agent.Save(_saveFileName);
                Console.WriteLine("Exiting program, current state saved.");
                Console.WriteLine("Press any key to close");
                Console.ReadLine();
                return;
            }

            //8. Create Draft
            //StringBuilder draft = new StringBuilder();
            //TaskDraft taskDraft = new TaskDraft();

            //Console.ForegroundColor = ConsoleColor.White;
            //Console.WriteLine("Putting the Draft Together");
            //for (int i = 0; i < taskNodes.Count; i++)
            //{
            //    draft.Append(taskNodes[i].Result);
            //}

            //taskDraft.Draft = draft.ToString();
            //taskDraft.TaskAgents = taskNodes;

            //File.WriteAllText(saveFileName, taskDraft.Draft);


            //ALOGORITHM STEPS
            //1. Get problem Description. Optional Set working directory for files
            //2. Create Top level role
            //3. Refine problem Requirements
            //3.1 All USER input to edit
            //4. Construct sequence of tasks
            //5. Ask for User input to confirm tasks (to remove)
            //6. Build agent task chain
            //7. Execute Tasks
            //8. Create Draft
            //9. Review with User and get acceptance

        }

        private static bool RunPromptBatchs(ReflectiveAgent agent, bool endWork, string saveFileName, int executionModulus, List<TreeNode<AgentTask>> newNodeValues)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("Processing Agent Tasks");
            Console.WriteLine();

            //7. Execute Tasks
            List<List<int>> batches = ReflectiveAgent.CreateBatches(newNodeValues.Count, executionModulus);

            foreach (var batch in batches)
            {
                agent.ExecutePromptSequence(batch, newNodeValues);
                agent.Save(saveFileName);

                //Perform review of outputs from batch
                ConsoleColor consoleColor = ConsoleColor.Gray;

                foreach (var item in batch)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("Section: " + newNodeValues[item].Data.SectionNumber + " " + newNodeValues[item].Data.Section);
                    Console.WriteLine();

                    Console.ForegroundColor = consoleColor;
                    Console.WriteLine(newNodeValues[item].Data.Result);

                    //Switch the color for each entry
                    if (consoleColor == ConsoleColor.Gray)
                    {
                        consoleColor = ConsoleColor.White;
                    }
                    else
                    {
                        consoleColor = ConsoleColor.Gray;
                    }

                    //Take User Input here: Finished, Expand, Retry, Retry with input
                    string selectedAction = agent.GetUserAction();

                    if (selectedAction.Equals("1"))
                    {
                        //This section is done
                        newNodeValues[item].Data.IsComplete = true;
                    }
                    else if (selectedAction.Equals("2"))
                    {
                        newNodeValues[item].Data.IsComplete = false;
                    }
                    else if (selectedAction.Equals("4"))
                    {
                        endWork = true;
                        break;
                    }
                    else
                    {
                        Console.WriteLine("Please provide your input:");
                        string userInput = Console.ReadLine();

                        //Agent expand, use prompt enrichement with user input
                        if (!string.IsNullOrEmpty(newNodeValues[item].Data.Prompt))
                        {
                            //newNodeValues[item].Data.ContextHistory.Add(newNodeValues[item].Data.Prompt);
                        }

                        newNodeValues[item].Data.ConversationMessages.Add(new ConversationMessage() { Speaker = SpeakerEnum.user, Message = userInput });
                        newNodeValues[item].Data.UserPrompt.Add(userInput);
                        newNodeValues[item].Data.IsComplete = false;
                    }
                }

                if (endWork)
                {
                    agent.Save(saveFileName);
                    break;
                }
            }

            return endWork;
        }

        private static bool SetNextActiveTask(ReflectiveAgent agent)
        {
            var incompleteTasks = agent.GetIncompleteTasks(agent.CurrentTask);

            if (incompleteTasks.Count > 0)
            {
                //Console.ForegroundColor = ConsoleColor.White;
                //Console.WriteLine("Selecting next AgentTask");
                //Select next treenode agenttask to work on
                agent.CurrentTask = incompleteTasks[0];

                //Console.ForegroundColor = ConsoleColor.Gray;
                //Console.WriteLine("Next task: " + agent.CurrentTask.Data.SectionNumber + " " + agent.CurrentTask.Data.Section);

                return false;//We're not done
            }
            
            return true;//We're done and can stop
        }

        private static void ApplyTaskTreeUpdates(ReflectiveAgent agent, TreeNode<AgentTask> currentTask)
        {
            List<TreeNode<AgentTask>> tasks = agent.AgentTaskTree.GetAllNodes(currentTask);

            //Review changes and add to remove list if to be removed
            for (int i = 0; i < tasks.Count; i++)
            {
                if (!string.IsNullOrEmpty(tasks[i].Data.SectionUpdate) && tasks[i].Data.SectionUpdate.ToLower().Contains("delete"))
                {
                    agent.AgentTaskTree.RemoveNode(tasks[i]);
                }
            }

            for (int i = 0; i < tasks.Count; i++)
            {
                if (!string.IsNullOrEmpty(tasks[i].Data.SectionUpdate) && tasks[i].Data.SectionUpdate.ToLower().Contains("change"))
                {
                    string[] newText = tasks[i].Data.SectionUpdate.Split(':');

                    if(newText.Length > 1)
                    {
                        tasks[i].Data.Section = newText[1];
                        tasks[i].Data.Prompt = newText[1];
                    }

                    tasks[i].Data.SectionUpdate = null;
                }
            }
        }

        private static void DisplayTaskList(ReflectiveAgent agent, TreeNode<AgentTask> currentTask)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("Task Outline:");

            Console.ForegroundColor = ConsoleColor.Gray;
            List<TreeNode<AgentTask>> tasks = agent.AgentTaskTree.GetAllNodes(currentTask);

            int index = 0;

            for (int i = 1; i < tasks.Count; i++)
            {
                //Relabel index values
                tasks[i].Data.Index = i;

                if (tasks[i].Data.IsComplete)
                {
                    Console.ForegroundColor= ConsoleColor.Green;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.White;
                }

                Console.WriteLine("[" + tasks[i].Data.Index + "] " + tasks[i].Data.SectionNumber + " " + tasks[i].Data.Section);

            }

            Console.WriteLine();
        }

        private static void GetUserInputForTasks(List<TreeNode<AgentTask>> children)
        {
            string commandInput = "";
            while (commandInput != "done" && commandInput != "d")
            {
                Console.Write("Enter item number and description: ");
                commandInput = Console.ReadLine();

                // Split the input into two parts: the item number and the description
                string[] parts = commandInput.Split(' ', 2);

                // If the user entered 'done' or 'd', exit the loop
                if (parts[0] == "done" || parts[0] == "d")
                {
                    break;
                }

                // Parse the item number
                if (!int.TryParse(parts[0], out int itemNumber))
                {
                    Console.WriteLine("Invalid input. Please enter a valid item number and description.");
                    continue;
                }

                //if (itemNumber > children.Count - 1)
                //{
                //    Console.WriteLine("Invalid input. Please enter a valid item number from 0 to " + (children.Count - 1));
                //    continue;
                //}

                // Switch statement to process selected item
                children[itemNumber].Data.SectionUpdate = parts[1];

            }

        }

    }
}