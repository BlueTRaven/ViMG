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

        private struct RunParams
        {
            public string[] args;
            public HeadlessRunner runner;
        }

        private string tempFolderName;

        [TestInitialize]
        public void Initialize()
        {
            Console.WriteLine("Test start {0}", DateTime.Now);
            Logger.SetAllLogLevels(Logger.LogLevel.Debug);

            tempFolderName = Path.GetTempPath() + "/vimg/saves/";
            WorldIO.SaveFolder = tempFolderName;

            // Clean up directory if it already exists - sometimes the case with errored-out tests
            if (Directory.Exists(tempFolderName))
                Directory.Delete(tempFolderName, true);
        }

        [TestMethod]
        public void TestRun()
        {
            using HeadlessRunner runner = new HeadlessRunner();
            RunParams p = new RunParams
            {
                runner = runner,
                args = [
                    "--defaultPort", "9050",
                    "--saveName", WORLD_NAME + Thread.CurrentThread.ManagedThreadId.ToString(),
                    "--createSave",
                    "--createLayer", "255",
                ],
            };
            Thread t = new(Run);
            t.Start(p);
            runner.PauseUpdating();
            runner.WaitUntilWorldLoaded();

            var waiter = runner.UpdateNTimes(1);
            waiter.Wait();
            GlobalState.Exit = true;

            Assert.IsTrue(t.Join(5 * 1000));
        }

        [TestMethod]
        public void TestAllGenerators()
        {
            using HeadlessRunner runner = new HeadlessRunner();
            int maxLayers = GlobalState.Registry.WorldLogicRegistry.maxLayers;

            for (int i = 0; i < maxLayers; i++)
            {
                GlobalState.Exit = false;

                if (GlobalState.Registry.WorldLogicRegistry.generators[i] != null)
                {
                    RunParams p = new RunParams
                    {
                        runner = runner,
                        args = [
                            "--defaultPort", "9050",
                            "--saveName", WORLD_NAME + Thread.CurrentThread.ManagedThreadId.ToString() + "_" + i.ToString(),
                            "--createSave",
                            "--createLayer", i.ToString(),
                       ],
                    };
                    Thread t = new(Run);
                    t.Start(p);
                    runner.PauseUpdating();
                    runner.WaitUntilWorldLoaded();

                    var waiter = runner.UpdateNTimes(1);
                    waiter.Wait();

                    GlobalState.Exit = true;
                    Assert.IsTrue(t.Join(5 * 1000));
                    runner.ResumeUpdating();
                }
            }
        }

        private void Run(object? o)
        {
            RunParams p = o as RunParams? ?? throw new Exception();
            GlobalState.Args.ParseArgs(p.args);
            p.runner.Run();
        }

        [TestCleanup]
        public void Cleanup()
        {
            Console.WriteLine("Clean up {0} {1} ", DateTime.Now, tempFolderName);
            Thread.Sleep(5 * 1000);
            Directory.Delete(tempFolderName, true);
        }
    }
}
