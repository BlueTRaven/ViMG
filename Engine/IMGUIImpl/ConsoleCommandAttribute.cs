using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViMG.IMGUIImpl
{
    [AttributeUsage(AttributeTargets.Method)]
    public class ConsoleCommandAttribute : Attribute
    {
        public string name;
        public string help;

        public ConsoleCommandAttribute(string name, string? help = null)
        {
            this.name = name;
            this.help = help ?? "";
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class ConsoleCommandArgAttribute : Attribute
    {
        public string name;
        public string description;
        public bool optional;

        public ConsoleCommandArgAttribute(string name, string? description = null, bool optional = false)
        {
            this.name = name;
            this.description = description ?? "";
            this.optional = optional;
        }
    }
}
