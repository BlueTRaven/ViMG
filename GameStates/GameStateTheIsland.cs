using BepuPhysics;
using BepuPhysics.Constraints;
using BepuUtilities.Memory;
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
using ViMG.Physics;
using ViMG.UIs;
using static ViMG.WorldInfoIO;

namespace ViMG.GameStates
{
    public class GameStateTheIsland : GameState
    {
        private readonly GraphicsDevice device;
        private readonly TextHelper.FontInfo fi;
        private Task<World> worldTask;
        private World world;

        public bool IsLoading;
        private static object lockObj = new object();
        private static string loadMessage;
        public static string LoadMessage 
        {
            get 
            {
                lock (lockObj) 
                {
                    //return a COPY since we might be modifying this value.
                    //This is slow, but whatever, we're only using this during loading.
                    return new string(loadMessage); 
                } 
            }
            set 
            {
                lock (lockObj) 
                {
                    loadMessage = value; 
                }
            }
        }
        public static int ProgressMin;
        public static int ProgressMax;

        public GameStateTheIsland(GameStateManager manager, GraphicsDevice device) : base(manager)
        {
            this.device = device;

            fi = new TextHelper.FontInfo(Main.assetsManager.GetAsset<SpriteFont>("fira_mono_sml"), 1, true);
        }

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

                if (world == null) throw new Exception("Errored while loading world");

                LoadMessage = "Loading World...";
                //Now we can tell the ChunkLoadManager what should be loaded.
                world.ChunkLoadManager.UpdateLoadTarget(world.WorldInfo.playerPosition);
                world.ChunkLoadManager.LoadAroundTarget(world);

                LoadMessage = "Loading World...\nFlushing queue...";
                //Finally, tell the ChunkLoadManager to actually load the things.
                //(We have to tell it this manually as it queues things up to load, and we want it to finish loading instead of load things in the background
                //as it normally does.)
                world.ChunkLoadManager.FlushLoadQueue();

                LoadMessage = "Loading World...\nFinishing...";
                world.FinishLoading(device);
                //World world = new World(manager, device, 512);
                //world.LoadWorld(device, worldName);

                IsLoading = false;

