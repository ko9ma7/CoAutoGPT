using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CollaborativeChatGPTLibrary.Classes
{
    [Serializable]
    public class Section
    {
        /// <summary>
        /// The section number of Section which is formatted {#.#.#.#}
        /// </summary>
        public string SectionNumber { get; set; }

        /// <summary>
        /// The Title of the Section
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Collection of Subsections under the Section
        /// </summary>
        public List<Section> SubSections { get; set; }

        public override string ToString()
        {
            // Serialize this object to a JSON string.
            return JsonConvert.SerializeObject(this, Newtonsoft.Json.Formatting.Indented);
        }

        public static Section FromString(string sectionString)
        {
            // Deserialize the JSON string to a Section object.
            return JsonConvert.DeserializeObject<Section>(sectionString);
        }
    }
}
