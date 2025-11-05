using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViMG.IMGUIImpl
{
    [AttributeUsage(AttributeTargets.Field)]
    public class ConsoleCommandVarAttribute : Attribute
    {
        public string name;
        public string description;
        public ConsoleCommandVarAttribute(string name, string description = "")
        {
            this.name = name;
            this.description = description;
        }
    }
}
