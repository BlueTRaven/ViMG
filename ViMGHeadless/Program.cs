using Engine;

namespace ViMGHeadless
{
    internal class Program
    {
        static void Main(string[] args)
        {
            GlobalState.Args.ParseArgs(args);
            HeadlessRunner runner = new HeadlessRunner();
            runner.Run();
        }
    }
}
