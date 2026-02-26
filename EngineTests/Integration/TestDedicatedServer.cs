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
    // Dedicated server cannot, at the moment, be parallelized due to its heavy reliance on global state (GlobalState class)
    [DoNotParallelize]
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
            runner.PauseUpdating();
            PostCommandAndWait(runner, "aaa");
            GlobalState.GameStateManager.TheIsland.waiterWorld.Wait();

            var waiter = runner.UpdateNTimes(1);
            waiter.Wait();
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

        private void PostCommandAndWait(HeadlessRunner runner, string command) 
        {
            var tracked = runner.PostCommand(command);
            tracked.waiter.Wait();
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
            Console.WriteLine("Clean up {0}", tempFolderName);
            Thread.Sleep(5 * 1000);
            Directory.Delete(tempFolderName, true);
        }
    }
}
