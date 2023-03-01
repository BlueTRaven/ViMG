using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Cubes;
using ViMG.Entities;
using ViMG.Generation;
using ViMG.UIs;

namespace ViMG.GameStates
{
    public class GameStateTheIsland : GameState
    {
        private readonly GraphicsDevice device;
        private readonly TextHelper.FontInfo fi;
        private Task<World> worldTask;
        private World world;

        public bool IsLoading;
        private string loadMessage;
        public string LoadMessage 
        {
            get 
            {
                object toLock = loadMessage == null ? device : loadMessage;
                lock (toLock) 
                {
                    //return a COPY since we might be modifying this value.
                    //This is slow, but whatever, we're only using this during loading.
                    return new string(loadMessage); 
                } 
            }
            set 
            {
                object toLock = loadMessage == null ? device : loadMessage;
                lock (toLock) 
                {
                    loadMessage = value; 
                }
            }
        }

        public GameStateTheIsland(GameStateManager manager, GraphicsDevice device) : base(manager)
        {
            this.device = device;

            fi = new TextHelper.FontInfo(Main.assetsManager.GetAsset<SpriteFont>("fira_mono_sml"), 1, true);
        }

        private static int n = 0;

        public void BeginLoadWorld(string worldName)
        {
            IsLoading = true;
            worldTask = new Task<World>(() =>
            {
                World world;
                if (!Directory.Exists("./saves/" + worldName + "/"))
                {
                    world = CreateWorld(device, worldName);
                }
                else
                {
                    world = LoadWorld(device, worldName);
                }
                world.FinishLoading(device);
                //World world = new World(manager, device, 512);
                //world.LoadWorld(device, worldName);

                return world;
            });

            Console.WriteLine("Run??? {0}", n);
            n++;
            if (Main.MULTITHREAD_LOADING)
                worldTask.Start();
            else worldTask.RunSynchronously();
        }

        public override void OnOpen(GameState changingFrom)
        {
            base.OnOpen(changingFrom);
        }

        public override void OnClose(GameState changingTo)
        {
            base.OnClose(changingTo);

            if (world != null)
            {
                world.ChunkLoadManager.UnloadAll();
                world.ChunkLoadManager.Dispose();
                world.ChunkManager.Dispose();
                world = null;
            }
            SetMenu(null);
        }

        public override void Update(GraphicsDevice device, double deltaTime)
        {
            if (world == null)
            {
                if (worldTask.IsCompleted)
                {
                    world = worldTask.Result;

                    IsLoading = false;
                }
                else System.Threading.Thread.Sleep(100);
            }

            if (world != null && !manager.Paused)
            {
                world.Update(deltaTime);
            }

            base.Update(device, deltaTime);
        }

        public World CreateWorld(GraphicsDevice device, string worldName)
        {
            const int SIZE_IN_CHUNKS = 32;

            //If the directory does not exist, run world generation, save, and then load.
            //TODO: this maybe shouldn't exist here?
            var entityManager = new EntityManager();
            var chunkGenerator = new ChunkGeneratorIsland(0);
            var entIO = new EntityManagerIO(entityManager);
            var chunkIO = new ChunkManagerIO(SIZE_IN_CHUNKS, "test");
            var chunkManager = new ChunkManager(SIZE_IN_CHUNKS, chunkIO, device);
            chunkManager.CreateInitializerCubeView();
            chunkManager.CreateThreadedCubeView(null);

            WorldInfoIO.WorldInfo worldInfo = new WorldInfoIO.WorldInfo()
            {
                playerPosition = new Vector3(-1),
                time = 0,
                pointsOfInterest = new List<PointOfInterest>(),
            };

            Skybox skybox = new Skybox();
            WorldPrototype prototype = new WorldPrototype(worldName, 0, entityManager, chunkManager, worldInfo, new WorldLogics.WorldLogicIsland(worldName, skybox, device), skybox);

            ChunkGeneratorTasker.GenerateWorld(manager, prototype, chunkGenerator);

            Main.SessionInformation.LastLoadedSave = worldName;

            var worldInfoIO = new WorldInfoIO();

            ProfilingHelper.Start("Saving Chunks...");
            //chunkIO.SerializeAll();
            chunkIO.Save(worldName);

            ProfilingHelper.End("Done.");

            var chunkLoadManager = new ChunkLoadManager(prototype.ChunkManager, prototype.EntityManager, chunkIO, entIO);
            prototype.ChunkManager.CreateThreadedCubeView(chunkLoadManager);

            var player = new Player();
            player.FirstCreated();
            prototype.EntityManager.Add(player, true);

            Vector3 playerSpawnPosition = chunkGenerator.GetPlayerPosition(prototype.ChunkManager);
            player.Position = playerSpawnPosition;
            player.SpawnPosition = CubePosition.FromWorldSpace(playerSpawnPosition);
            prototype.WorldInfo.playerPosition = player.Position;

            World world = new World(manager, prototype, chunkLoadManager, worldInfoIO, entIO, chunkIO, device, SIZE_IN_CHUNKS * Chunk.CHUNK_SIZE);
            entityManager.AddLaterEntities();

            ProfilingHelper.Start("Saving Entities...");
            entIO.SerializeAll(SIZE_IN_CHUNKS);
            entIO.Save(worldName);
            ProfilingHelper.End("Done.");

            ProfilingHelper.Start("Reloading...");
            //The way world creation is set up is that it creates everything - the entire world - at the same time.
            //That means we'd have entirely too much stuff in memory after we're done. We're not going to be near half of that stuff.
            //Instead of letting that sit in memory, we just unload EVERYTHING
            //then reload the things that are nearby.
            //The World is responsible for the loading later.
            //Note that the ChunkLoadManager isn't aware that anything is loaded (since we don't use the ChunkLoadManager for world generation).
            //So we just call the raw Unload functions.
            entityManager.UnloadAll();
            //chunkLoadManager.UnloadAll();
            
            worldInfoIO.Save(worldName, world.WorldInfo);


            ProfilingHelper.End("Done.");

            return world;
        }

