using Engine;
using Engine.Entities;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG;
using ViMG.Cubes;
using ViMG.Entities;
using static ViMG.UIs.UI;

namespace EngineTests.Integration
{
    [TestClass]
    [DoNotParallelize]
    public sealed class TestEntities
    {
        private const string WORLD_NAME = "___test_world";

        private static Logger Logger = Logger.InitLogger("TestEntities", true, Logger.LogLevel.Debug);

        private static string tempFolderName;
        private static HeadlessRunner runner;
        private static Thread runnerThread;
        // A cancellation token source that should include both the test context's cancellation token,
        // as well as the runner's cancellation token source.
        // This is primarily so that if either the test is cancelled or the world errors out and is destroyed,
        // we observe the cancellation in both cases.
        private static CancellationTokenSource cts;
        
        public TestContext TestContext { get; set; }

        [ClassInitialize]
        public static void Initialize(TestContext testContext)
        {
            Console.WriteLine("Test start {0}", DateTime.Now);
            Logger.SetAllLogLevels(Logger.LogLevel.Debug);

            tempFolderName = Path.GetTempPath() + "/vimg/saves/";
            WorldIO.SaveFolder = tempFolderName;

            // Clean up directory if it already exists - sometimes the case with errored-out tests
            if (Directory.Exists(tempFolderName))
                Directory.Delete(tempFolderName, true);

            GlobalState.Exit = false;
            runner = new HeadlessRunner(testContext.CancellationToken);
            cts = CancellationTokenSource.CreateLinkedTokenSource(runner.cts.Token, testContext.CancellationToken);

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
            runnerThread = new(Common.Run);
            runnerThread.Start(p);
            runner.PauseUpdating();
            runner.WaitUntilWorldLoaded(cts.Token);

            var waiter = runner.UpdateNTimes(1);
            waiter.Wait(cts.Token);
        }

        public static IEnumerable<object[]> TestData
        {
            get
            {
                var entRegistry = GlobalState.Registry.EntityRegistry;
                var ents = entRegistry.GetIterable().ToArray();
                foreach (var ent in ents)
                {
                    if (cts.IsCancellationRequested) break;

                    if (ent.CanNew())
                    {
                        yield return [ent.Id];
                    }
                }
            }
        }

        [TestMethod]
        [DynamicData(nameof(TestData))]
        // Creates all entities (that can be created) one at a time, updates them a few times, then unloads them.
        public void CreateEntity(int id)
        {
            ChunkPosition spawnChunk = Common.GetSpawnChunk(cts.Token, runner);
            Common.ForceLoadChunk(cts.Token, runner, spawnChunk);

            var entType = GlobalState.Registry.EntityRegistry.Get(id);
            Assert.IsNotNull(entType);
            
            Vector3 spawnPos = spawnChunk.InWorldSpace() + new Vector3(Chunk.CHUNK_SIZE / 2 * Cube.CUBE_SCALE, 0, Chunk.CHUNK_SIZE / 2 * Cube.CUBE_SCALE);
            Logger.Info("Create entity {0} {1} at {2}", id, entType.Identifier, spawnPos);
            EntityManager.EntityReference reference = Common.CreateEntity(cts.Token, runner, new Common.CreateEntityParams { id = entType.Id, position = spawnPos });
            Logger.Info("Has ref {0} {1}", reference.id, reference.generation);
            var waiter = runner.FixedUpdateNTimes(1);
            waiter.Wait(cts.Token);
            Logger.Info("Unload");
            Common.UnloadAllEntities(cts.Token, runner);
            waiter = runner.FixedUpdateNTimes(1);
            waiter.Wait(cts.Token);
            Common.ValidateAllUnloaded(cts.Token, runner);
            Logger.Info("Done");
        }

        [TestMethod]
        // Tests creating all entities all at once, and updates them for 5 seconds.
        public void CreateAllEntities()
        {
            ChunkPosition spawnChunk = Common.GetSpawnChunk(cts.Token, runner);
            Common.ForceLoadChunk(cts.Token, runner, spawnChunk);

            var entRegistry = GlobalState.Registry.EntityRegistry;
            var ents = entRegistry.GetIterable().ToArray();
            foreach (var ent in ents)
            {
                if (ent.CanNew())
                {
                    var entType = GlobalState.Registry.EntityRegistry.Get(ent.Id);
                    Assert.IsNotNull(entType);

                    Vector3 spawnPos = spawnChunk.InWorldSpace() + new Vector3(Chunk.CHUNK_SIZE / 2 * Cube.CUBE_SCALE, 0, Chunk.CHUNK_SIZE / 2 * Cube.CUBE_SCALE);
                    Logger.Info("Create entity {0} {1} at {2}", ent.Id, entType.Identifier, spawnPos);
                    EntityManager.EntityReference reference = Common.CreateEntity(cts.Token, runner, new Common.CreateEntityParams { id = entType.Id, position = spawnPos });
                    Logger.Info("Has ref {0} {1}", reference.id, reference.generation);
                }
            }

            // 5 seconds of updates
            var waiter = runner.FixedUpdateNTimes(Main.FIXED_FPS * 5);
            waiter.Wait(cts.Token);

            Logger.Info("Done");
        }

        [ClassCleanup]
        public static void Cleanup()
        {
            Console.WriteLine("Clean up {0} {1} ", DateTime.Now, tempFolderName);
            runner.Dispose();
            Assert.IsTrue(runnerThread.Join(5 * 1000));

            Thread.Sleep(5 * 1000);
            Directory.Delete(tempFolderName, true);
        }
    }
}
