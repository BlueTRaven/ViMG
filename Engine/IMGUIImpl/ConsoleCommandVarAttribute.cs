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
        // sync'd between client and server.
        // Server will sync this command to any clients connecting.
        // Server admin clients can send commands to servers.
        public bool sync; 
        public ConsoleCommandVarAttribute(string name, string description = "", bool sync = false)
        {
            this.name = name;
            this.description = description;
        }
    }
}