        public World LoadWorld(GraphicsDevice device, string worldName)
        {
            const int SIZE_IN_CHUNKS = 32;
            const int SIZE_IN_CUBES = SIZE_IN_CHUNKS * Chunk.CHUNK_SIZE;

            int spawnX = Main.random.Next(SIZE_IN_CUBES / 2 - 4, SIZE_IN_CUBES / 2 + 4);
            int spawnZ = Main.random.Next(SIZE_IN_CUBES / 2 - 4, SIZE_IN_CUBES / 2 + 4);

            CubePosition defaultPlayerSpawnLocation = CubePosition.FromWorldSpace(
                new Vector3(SIZE_IN_CUBES * Cube.CUBE_SCALE / 2f, SIZE_IN_CUBES * Cube.CUBE_SCALE, SIZE_IN_CUBES * Cube.CUBE_SCALE / 2f));
            defaultPlayerSpawnLocation.X = spawnX;
            defaultPlayerSpawnLocation.Z = spawnZ;
            defaultPlayerSpawnLocation.Y = SIZE_IN_CUBES;

            ProfilingHelper.Start("Loading world...");
            manager.TheIsland.LoadMessage = "Loading World...";
            var entityManager = new EntityManager();
            var worldInfoIO = new WorldInfoIO();
            var chunkIO = new ChunkManagerIO(SIZE_IN_CHUNKS, "test");
            var entIO = new EntityManagerIO(entityManager);
            var chunkManager = new ChunkManager(SIZE_IN_CHUNKS, chunkIO, device);

            manager.TheIsland.LoadMessage = "Loading World...\n" +
                "Reading from disk...";
            WorldIO.LoadError error = worldInfoIO.Load(worldName, out WorldInfoIO.WorldInfo worldInfo);
            if (error == WorldIO.LoadError.InvalidVersion)
                Console.WriteLine("World Info file could not be loaded. The current file version ({0}) is not supported.", worldInfoIO.Version);

            if (worldInfo.playerPosition.LengthSquared() < 0)
                worldInfo.playerPosition = defaultPlayerSpawnLocation.InWorldSpace();

            Skybox skybox = new Skybox();
            WorldPrototype prototype = new WorldPrototype(worldName, 0, entityManager, chunkManager, worldInfo, new WorldLogics.WorldLogicIsland(worldName, skybox, device), skybox);

            error = chunkIO.Load(worldName);
            if (error == WorldIO.LoadError.InvalidVersion)
                Console.WriteLine("Chunk file could not be loaded. The current file version ({0}) is not supported.", chunkIO.Version);

            error = entIO.Load(worldName);//saver.Load(device, this, folderName);
            if (error == WorldIO.LoadError.InvalidVersion)
                Console.WriteLine("Entity file could not be loaded. The current file version ({0}) is not supported.", entIO.Version);

            var ChunkLoadManager = new ChunkLoadManager(prototype.ChunkManager, prototype.EntityManager, chunkIO, entIO);
            prototype.ChunkManager.CreateThreadedCubeView(ChunkLoadManager);
            prototype.ChunkManager.CreateInitializerCubeView();

            manager.TheIsland.LoadMessage = "Loading World...\n" +
                "Deserializing...";

            //Deserialize this player chunk; the player entity is created.
            //We do this this way since the player is, really, just another entity. Treating it otherwise (with its own deserialization routine)
            //is overcomplicating the problem.
            //entIO.DeserializePlayerChunk();

            Main.SessionInformation.LastLoadedSave = worldName;

            ProfilingHelper.End("World loading done.");

            World world = new World(manager, prototype, ChunkLoadManager, worldInfoIO, entIO, chunkIO, device, SIZE_IN_CHUNKS * Chunk.CHUNK_SIZE);
            return world;

            //Main.FogManager.Set(1300f, 1700f, Main.assetsManager.GetAsset<Texture2D>("heightmap_layer1_day"), Main.assetsManager.GetAsset<Texture2D>("heightmap_layer1_night"), 0);
            //ChunkLoadManager2 = new ChunkLoadManager(saver2, ChunkManager2, 6, 6, 8);

            //EntityManager.Add(new EntityLeviathan());
        }

        public override void Draw(GraphicsDevice device)
        {
            base.Draw(device);

            if (world != null)
            {
                world.Draw(device);
            }
        }

        public override void DrawUI(SpriteBatch batch)
        {
            base.DrawUI(batch);

            if (world != null)
            {
                world.DrawUI(batch);
            }

            if (IsLoading && LoadMessage != null)
            {
                string loadMessage = LoadMessage;

                TextHelper.DrawText(batch, fi, loadMessage, Color.White, new Rectangle(0, 0, Options.CurrentWindowResolution.X, Options.CurrentWindowResolution.Y), Enums.Alignment.Center, Options.CurrentWindowResolution.X, 1);
            }
        }
    }
}
