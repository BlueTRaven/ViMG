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
            //HeadlessRunner runner = new HeadlessRunner();
            //Thread t = new Thread(Run);
            //t.Start(runner);
            //var tracked = runner.PostCommand("print hello");
            //tracked.waiter.Wait();
            //Console.WriteLine("From tracked command: {0}", tracked.output);
            //GlobalState.GameStateManager.TheIsland.waiterWorld.Wait();
            //GlobalState.Exit = true;

            //if (!t.Join(5 * 1000))
            //{
            //    Debug.Assert(false);
            //}
            //else
            //{
            //    Debug.Assert(!t.IsAlive);
            //}
        }

        private static void Run(object? o)
        {
            HeadlessRunner runner = o as HeadlessRunner ?? throw new Exception();
            GlobalState.Args.ParseArgs([
                "--defaultPort", "9050",
                "--saveName", WORLD_NAME + Thread.CurrentThread.ManagedThreadId.ToString(),
                "--createSave",
                ]);
            runner.Run();
        }
    }
}
