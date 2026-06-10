using Engine;
using Engine.Entities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG;
using ViMG.Entities;
using ViMG.UIs;

namespace EngineTests.Integration
{
    [TestClass]
    // Dedicated server cannot, at the moment, be parallelized due to its heavy reliance on global state (GlobalState class)
    [DoNotParallelize]
    public sealed class TestGenerateWorld
    {
        private const string WORLD_NAME = "___test_world";

        private static Logger Logger = Logger.InitLogger("TestGenerateWorld", true, Logger.LogLevel.Debug);

        private string tempFolderName;
        
        public TestContext TestContext { get; set; }

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
            using (HeadlessRunner runner = new HeadlessRunner())
            {
                Common.RunParams p = new Common.RunParams
                {
                    runner = runner,
                    args = [
                        "--defaultPort", "9050",
                    "--saveName", WORLD_NAME + Thread.CurrentThread.ManagedThreadId.ToString(),
                    "--createSave",
                    "--createLayer", "255",
                ],
                };
                Thread t = new(Common.Run);
                t.Start(p);
                runner.PauseUpdating();
                runner.WaitUntilWorldLoaded(TestContext.CancellationToken);

                var waiter = runner.UpdateNTimes(1);
                waiter.Wait(TestContext.CancellationToken);
                GlobalState.Exit = true;

                Assert.IsTrue(t.Join(5 * 1000));

                Assert.IsNull(runner.output.runnerThreadException);
                Assert.IsNull(runner.output.gameThreadException);
            }
        }

        [TestMethod]
        public void TestAllGenerators()
        {
            HeadlessRunner.Output output = null;
            using (HeadlessRunner runner = new HeadlessRunner())
            {
                int maxLayers = GlobalState.Registry.WorldLogicRegistry.maxLayers;

                for (int i = 0; i < maxLayers; i++)
                {
                    GlobalState.Exit = false;

                    if (GlobalState.Registry.WorldLogicRegistry.generators[i] != null)
                    {
                        Common.RunParams p = new Common.RunParams
                        {
                            runner = runner,
                            args = [
                                "--defaultPort", "9050",
                            "--saveName", WORLD_NAME + Thread.CurrentThread.ManagedThreadId.ToString() + "_" + i.ToString(),
                            "--createSave",
                            "--createLayer", i.ToString(),
                       ],
                        };
                        Thread t = new(Common.Run);
                        t.Start(p);
                        runner.PauseUpdating();
                        runner.WaitUntilWorldLoaded(TestContext.CancellationToken);

                        var waiter = runner.UpdateNTimes(1);
                        waiter.Wait(TestContext.CancellationToken);

                        GlobalState.Exit = true;
                        Assert.IsTrue(t.Join(5 * 1000));
                        runner.ResumeUpdating();
                    }
                }
                output = runner.output;
            }

            Assert.IsNull(output.runnerThreadException);
            Assert.IsNull(output.gameThreadException);
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
