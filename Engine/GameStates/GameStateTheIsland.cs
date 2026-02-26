using BepuPhysics;
using BepuPhysics.Constraints;
using BepuUtilities.Memory;
using BrUtility;
using Engine;
using Engine.ChunkStuff;
using Engine.Clients;
using Engine.IMGUIImpl;
using Engine.Items;
using Engine.Networking;
using Engine.Networking.Messages;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Cubes;
using ViMG.Entities;
using ViMG.Generation;
using ViMG.IMGUIImpl;
using ViMG.Physics;
using ViMG.Rendering;
using ViMG.UIs;

namespace ViMG.GameStates
{
    public class GameStateTheIsland : GameState
    {
        private static Engine.Logger Logger = Engine.Logger.InitLogger("GameStateTheIsland", true, Engine.Logger.LogLevel.Info);

        [ConsoleCommandVar("pause_when_world_loaded", "Set GameStateManager.Paused to true when the current world is done loading.")]
        public static bool PauseWhenWorldLoaded = false;

        private GraphicsDevice device;
        private TextHelper.FontInfo fi;
        private Task<World>? worldTask;
        private World? world;
        private ClientStates? client;

        public bool IsLoading;
        private static object lockObj = new object();
        private static string loadMessage = "";
        public static string LoadMessage 
        {
            get 
            {
                lock (lockObj) 
                {
                    return loadMessage; 
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

        public PlayerManagerIO? playerIO;
        public NetworkManager? netManagerServer;
        public NetworkManager? netManagerClient;

        // Set when world is loaded
        public ManualResetEventSlim waiterWorld;

        public string? localPlayerName;

        public GameStateTheIsland(GameStateManager manager) : base(manager)
        {
            waiterWorld = new ManualResetEventSlim(false);
        }

        public override void LoadContent(GraphicsDevice device)
        {
            this.device = device;
            base.LoadContent(device);
            
            fi = new TextHelper.FontInfo(GlobalState.AssetsManager.GetAsset<SpriteFont>("fira_mono_sml"), 1, true);
        }

        public bool BeginLoadWorld(string worldName)
        {
            using var zone = TracyImpl.Tracy.BeginZone();

            var invalidChars = System.IO.Path.GetInvalidFileNameChars();
            if (string.IsNullOrWhiteSpace(worldName) || worldName.Any(c => invalidChars.Contains(c)))
            {
                Logger.Log(Engine.Logger.LogLevel.Error, "'{0}' is an invalid world file name.", worldName);
                return false;
            }

            Debug.Assert(!IsLoading);

            Logger.Log(Engine.Logger.LogLevel.Info, "BeginLoadWorld");

            IsLoading = true;
            worldTask = new Task<World>(() =>
            {
                ProfilingHelper.Start(Logger, "Loading and Flushing World...");

                World world;
                if (!Directory.Exists(WorldIO.SaveFolder + worldName + "/"))
                {
                    world = CreateWorld(device, worldName);
                    playerIO = new PlayerManagerIO(); 
                    playerIO.Load(worldName);
                }
                else
                {
                    world = LoadWorld(device, worldName);
                    playerIO = new PlayerManagerIO();
                    playerIO.Load(worldName);
                }

                //if (!GlobalState.Args.dedicatedServer)
                //    playerIO.DeserializeLocal(world);

                if (world == null) throw new Exception("Errored while loading world");

                LoadMessage = "Loading World...";
                ProfilingHelper.Start(Logger, "Building Meshes...");
                //if (!GlobalState.Args.dedicatedServer)
                //{
                    //Now we can tell the ChunkLoadManager what should be loaded.
                    //world.ChunkLoadManager.LoadAroundTarget(world, ChunkPosition.WorldSpaceChunk(world.WorldInfo.playerPositions[world.localPlayerIndex]), tempRenderDistance: 1);
                //}

                LoadMessage = "Loading World...\nFlushing queue...";
                //Finally, tell the ChunkLoadManager to actually load the things.
                //(We have to tell it this manually as it queues things up to load, and we want it to finish loading instead of load things in the background
                //as it normally does.)
                world.ChunkLoadManager.FlushLoadQueue(world);
                ProfilingHelper.End(Logger, "Done Building Meshes.");

                LoadMessage = "Loading World...\nFinishing...";
                world.FinishLoading(device);

                IsLoading = false;
                ProfilingHelper.End(Logger, "Finished Loading and Flushing World.");

                return world;
            });

            if (GlobalState.MULTITHREAD_LOADING)
                worldTask.Start();
            else worldTask.RunSynchronously();

            return true;
        }

        public Task<World> BeginLoadLayer(string worldName, int layer)
        {
            using var zone = TracyImpl.Tracy.BeginZone();

            if (!LayerExists(layer))
            {
                Logger.Log(Engine.Logger.LogLevel.Error, "Tried to load layer {0} but this layer was not yet implemented.", layer);
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

            if (GlobalState.MULTITHREAD_LOADING)
                layerTask.Start();
            else layerTask.RunSynchronously();

            return layerTask;
        }

        public void Connect(string ip, int port, bool delay = false)
        {
            if (netManagerServer != null)
            {
                netManagerServer.Ip = ip;
                netManagerServer.Port = port;
            }
            if (netManagerClient != null)
            {
                netManagerClient.Ip = ip;
                netManagerClient.Port = port;
            }

            if (GlobalState.GameStateManager.netMode == GameStateManager.NetworkingMode.Client)
                ConnectLocal();
        }

        public void ConnectLocal()
        {
            // NetworkManager defaults are already set up to connect locally, so we don't really need to do anything
            netManagerServer?.Connect(GameStateManager.NetworkingMode.Server);
            netManagerClient?.Connect(GameStateManager.NetworkingMode.Client);
        }

        public void Disconnect()
        {
            netManagerServer?.Disconnect();
            netManagerClient?.Disconnect();
        }

        public override void OnOpen(GameState changingFrom)
        {
            base.OnOpen(changingFrom);

            if (manager.netMode == GameStateManager.NetworkingMode.Singleplayer)
            {
                netManagerClient = new();
                netManagerServer = new();
            }
            else if (manager.netMode == GameStateManager.NetworkingMode.Server)
            {
                netManagerServer = new();
            }
            else if (manager.netMode == GameStateManager.NetworkingMode.Client)
            {
                netManagerClient = new();
            }
        }

        public override void OnClose(GameState changingTo)
        {
            base.OnClose(changingTo);
            Disconnect();
            netManagerServer = null;
            netManagerClient = null;

            if (world != null)
            {
                world.Dispose();
                world = null;
            }
            if (client != null)
            {
                client.Dispose();
                client = null;
            }
            IMGUINetworkDebug.ClearMessages();
            SyncWorldState.Instance.ServerShutdown();
            SetMenu(null);
        }

        public bool StartSingleplayer(string worldName)
        {
            if (BeginLoadWorld(worldName))
            {
                client = new ClientStates(device);
                return true;
            }

            return false;
        }

        public bool StartServer(string worldName, string ip, int port)
        {
            netManagerServer!.Ip = ip;
            netManagerServer!.Port = port;
            return BeginLoadWorld(worldName);
        }

        public void StartClient(string ip, int port)
        {
            netManagerClient!.Ip = ip;
            netManagerClient!.Port = port;
            client = new ClientStates(device);
            ConnectLocal();
        }

        public bool PollWorldLoaded()
        {
            if (world == null)
            {
                if (worldTask != null && worldTask.IsCompleted)
                {
                    world = worldTask.Result;
                    worldTask = null;
                    waiterWorld.Set();

                    ConnectLocal();

                    if (PauseWhenWorldLoaded)
                    {
                        manager.Paused = true;
                        // Shoud we continue to execute here or just return?
                    }

                    return true;
                }
            }

            return world != null;
        }

        public override void UnfixedUpdate(double deltaTime)
        {
            base.UnfixedUpdate(deltaTime);

            PollWorldLoaded();

            if (world != null)
            {
                // Server can only poll events if world is loaded?
                // I don't know if this really should be true or not. We might want to just instantly disconnect players while waiting? Or something?
                netManagerServer?.PollEvents();
            }

            if (GlobalState.GameStateManager.netMode != GameStateManager.NetworkingMode.Server)
            {
                // If world takes longer than client whoami timeout, this might fail?
                if (netManagerClient.ClientHasConnected())
                {
                    netManagerClient?.PollEvents();
                }
                else
                {
                    netManagerClient.CheckConnected();
                }
            }
        }

        public override void Update(double deltaTime)
        {
            using var zone = TracyImpl.Tracy.BeginZone();

            if (world != null)
            {
                if (!manager.Paused)
                    world.Update(deltaTime);
            }

            if (GlobalState.GameStateManager.netMode != GameStateManager.NetworkingMode.Server) 
            {
                // If world takes longer than client whoami timeout, this might fail?
                if (netManagerClient.ClientHasConnected())
                {
                    if (client != null && !manager.Paused)
                    {
                        client.CurrentTime += deltaTime;

                        client.ChunkManager.ChunkMesher.Update(client.currInterpState.camera.Position, client.ChunkManager.CopyManager, client.Current().entities);
                        client.UpdatePlayer(deltaTime);
                    }
                } 
            }

            base.Update(deltaTime);
        }

        public World GetWorld()
        {
            return this.world;
        }

        public ClientStates GetClient()
        {
            return client;
        }

        public void SetWorld(World world)
        {
            this.world = world;
        }

        public static World CreateWorld(GraphicsDevice? device, string worldName)
        {
            using var zone = TracyImpl.Tracy.BeginZone();

            const int SIZE_IN_CHUNKS = 32;

            var physicsInfo = new PhysicsInfo();

            var chunkMesher = ChunkMesher.CollisionOnly(SIZE_IN_CHUNKS, physicsInfo);
            var entityManager = new EntityManager();
            var inventoryManager = new InventoryManager();
            var entIO = new EntityManagerIO(entityManager, 0);
            var chunkIO = new ChunkManagerIO(SIZE_IN_CHUNKS, "test", 0);
            var chunkManager = new ChunkManager(SIZE_IN_CHUNKS, chunkIO, entityManager.MeshCubeTrackers, chunkMesher);

            WorldInfoIO.WorldInfo worldInfo = new WorldInfoIO.WorldInfo()
            {
                playerPositions = new Vector3[World.MAX_PLAYERS],
                playerLayers = new int[World.MAX_PLAYERS],
                furthestLayer = 0,
                time = 0,
                pointsOfInterest = new List<PointOfInterest>(),

                flags = new WorldLogics.WorldFlags(),

                housings = new List<Housing>()
            };

            var worldInfoIO = new WorldInfoIO();
               
            Skybox? skybox = device != null ? new Skybox() : null;

            // This is up here so we can use this information when loading a world (coconut easter egg)
            // but it also might present a problem; if we error at any point during the creation/loading process,
            // pressing "Continue" will just try to load the same world that caused the error instead of staying the same.
            GlobalState.SessionInformation.LastLoadedSave = worldName;

            var generator = CreateLayerGenerator(0);
            var logic = CreateLayerLogic(0);

            chunkIO.CreateAll();

            WorldPrototype prototype = new WorldPrototype(worldName, 0, entityManager, inventoryManager, chunkManager, worldInfo, logic, skybox, physicsInfo, new HousingManager());

            ChunkGeneratorTasker.GenerateWorld(prototype, generator);

            Vector3 playerSpawnPosition = generator.GetPlayerPosition(prototype.ChunkManager);
            for (int i = 0; i < World.MAX_PLAYERS; i++)
            {
                prototype.WorldInfo.playerPositions[i] = playerSpawnPosition;
                prototype.WorldInfo.playerLayers[i] = 0;
            }
            prototype.WorldInfo.spawnPosition = playerSpawnPosition;
            prototype.WorldInfo.spawnLayer = 0;

            ProfilingHelper.Start(Logger, "Saving Chunks...");
            chunkIO.Save(worldName);

            ProfilingHelper.End(Logger, "Done.");

            var chunkLoadManager = new ChunkLoadManager(chunkMesher, prototype.ChunkManager, prototype.EntityManager, chunkIO, entIO);

            //var player = new Player(0, PlayerManagerIO.GetHashCodeForName(GlobalState.GameStateManager.TheIsland.localPlayerName), true);
            //prototype.EntityManager.Add(player, true);

            //player.Position = playerSpawnPosition;
            //player.SpawnPosition = CubePosition.FromWorldSpace(playerSpawnPosition);

            World world = new World(prototype, chunkLoadManager, worldInfoIO, entIO, chunkIO, SIZE_IN_CHUNKS * Chunk.CHUNK_SIZE);
            if (!GlobalState.IsHeadless)
                world.InitMeshes(device);
            prototype.Logic.Initialize(world);
            entityManager.AddLaterEntities();

            ProfilingHelper.Start(Logger, "Saving Entities...");
            entIO.SerializeAll(SIZE_IN_CHUNKS);
            entIO.Save(worldName);

            var playerIO = new PlayerManagerIO();
            playerIO.SerializeAll(world);
            playerIO.Save(worldName);

            ProfilingHelper.End(Logger, "Done.");

            ProfilingHelper.Start(Logger, "Reloading...");

            //The way world creation is set up is that it creates everything - the entire world - at the same time.
            //That means we'd have entirely too much stuff in memory after we're done. We're not going to be near half of that stuff.
            //Instead of letting that sit in memory, we just unload EVERYTHING
            //then reload the things that are nearby.
            //The World is responsible for the loading later.
            //Note that the ChunkLoadManager isn't aware that anything is loaded (since we don't use the ChunkLoadManager for world generation).
            //So we just call the raw Unload functions.
            world.isCreateWorldReloading = true;
            entityManager.UnloadAll();
            entIO.RemoveSerializedIdsFromFreeList(entityManager.GetFreeList());
            Array.Fill(world.player, null);
            world.isCreateWorldReloading = false;
            //chunkLoadManager.UnloadAll();

            worldInfoIO.Save(worldName, world.WorldInfo);
            GlobalState.SessionIO?.Save();

            ProfilingHelper.End(Logger, "Done.");

            return world;
        }

        public void LoadNone()
        {
            using var zone = TracyImpl.Tracy.BeginZone();

            const int SIZE_IN_CHUNKS = 32;
            const int SIZE_IN_CUBES = SIZE_IN_CHUNKS * Chunk.CHUNK_SIZE;

            int spawnX = GlobalState.random.Next(SIZE_IN_CUBES / 2 - 4, SIZE_IN_CUBES / 2 + 4);
            int spawnZ = GlobalState.random.Next(SIZE_IN_CUBES / 2 - 4, SIZE_IN_CUBES / 2 + 4);

            CubePosition defaultPlayerSpawnLocation = CubePosition.FromWorldSpace(
                new Vector3(SIZE_IN_CUBES * Cube.CUBE_SCALE / 2f, SIZE_IN_CUBES * Cube.CUBE_SCALE, SIZE_IN_CUBES * Cube.CUBE_SCALE / 2f));
            defaultPlayerSpawnLocation.X = spawnX;
            defaultPlayerSpawnLocation.Z = spawnZ;
            defaultPlayerSpawnLocation.Y = SIZE_IN_CUBES;

            ProfilingHelper.Start(Logger, "Loading world...");
            LoadMessage = "Loading World...";
            var entityManager = new EntityManager();
            var inventoryManager = new InventoryManager();
            var worldInfoIO = new WorldInfoIO();

            LoadMessage = "Loading World...\n" +
                "Reading from disk...";
            var physicsInfo = new PhysicsInfo();

            var chunkMesher = ChunkMesher.CollisionOnly(SIZE_IN_CHUNKS, physicsInfo);// new ChunkMesher(SIZE_IN_CHUNKS, physicsInfo, device);
            var chunkIO = new ChunkManagerIO(SIZE_IN_CHUNKS, "test", 0);
            var entIO = new EntityManagerIO(entityManager, 0);
            var chunkManager = new ChunkManager(SIZE_IN_CHUNKS, chunkIO, entityManager.MeshCubeTrackers, chunkMesher);
            var housingManager = new HousingManager();

            var logic = CreateLayerLogic(0);

            WorldPrototype prototype = new WorldPrototype(null, 0, entityManager, inventoryManager, chunkManager, WorldInfoIO.WorldInfo.Empty, logic, new Skybox(), physicsInfo, housingManager);

            var ChunkLoadManager = new ChunkLoadManager(chunkMesher, prototype.ChunkManager, prototype.EntityManager, chunkIO, entIO);

            LoadMessage = "Loading World...\n" +
                "Deserializing...";

            ProfilingHelper.End(Logger, "World loading done.");

            World world = new World(prototype, ChunkLoadManager, worldInfoIO, entIO, chunkIO, SIZE_IN_CHUNKS * Chunk.CHUNK_SIZE);
            world.InitMeshes(device);
            prototype.Logic.Initialize(world);

            this.world = world;
            world.FinishLoading(device);

            IsLoading = false;
        }

        public World LoadWorld(GraphicsDevice device, string worldName)
        {
            using var zone = TracyImpl.Tracy.BeginZone();

            const int SIZE_IN_CHUNKS = 32;
            const int SIZE_IN_CUBES = SIZE_IN_CHUNKS * Chunk.CHUNK_SIZE;

            int spawnX = GlobalState.random.Next(SIZE_IN_CUBES / 2 - 4, SIZE_IN_CUBES / 2 + 4);
            int spawnZ = GlobalState.random.Next(SIZE_IN_CUBES / 2 - 4, SIZE_IN_CUBES / 2 + 4);

            CubePosition defaultPlayerSpawnLocation = CubePosition.FromWorldSpace(
                new Vector3(SIZE_IN_CUBES * Cube.CUBE_SCALE / 2f, SIZE_IN_CUBES * Cube.CUBE_SCALE, SIZE_IN_CUBES * Cube.CUBE_SCALE / 2f));
            defaultPlayerSpawnLocation.X = spawnX;
            defaultPlayerSpawnLocation.Z = spawnZ;
            defaultPlayerSpawnLocation.Y = SIZE_IN_CUBES;

            ProfilingHelper.Start(Logger, "Loading world...");
            LoadMessage = "Loading World...";
            var entityManager = new EntityManager();
            var inventoryManager = new InventoryManager();
            var worldInfoIO = new WorldInfoIO();

            LoadMessage = "Loading World...\n" +
                "Reading from disk...";
            WorldIO.LoadError error = worldInfoIO.Load(worldName, out WorldInfoIO.WorldInfo worldInfo);
            if (worldInfoIO.HandleError(error, worldName))
                return null;

            var physicsInfo = new PhysicsInfo();

            var chunkMesher = ChunkMesher.CollisionOnly(SIZE_IN_CHUNKS, physicsInfo);
            var chunkIO = new ChunkManagerIO(SIZE_IN_CHUNKS, "test", worldInfo.playerLayers[0]);
            var entIO = new EntityManagerIO(entityManager, worldInfo.playerLayers[0]);
            var chunkManager = new ChunkManager(SIZE_IN_CHUNKS, chunkIO, entityManager.MeshCubeTrackers, chunkMesher);
            var housingManager = new HousingManager();
            housingManager.FinishLoading(worldInfo);

            GlobalState.SessionInformation.LastLoadedSave = worldName;

            var logic = CreateLayerLogic(worldInfo.playerLayers[0]);

            WorldPrototype prototype = new WorldPrototype(worldName, worldInfo.playerLayers[0], entityManager, inventoryManager, chunkManager, worldInfo, logic, new Skybox(), physicsInfo, housingManager);

            error = chunkIO.Load(worldName);
            if (chunkIO.HandleError(error, worldName))
                return null;

            error = entIO.Load(worldName);//saver.Load(device, this, folderName);
            entIO.RemoveSerializedIdsFromFreeList(entityManager.GetFreeList());
            if (entIO.HandleError(error, worldName))
                return null;

            var ChunkLoadManager = new ChunkLoadManager(chunkMesher, prototype.ChunkManager, prototype.EntityManager, chunkIO, entIO);

            LoadMessage = "Loading World...\n" +
                "Deserializing...";

            ProfilingHelper.End(Logger, "World loading done.");

            World world = new World(prototype, ChunkLoadManager, worldInfoIO, entIO, chunkIO, SIZE_IN_CHUNKS * Chunk.CHUNK_SIZE);
            world.InitMeshes(device);
            prototype.Logic.Initialize(world);
            return world;

            //EntityManager.Add(new EntityLeviathan());
        }

        public World LoadLayer(string worldName, int layer)
        {
            using var zone = TracyImpl.Tracy.BeginZone();

            const int SIZE_IN_CHUNKS = 32;
            const int SIZE_IN_CUBES = SIZE_IN_CHUNKS * Chunk.CHUNK_SIZE;

            int spawnX = GlobalState.random.Next(SIZE_IN_CUBES / 2 - 4, SIZE_IN_CUBES / 2 + 4);
            int spawnZ = GlobalState.random.Next(SIZE_IN_CUBES / 2 - 4, SIZE_IN_CUBES / 2 + 4);

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

            if (worldInfo.furthestLayer < layer)
            {
                ProfilingHelper.Start(Logger, "Creating Unvisited Layer...");

                worldInfo.furthestLayer = layer;

                var entityManager = new EntityManager();
                var inventoryManager = new InventoryManager();
                
                var physicsInfo = new PhysicsInfo();

                var chunkMesher = ChunkMesher.CollisionOnly(SIZE_IN_CHUNKS, physicsInfo);
                var chunkIO = new ChunkManagerIO(SIZE_IN_CHUNKS, "test", layer);
                var entIO = new EntityManagerIO(entityManager, layer);
                var chunkManager = new ChunkManager(SIZE_IN_CHUNKS, chunkIO, entityManager.MeshCubeTrackers, chunkMesher);

                Skybox skybox = new Skybox();

                GlobalState.SessionInformation.LastLoadedSave = worldName;

                var generator = CreateLayerGenerator(layer);
                var logic = CreateLayerLogic(layer);

                WorldPrototype prototype = new WorldPrototype(worldName, layer, entityManager, inventoryManager, chunkManager, worldInfo, logic, skybox, physicsInfo, new HousingManager());

                ChunkGeneratorTasker.GenerateWorld(prototype, generator);

                ProfilingHelper.Start(Logger, "Saving Chunks...");
                chunkIO.Save(worldName);

                ProfilingHelper.End(Logger, "Done.");

                var chunkLoadManager = new ChunkLoadManager(chunkMesher, prototype.ChunkManager, prototype.EntityManager, chunkIO, entIO);

                World world = new World(prototype, chunkLoadManager, worldInfoIO, entIO, chunkIO, SIZE_IN_CHUNKS * Chunk.CHUNK_SIZE);
                prototype.Logic.Initialize(world);
                entityManager.AddLaterEntities();

                ProfilingHelper.Start(Logger, "Saving Entities...");
                entIO.SerializeAll(SIZE_IN_CHUNKS);
                entIO.Save(worldName);
                ProfilingHelper.End(Logger, "Done.");

                ProfilingHelper.Start(Logger, "Reloading...");
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

                ProfilingHelper.End(Logger, "Done.");

                return world;
            }
            else
            {
                ProfilingHelper.Start(Logger, "Loading Layer...");

                var entityManager = new EntityManager();
                var inventoryManager = new InventoryManager();

                var physicsInfo = new PhysicsInfo();

                var chunkMesher = ChunkMesher.CollisionOnly(SIZE_IN_CHUNKS, physicsInfo);
                var chunkIO = new ChunkManagerIO(SIZE_IN_CHUNKS, "test", layer);
                var entIO = new EntityManagerIO(entityManager, layer);
                var chunkManager = new ChunkManager(SIZE_IN_CHUNKS, chunkIO, entityManager.MeshCubeTrackers, chunkMesher);

                var housingManager = new HousingManager();
                housingManager.FinishLoading(worldInfo);

                GlobalState.SessionInformation.LastLoadedSave = worldName;
                var logic = CreateLayerLogic(layer);

                Skybox skybox = new Skybox();

                WorldPrototype prototype = new WorldPrototype(worldName, layer, entityManager, inventoryManager, chunkManager, worldInfo, logic, skybox, physicsInfo, housingManager);

                error = chunkIO.Load(worldName);
                if (chunkIO.HandleError(error, worldName))
                    return null;

                error = entIO.Load(worldName);//saver.Load(device, this, folderName);
                entIO.RemoveSerializedIdsFromFreeList(entityManager.GetFreeList());
                if (entIO.HandleError(error, worldName))
                    return null;

                var ChunkLoadManager = new ChunkLoadManager(chunkMesher, prototype.ChunkManager, prototype.EntityManager, chunkIO, entIO);

                LoadMessage = "Loading World...\n" +
                    "Deserializing...";

                ProfilingHelper.End(Logger, "World loading done.");

                World world = new World(prototype, ChunkLoadManager, worldInfoIO, entIO, chunkIO, SIZE_IN_CHUNKS * Chunk.CHUNK_SIZE);
                world.InitMeshes(device);
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

        private static ChunkGenerator CreateLayerGenerator(int layer)
        {
            if (GlobalState.Registry.WorldLogicRegistry.generators == null || GlobalState.Registry.WorldLogicRegistry.generators.Length < layer || GlobalState.Registry.WorldLogicRegistry.generators[layer] == null) 
                throw new Exception(string.Format("No LayerGenerator defined for layer {0}", layer));
            ChunkGenerator generator = (ChunkGenerator)Activator.CreateInstance(GlobalState.Registry.WorldLogicRegistry.generators[layer], layer, 0);

            //switch (layer)
            //{
            //    case 0:
            //        generator = new ChunkGeneratorIsland(layer);
            //        break;
            //    case 1:
            //        generator = new ChunkGeneratorCatacombs();
            //        break;
            //    default:
            //        Console.WriteLine("LAYER {0} HAS NOT YET BEEN FILLED OUT YET AND IS UNIMPLEMENTED!", layer);
            //        generator = null;
            //        break;
            //}

            return generator;
        }

        private static WorldLogics.WorldLogic CreateLayerLogic(int layer)
        {
            if (GlobalState.Registry.WorldLogicRegistry.logics == null || GlobalState.Registry.WorldLogicRegistry.logics.Length < layer || GlobalState.Registry.WorldLogicRegistry.logics[layer] == null)
                throw new Exception(string.Format("No WorldLogic defined for layer {0}", layer));

            WorldLogics.WorldLogic logic = (WorldLogics.WorldLogic)Activator.CreateInstance(GlobalState.Registry.WorldLogicRegistry.logics[layer]);

            //switch (layer)
            //{
            //    case 0:
            //        logic = new WorldLogics.WorldLogicIsland(worldName, device);
            //        break;
            //    case 1:
            //        logic = new WorldLogics.WorldLogicCatacombs(device);
            //        break;
            //    default:
            //        Console.WriteLine("LAYER {0} HAS NOT YET BEEN FILLED OUT YET AND IS UNIMPLEMENTED!", layer);
            //        logic = null;
            //        break;
            //}

            return logic;
        }

        public void Save(bool backup)
        {
            if (world != null)
            {
                var watch = Stopwatch.StartNew();
                GlobalState.SessionInformation.LastLoadedSave = world.LoadedFolderName;
                GlobalState.SessionIO.Save();

                if (backup)
                {
                    string dir = string.Format("saves_bkp/{0}", DateTime.Now.ToString("yyyy-MM-dd"));
                    Directory.CreateDirectory(dir);
                    using (FileStream fs = new FileStream(string.Format("{0}/{1}-{2}.zip", dir, world.LoadedFolderName, DateTime.Now.ToString("hh-mm-ss")), FileMode.Create, FileAccess.Write))
                    {
                        ZipFile.CreateFromDirectory(string.Format("{0}{1}", WorldIO.SaveFolder, world.LoadedFolderName), fs);
                    }
                }
                world.SaveWorld();
                playerIO?.SerializeAll(world);
                playerIO?.Save(world.LoadedFolderName);
                playerIO?.DecacheCurrentlySerialized(world.EntityManager);

                IMGUIConsole.LogLineAndSend(string.Format("Saved Game in {0} seconds", watch.Elapsed.TotalSeconds));
            }
        }

        public override void Draw(GraphicsDevice device, SpriteBatch batch, double deltaTime)
        {
            using var zone = TracyImpl.Tracy.BeginZone();

            base.Draw(device, batch, deltaTime);

            if (client != null)
            {
                client.Render(device, batch, deltaTime);
            }

            //if (GlobalState.gameStateManager.netMode == GameStateManager.NetworkingMode.Singleplayer && world != null)
            //{
            //    world.DrawDebug(device);
            //}
        }

        public override void DrawUI(SpriteBatch batch)
        {
            using var zone = TracyImpl.Tracy.BeginZone();

            if (!IsLoading)
            {
                base.DrawUI(batch);
            }

            if (client != null)
                client.RenderUI(device, batch, 0);

            IMGUINetworkDebug.Render(batch);

            //if (world != null && !IsLoading)
            //{
            //    world.DrawUI(batch);
            //}

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

            if (GlobalState.GameStateManager.netMode == GameStateManager.NetworkingMode.Client && !netManagerClient!.ClientHasConnected())
            {
                TextHelper.DrawText(batch, fi, string.Format("Connecting to {0}:{1}...", netManagerClient.Ip, netManagerClient.Port), Color.White,
                    new Rectangle(0, 0, Options.CurrentWindowResolution.X, Options.CurrentWindowResolution.Y),
                    Enums.Alignment.Center, Options.CurrentWindowResolution.X, 1);
            }

            StringBuilder sb = new StringBuilder();
            sb.Append("This is a ");
            switch (GlobalState.GameStateManager.netMode)
            {
                case GameStateManager.NetworkingMode.Client:
                    sb.Append("Client session. Connected to: ");
                    sb.Append(netManagerClient?.netManager.FirstPeer?.ToString());
                    sb.Append(".");
                    break;
                case GameStateManager.NetworkingMode.Server:
                    sb.Append("Server session. There are ");
                    sb.Append(netManagerServer?.uniqueNetPlayers);
                    sb.Append(" connected players.");
                    break;
                case GameStateManager.NetworkingMode.Singleplayer:
                    sb.Append("Singleplayer session.");
                    break;
            }
            TextHelper.DrawText(batch, fi, sb.ToString(), Color.White,
                    new Rectangle(0, (int)(fi.font.LineSpacing * 1.5f), Options.CurrentWindowResolution.X, Options.CurrentWindowResolution.Y),
                    Enums.Alignment.TopLeft, Options.CurrentWindowResolution.X, 1);
        }

        public override void Dispose()
        {
            base.Dispose();

            client?.Dispose();
            world?.Dispose();
        }
    }
}