                return world;
            });

            if (Main.MULTITHREAD_LOADING)
                worldTask.Start();
            else worldTask.RunSynchronously();
        }

        public Task<World> BeginLoadLayer(string worldName, int layer)
        {
            if (!LayerExists(layer))
            {
                Console.WriteLine("Tried to load layer {0} but this layer was not yet implemented.", layer);
                return null;
            }

            //Note that this version assumes the world exists.
            IsLoading = true;
            var layerTask = new Task<World>(() =>
            {
                World world = LoadLayer(worldName, layer);
                world.FinishLoading(device);

                IsLoading = false;
                return world;
            });

            if (Main.MULTITHREAD_LOADING)
                layerTask.Start();
            else layerTask.RunSynchronously();

            return layerTask;
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
                world.Dispose();
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
                    worldTask = null;
                }
            }

            if (world != null && !manager.Paused)
            {
                world.Update(deltaTime);
            }

            base.Update(device, deltaTime);
        }

        public void SetWorld(World world)
        {
            this.world = world;
        }

        public World CreateWorld(GraphicsDevice device, string worldName)
        {
            const int SIZE_IN_CHUNKS = 32;

            var physicsInfo = new PhysicsInfo();

            var entityManager = new EntityManager();
            var entIO = new EntityManagerIO(entityManager, 0);
            var chunkIO = new ChunkManagerIO(SIZE_IN_CHUNKS, "test", 0);
            var chunkManager = new ChunkManager(SIZE_IN_CHUNKS, chunkIO, physicsInfo, device);
            chunkManager.CreateInitializerCubeView();
            chunkManager.CreateThreadedCubeView(null);

            WorldInfoIO.WorldInfo worldInfo = new WorldInfoIO.WorldInfo()
            {
                playerPosition = new Vector3(-1),
                playerLayer = 0,
                furthestLayer = 0,
                time = 0,
                pointsOfInterest = new List<PointOfInterest>(),

                flags = new WorldLogics.WorldFlags(),

                housings = new List<Housing>()
            };

            var worldInfoIO = new WorldInfoIO();

            Skybox skybox = new Skybox();

            var generator = CreateLayerGenerator(0);
            var logic = CreateLayerLogic(0, worldName, device);

            WorldPrototype prototype = new WorldPrototype(worldName, 0, entityManager, chunkManager, worldInfo, logic, skybox, physicsInfo, new HousingManager());

            ChunkGeneratorTasker.GenerateWorld(manager, prototype, generator);

            Main.SessionInformation.LastLoadedSave = worldName;

            ProfilingHelper.Start("Saving Chunks...");
            chunkIO.Save(worldName);

            ProfilingHelper.End("Done.");

            var chunkLoadManager = new ChunkLoadManager(prototype.ChunkManager, prototype.EntityManager, chunkIO, entIO);
            prototype.ChunkManager.CreateThreadedCubeView(chunkLoadManager);

            var player = new Player();
            player.FirstCreated();
            prototype.EntityManager.Add(player, true);

            Vector3 playerSpawnPosition = generator.GetPlayerPosition(prototype.ChunkManager);
            player.Position = playerSpawnPosition;
            player.SpawnPosition = CubePosition.FromWorldSpace(playerSpawnPosition);
            prototype.WorldInfo.playerPosition = player.Position;
            prototype.WorldInfo.playerLayer = 0;

            World world = new World(manager, prototype, chunkLoadManager, worldInfoIO, entIO, chunkIO, device, SIZE_IN_CHUNKS * Chunk.CHUNK_SIZE);
            prototype.Logic.Initialize(world);
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
            Main.SessionIO.Save();

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
            LoadMessage = "Loading World...";
            var entityManager = new EntityManager();
            var worldInfoIO = new WorldInfoIO();

            LoadMessage = "Loading World...\n" +
                "Reading from disk...";
            WorldIO.LoadError error = worldInfoIO.Load(worldName, out WorldInfoIO.WorldInfo worldInfo);
            if (worldInfoIO.HandleError(error, worldName))
                return null;

            if (worldInfo.playerPosition.LengthSquared() < 0)
                worldInfo.playerPosition = defaultPlayerSpawnLocation.InWorldSpace();

            var physicsInfo = new PhysicsInfo();

            var chunkIO = new ChunkManagerIO(SIZE_IN_CHUNKS, "test", worldInfo.playerLayer);
            var entIO = new EntityManagerIO(entityManager, worldInfo.playerLayer);
            var chunkManager = new ChunkManager(SIZE_IN_CHUNKS, chunkIO, physicsInfo, device);
            var housingManager = new HousingManager();
            housingManager.FinishLoading(worldInfo);

            var logic = CreateLayerLogic(worldInfo.playerLayer, worldName, device);

            WorldPrototype prototype = new WorldPrototype(worldName, worldInfo.playerLayer, entityManager, chunkManager, worldInfo, logic, new Skybox(), physicsInfo, housingManager);

            error = chunkIO.Load(worldName);
            if (chunkIO.HandleError(error, worldName))
                return null;

            error = entIO.Load(worldName);//saver.Load(device, this, folderName);
            if (entIO.HandleError(error, worldName))
                return null;

            var ChunkLoadManager = new ChunkLoadManager(prototype.ChunkManager, prototype.EntityManager, chunkIO, entIO);
            prototype.ChunkManager.CreateThreadedCubeView(ChunkLoadManager);
            prototype.ChunkManager.CreateInitializerCubeView();

            LoadMessage = "Loading World...\n" +
                "Deserializing...";

            //Deserialize this player chunk; the player entity is created.
            //We do this this way since the player is, really, just another entity. Treating it otherwise (with its own deserialization routine)
            //is overcomplicating the problem.
            //entIO.DeserializePlayerChunk();

            Main.SessionInformation.LastLoadedSave = worldName;

            ProfilingHelper.End("World loading done.");

            World world = new World(manager, prototype, ChunkLoadManager, worldInfoIO, entIO, chunkIO, device, SIZE_IN_CHUNKS * Chunk.CHUNK_SIZE);
            prototype.Logic.Initialize(world);
            return world;

            //EntityManager.Add(new EntityLeviathan());
        }

        public World LoadLayer(string worldName, int layer)
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

            LoadMessage = "Loading World...";
            var worldInfoIO = new WorldInfoIO();

            LoadMessage = "Loading World...\n" +
                "Reading from disk...";
            WorldIO.LoadError error = worldInfoIO.Load(worldName, out WorldInfoIO.WorldInfo worldInfo);
            
            if (worldInfoIO.HandleError(error, worldName))
                return null;

            if (worldInfo.playerPosition.LengthSquared() < 0)
                worldInfo.playerPosition = defaultPlayerSpawnLocation.InWorldSpace();

            if (worldInfo.furthestLayer < layer)
            {
                ProfilingHelper.Start("Creating Unvisited Layer...");

                worldInfo.furthestLayer = layer;

                var entityManager = new EntityManager();
                
                var physicsInfo = new PhysicsInfo();

                var chunkIO = new ChunkManagerIO(SIZE_IN_CHUNKS, "test", layer);
                var entIO = new EntityManagerIO(entityManager, layer);
                var chunkManager = new ChunkManager(SIZE_IN_CHUNKS, chunkIO, physicsInfo, device);
                chunkManager.CreateInitializerCubeView();
                chunkManager.CreateThreadedCubeView(null);

                Skybox skybox = new Skybox();

                var generator = CreateLayerGenerator(layer);
                var logic = CreateLayerLogic(layer, worldName, device);

                WorldPrototype prototype = new WorldPrototype(worldName, layer, entityManager, chunkManager, worldInfo, logic, skybox, physicsInfo, new HousingManager());

                ChunkGeneratorTasker.GenerateWorld(manager, prototype, generator);

                Main.SessionInformation.LastLoadedSave = worldName;

                ProfilingHelper.Start("Saving Chunks...");
                chunkIO.Save(worldName);

                ProfilingHelper.End("Done.");

                var chunkLoadManager = new ChunkLoadManager(prototype.ChunkManager, prototype.EntityManager, chunkIO, entIO);
                prototype.ChunkManager.CreateThreadedCubeView(chunkLoadManager);

                World world = new World(manager, prototype, chunkLoadManager, worldInfoIO, entIO, chunkIO, device, SIZE_IN_CHUNKS * Chunk.CHUNK_SIZE);
                prototype.Logic.Initialize(world);
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
            else
            {
                ProfilingHelper.Start("Loading Layer...");

                var entityManager = new EntityManager();

                var physicsInfo = new PhysicsInfo();

                var chunkIO = new ChunkManagerIO(SIZE_IN_CHUNKS, "test", layer);
                var entIO = new EntityManagerIO(entityManager, layer);
                var chunkManager = new ChunkManager(SIZE_IN_CHUNKS, chunkIO, physicsInfo, device);

                var housingManager = new HousingManager();
                housingManager.FinishLoading(worldInfo);

                var logic = CreateLayerLogic(layer, worldName, device);

                Skybox skybox = new Skybox();

                WorldPrototype prototype = new WorldPrototype(worldName, layer, entityManager, chunkManager, worldInfo, logic, skybox, physicsInfo, housingManager);

                error = chunkIO.Load(worldName);
                if (chunkIO.HandleError(error, worldName))
                    return null;

                error = entIO.Load(worldName);//saver.Load(device, this, folderName);
                if (entIO.HandleError(error, worldName))
                    return null;

                var ChunkLoadManager = new ChunkLoadManager(prototype.ChunkManager, prototype.EntityManager, chunkIO, entIO);
                prototype.ChunkManager.CreateThreadedCubeView(ChunkLoadManager);
                prototype.ChunkManager.CreateInitializerCubeView();

                LoadMessage = "Loading World...\n" +
                    "Deserializing...";

                //Deserialize this player chunk; the player entity is created.
                //We do this this way since the player is, really, just another entity. Treating it otherwise (with its own deserialization routine)
                //is overcomplicating the problem.
                //entIO.DeserializePlayerChunk();

                Main.SessionInformation.LastLoadedSave = worldName;

                ProfilingHelper.End("World loading done.");

                World world = new World(manager, prototype, ChunkLoadManager, worldInfoIO, entIO, chunkIO, device, SIZE_IN_CHUNKS * Chunk.CHUNK_SIZE);
                prototype.Logic.Initialize(world);
                return world;
            }
        }

        private bool LayerExists(int layer)
        {
            switch (layer)
            {
                case 0:
                    return true;
                case 1:
                    return true;
                default:
                    return false;
            }
        }

        private ChunkGenerator CreateLayerGenerator(int layer)
        {
            ChunkGenerator generator;

            switch (layer)
            {
                case 0:
                    generator = new ChunkGeneratorIsland(layer);
                    break;
                case 1:
                    generator = new ChunkGeneratorCatacombs();
                    break;
                default:
                    Console.WriteLine("LAYER {0} HAS NOT YET BEEN FILLED OUT YET AND IS UNIMPLEMENTED!", layer);
                    generator = null;
                    break;
            }

            return generator;
        }

        private WorldLogics.WorldLogic CreateLayerLogic(int layer, string worldName, GraphicsDevice device)
        {
            WorldLogics.WorldLogic logic;

            switch (layer)
            {
                case 0:
                    logic = new WorldLogics.WorldLogicIsland(worldName, device);
                    break;
                case 1:
                    logic = new WorldLogics.WorldLogicCatacombs(device);
                    break;
                default:
                    Console.WriteLine("LAYER {0} HAS NOT YET BEEN FILLED OUT YET AND IS UNIMPLEMENTED!", layer);
                    logic = null;
                    break;
            }

            return logic;
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
                TextHelper.DrawText(batch, fi, loadMessage, Color.White, 
                    new Rectangle(0, 0, Options.CurrentWindowResolution.X, Options.CurrentWindowResolution.Y), 
                    Enums.Alignment.Center, Options.CurrentWindowResolution.X, 1);

                if (ProgressMin >= 0)
                TextHelper.DrawText(batch, fi, ProgressMin + "/" + ProgressMax, Color.White,
                    new Rectangle(0, (int)(fi.font.LineSpacing * 1.5f), Options.CurrentWindowResolution.X, Options.CurrentWindowResolution.Y), 
                    Enums.Alignment.Center, Options.CurrentWindowResolution.X, 1);
            }
        }
    }
}
