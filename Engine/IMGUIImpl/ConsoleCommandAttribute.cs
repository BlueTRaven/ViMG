using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViMG.IMGUIImpl
{
    public enum ConsoleCommandRunSide
    {
        // Only run locally.
        Client,
        // Only run on the server. Requires admin.
        Server,
        // Run on both the client and server. Requires admin. Run on the server first, validated, then sent to the client.
        ServerAndClient,
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class ConsoleCommandAttribute : Attribute
    {
        public readonly string name;
        public readonly string help;
        // Side on which the command gets executed.
        public readonly ConsoleCommandRunSide executionSide;
        // Side on which it is valid to input this command.
        public readonly ConsoleCommandRunSide consoleSide;

        public ConsoleCommandAttribute(string name, string? help = null, ConsoleCommandRunSide executionSide = ConsoleCommandRunSide.Client, ConsoleCommandRunSide consoleSide = ConsoleCommandRunSide.Client)
        {
            this.name = name;
            this.help = help ?? "";
            this.executionSide = executionSide;
            this.consoleSide = consoleSide;
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
