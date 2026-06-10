using Engine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG;
using ViMG.Entities;

namespace EngineTests.Integration
{
    [TestClass]
    [DoNotParallelize]
    public class TestBlocks
    {
        private const string WORLD_NAME = "___test_world_blocks";
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
                (bool, bool)[] playerUsePermutations = [
                    (false, false),
                    (true, false),
                    (false, true),
                    (true, true),
                ];
                foreach ((bool playerPlace, bool playerDestroy) in playerUsePermutations)
                {
                    var cubeRegistry = GlobalState.Registry.CubeRegistry;
                    var cubes = cubeRegistry.GetIterable().ToArray();
                    foreach (var cube in cubes)
                    {
                        if (cts.IsCancellationRequested) break;

                        yield return [cube.Id, playerPlace, playerDestroy];
                    }
                }
            }
        }

        [TestMethod]
        [DynamicData(nameof(TestData))]
        public void CreateBlock(int id, bool playerPlace, bool playerDestroy)
        {
            ChunkPosition spawnChunk = Common.GetSpawnChunk(cts.Token, runner);
            Common.ForceLoadChunk(cts.Token, runner, spawnChunk);

            CubePosition putPosition = spawnChunk.InCubeSpace();
            var cube = GlobalState.Registry.CubeRegistry.Get(id);
            Assert.IsNotNull(cube);

            EntityManager.EntityReference invalidRef = new EntityManager.EntityReference { id = -1, generation = -1 };
            EntityManager.EntityReference player = invalidRef;
            if (playerPlace || playerDestroy)
            {
                player = Common.CreateEntity(cts.Token, runner, new Common.CreateEntityParams
                {
                    id = GlobalState.Registry.EntityRegistry.Get<Player>().Id,
                    position = putPosition.InWorldSpace(),
                });
            }

            if (!playerPlace)
                Common.CreateCube(cts.Token, runner, new Common.CreateCubeParams { position = putPosition, id = id, playerRef = invalidRef });
            else Common.PlaceCube(cts.Token, runner, new Common.CreateCubeParams { position = putPosition, id = id, playerRef = player });

            // Remove it
            if (!playerDestroy)
                Common.CreateCube(cts.Token, runner, new Common.CreateCubeParams { position = putPosition, id = 0, playerRef = invalidRef });
            else Common.MineCube(cts.Token, runner, new Common.CreateCubeParams { position = putPosition, id = 0, playerRef = player });

            Common.UnloadAllEntities(cts.Token, runner);
            Common.ValidateAllUnloaded(cts.Token, runner);
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
