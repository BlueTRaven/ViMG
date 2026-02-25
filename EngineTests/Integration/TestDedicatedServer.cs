using Engine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG;
using ViMG.UIs;

namespace EngineTests.Integration
{
    [TestClass]
    public sealed class TestDedicatedServer
    {
        private const string WORLD_NAME = "___test_world";

        private string tempFolderName;

        [TestInitialize]
        public void Initialize()
        {
            tempFolderName = Path.GetTempPath() + "/saves/";
            WorldIO.SaveFolder = tempFolderName;
        }

        [TestMethod]
        public void TestRun()
        {
            Logger.SetAllLogLevels(Logger.LogLevel.Debug);
            HeadlessRunner runner = new HeadlessRunner();
            Thread t = new Thread(Run);
            t.Start(runner);
            var tracked = runner.PostCommand("print hello");
            tracked.waiter.Wait();
            Console.WriteLine("From tracked command: {0}", tracked.output);
            GlobalState.GameStateManager.TheIsland.waiterWorld.Wait();
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
            Directory.Delete(tempFolderName, true);
        }
    }
}
