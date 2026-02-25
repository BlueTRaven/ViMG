using Engine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.UIs;

namespace EngineTests.Integration
{
    [TestClass]
    public sealed class TestDedicatedServer
    {
        private const string WORLD_NAME = "___test_world";
        [TestMethod]
        public void TestRun()
        {
            HeadlessRunner runner = new HeadlessRunner();
            Thread t = new Thread(Run);
            t.Start(runner);
            Thread.Sleep(5 * 1000);
            var tracked = runner.PostCommand("print hello");
            tracked.waiter.Wait();
            Console.WriteLine("From tracked command: {0}", tracked.output);
            GlobalState.Exit = true;

            if (!t.Join(5 * 1000))
            {
                Assert.Fail();
            }
            else
            {
                Assert.IsFalse(t.IsAlive);
            }
        }

        private void Run(object? o)
        {
            HeadlessRunner runner = o as HeadlessRunner ?? throw new Exception();
            GlobalState.Args.ParseArgs([
                "--defaultPort", "9050",
                "--saveName", WORLD_NAME + Thread.CurrentThread.ManagedThreadId.ToString(),
                "--createSave",
                ]);
            runner.Run();
        }

        [TestCleanup]
        public void Cleanup()
        {
            string[] paths = MenuMain.GetWorldSaveDirectoriesFull();
            foreach (string str in paths)
            {
                if (str.StartsWith(WORLD_NAME))
                {
                    File.Delete(str);
                }
            }
        }
    }
}
