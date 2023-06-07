using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CollaborativeChatGPTLibrary.Formatting
{
    [Serializable]
    public class Outline
    {
        public string title { get; set; }
        public List<Section> sections { get; set; }
    }

    [Serializable]
    public class Section
    {
        public string section_number { get; set; }
        public string section_title { get; set; }
        public List<Subsection> subsections { get; set; }
    }

    [Serializable]
    public class Subsection
    {
        public string subsection_number { get; set; }
        public string subsection_title { get; set; }
    }
}
