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
        public ConsoleCommandAttribute(string name, string help = null)
        {
            this.name = name;
            this.help = help;
        }
    }
}
