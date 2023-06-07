using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CollaborativeChatGPTLibrary.Classes
{
    [Serializable]
    public class AgentTask : ICloneable
    {

        public AgentTask(string prompt, string section, List<string> contextHistory, string sectionNumber = null, int index = -1)
        {
            Prompt = prompt;
            Section = section;
            IsComplete = false;
            SectionNumber = sectionNumber;
            Index = index;
            ContextHistory = new List<string>();

            if(contextHistory != null )
            {
                ContextHistory = contextHistory.ToList();
            }

            ContextHistory.Add(prompt);
            ConversationMessages.Add(new ConversationMessage() { Speaker = SpeakerEnum.user, Message = prompt });

            if (sectionNumber != null)
            {
                string[] levelParts = sectionNumber.Split('.');
                SectionParts = new List<int>();

                for (int i = 0; i < levelParts.Length; i++)
                {
                    if (!string.IsNullOrEmpty(levelParts[i]))
                    {
                        string line = RemoveSpecialCharacters(levelParts[i]);
                        SectionParts.Add(int.Parse(line));
                    }
                        
                }
            }

        }

        public Guid Id { get; set; } = Guid.NewGuid();

        public int MaxContextWindowSize { get; set; } = 20;

        public string SectionUpdate { get; set; }

        public string Outline { get; set; }

        public int Index { get; set; } = -1;

        public List<ConversationMessage> ConversationMessages { get; set; } = new List<ConversationMessage>();

        public List<int> SectionParts { get; set; } = new List<int>();

        public List<string> UserPrompt { get; set; } = new List<string>();

        public string Prompt { get; set; }

        public string ExpertPrompt { get; set; }

        public string Section { get; set; }

        public string SectionNumber { get; set; }

        public string FileName { get; set; }

        public List<string> Tools { get; set; } = new List<string>();

        public string Context { get; set; }

        public List<string> ContextHistory { get; set; } = new List<string> { };

        public bool IsComplete { get; set; }

        //public bool NeedsReview { get; set; }

        public string Result { get; set; }

        //[field: NonSerialized]
        public Guid UnitTestId { get; set; }

        public object Clone()
        {
            // Here we use the serialization to perform a deep copy.
            var formatter = new BinaryFormatter();
            using var stream = new MemoryStream();
            formatter.Serialize(stream, this);
            stream.Seek(0, SeekOrigin.Begin);
            return formatter.Deserialize(stream);
        }

        static string RemoveSpecialCharacters(string input)
        {
            // Remove special characters using regular expression
            string pattern = "[^a-zA-Z0-9]";
            string replacement = "";
            string result = Regex.Replace(input, pattern, replacement);

            return result;
        }
    }
}
