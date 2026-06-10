using Engine;
using System.Diagnostics;

namespace ViMGHeadless
{
    internal class Program
    {
        private const string WORLD_NAME = "___test_world";

        static void Main(string[] args)
        {
            GlobalState.Args.ParseArgs(args);
            Logger.SetAllLogLevels(Logger.LogLevel.Debug);
            HeadlessRunner runner = new HeadlessRunner();
            runner.Run();
        }
    }
}
