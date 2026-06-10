using Engine.ChunkStuff;
using Engine.Common.Entities;
using Engine.Items;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SharpDX.Direct2D1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG;
using ViMG.Cubes;
using ViMG.Entities;
using ViMG.GameStates;
using ViMG.Generation;
using ViMG.Physics;

namespace Engine.Server
{
    public class WorldTask
    {
        private static Logger Logger = Logger.InitLogger("WorldTask", true, Logger.LogLevel.Info);

        public readonly string saveName;
        public readonly int layer;

        // If true, generates instead of loads. Assumes that the layer does not already exist.
        public readonly bool generate;

        public readonly Task<World> task;

        public WorldTask(string saveName, int layer)
        {
            this.saveName = saveName;
            this.layer = layer;

            this.task = new Task<World>(StateFn, state: this);
        }

        private static World StateFn(object? obj)
        {
            return DoLoadWorld((WorldTask)obj);
        }

        private static World DoLoadWorld(WorldTask state)
        {
            ProfilingHelper.Start(Logger, "Loading and Flushing World...");

            if (!Directory.Exists(WorldIO.SaveFolder + state.saveName + "/"))
                throw new Exception(string.Format("Save directory {0} does not exist", WorldIO.SaveFolder + state.saveName + "/"));

            World? world;
            if (state.generate)
            {
                Logger.Info("Creating world {0} layer {1}", state.saveName, state.layer);
                world = GenerateWorld(state.saveName, state.layer);
            }
            else
            {
                Logger.Info("Loading world {0} layer {1}", state.saveName, state.layer);
                world = LoadWorld(state.saveName, state.layer);
            }

            if (world == null) throw new Exception("Errored while loading world");

            ServerState.LoadMessage = "Loading World...";
            ProfilingHelper.Start(Logger, "Building Meshes...");

            ServerState.LoadMessage = "Loading World...\nFlushing queue...";
            //Finally, tell the ChunkLoadManager to actually load the things.
            //(We have to tell it this manually as it queues things up to load, and we want it to finish loading instead of load things in the background
            //as it normally does.)
            world.ChunkLoadManager.FlushLoadQueue(world);
            ProfilingHelper.End(Logger, "Done Building Meshes.");

            ServerState.LoadMessage = "Loading World...\nFinishing...";
            world.FinishLoading();

            ProfilingHelper.End(Logger, "Finished Loading and Flushing World.");

            return world;
        }

        // Generates a world.
        public static World GenerateWorld(string saveName, int layer)
        {
            using var zone = ViMG.TracyImpl.Tracy.BeginZone();

            const int SIZE_IN_CHUNKS = 32;

            var physicsInfo = new PhysicsInfo();

            var chunkMesher = ChunkMesher.CollisionOnly(SIZE_IN_CHUNKS, physicsInfo);
            var entityManager = new EntityManager();
            var inventoryManager = new InventoryManager();
            var entIO = new EntityManagerIO(entityManager, layer);
            var chunkIO = new ChunkManagerIO(SIZE_IN_CHUNKS, layer);
            var chunkManager = new ChunkManager(SIZE_IN_CHUNKS, chunkIO, entityManager.MeshCubeTrackers, chunkMesher);

            WorldInfoIO.WorldInfo worldInfo = new WorldInfoIO.WorldInfo()
            {
                playerPositions = new Vector3[World.MAX_PLAYERS],
                playerLayers = new int[World.MAX_PLAYERS],
                furthestLayer = 0,
                time = 0,
                pointsOfInterest = new List<PointOfInterest>(),

                flags = new ViMG.WorldLogics.WorldFlags(),

                housings = new List<Housing>()
            };

            var worldInfoIO = new WorldInfoIO();

            // This is up here so we can use this information when loading a world (coconut easter egg)
            // but it also might present a problem; if we error at any point during the creation/loading process,
            // pressing "Continue" will just try to load the same world that caused the error instead of staying the same.
            GlobalState.SessionInformation.LastLoadedSave = saveName;

            var generator = CreateLayerGenerator(layer);
            var logic = CreateLayerLogic(layer);

            chunkIO.CreateAll();

            WorldPrototype prototype = new WorldPrototype(saveName, 0, entityManager, inventoryManager, chunkManager, worldInfo, logic, null, physicsInfo, new HousingManager());

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
            chunkIO.Save(saveName);

            ProfilingHelper.End(Logger, "Done.");

            var chunkLoadManager = new ChunkLoadManager(chunkMesher, prototype.ChunkManager, prototype.EntityManager, chunkIO, entIO);

            World world = new World(prototype, chunkLoadManager, worldInfoIO, entIO, chunkIO, SIZE_IN_CHUNKS * Chunk.CHUNK_SIZE);
            prototype.Logic.Initialize(world);
            entityManager.AddLaterEntities();

            ProfilingHelper.Start(Logger, "Saving Entities...");
            entIO.SerializeAll(SIZE_IN_CHUNKS);
            entIO.Save(saveName);

            var playerIO = new PlayerManagerIO();
            playerIO.SerializeAll(world);
            playerIO.Save(saveName);

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

            worldInfoIO.Save(saveName, world.WorldInfo);
            GlobalState.SessionIO?.Save();

            ProfilingHelper.End(Logger, "Done.");

            return world;
        }

        private static ChunkGenerator CreateLayerGenerator(int layer)
        {
            if (GlobalState.Registry.WorldLogicRegistry.generators == null || GlobalState.Registry.WorldLogicRegistry.generators.Length < layer || GlobalState.Registry.WorldLogicRegistry.generators[layer] == null)
                throw new Exception(string.Format("No LayerGenerator defined for layer {0}", layer));
            ChunkGenerator generator = (ChunkGenerator)Activator.CreateInstance(GlobalState.Registry.WorldLogicRegistry.generators[layer], layer, 0);

            return generator;
        }

        private static ViMG.WorldLogics.WorldLogic CreateLayerLogic(int layer)
        {
            if (GlobalState.Registry.WorldLogicRegistry.logics == null || GlobalState.Registry.WorldLogicRegistry.logics.Length < layer || GlobalState.Registry.WorldLogicRegistry.logics[layer] == null)
                throw new Exception(string.Format("No WorldLogic defined for layer {0}", layer));

            ViMG.WorldLogics.WorldLogic logic = (ViMG.WorldLogics.WorldLogic)Activator.CreateInstance(GlobalState.Registry.WorldLogicRegistry.logics[layer]);

            return logic;
        }

        public static World? LoadWorld(string worldName, int layer)
        {
            using var zone = ViMG.TracyImpl.Tracy.BeginZone();

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
            ServerState.LoadMessage = "Loading World...";
            var entityManager = new EntityManager();
            var inventoryManager = new InventoryManager();
            var worldInfoIO = new WorldInfoIO();

            ServerState.LoadMessage = "Loading World...\n" +
                "Reading from disk...";
            WorldIO.LoadError error = worldInfoIO.Load(worldName, out WorldInfoIO.WorldInfo worldInfo);
            if (worldInfoIO.HandleError(error, worldName))
                return null;

            var physicsInfo = new PhysicsInfo();

            var chunkMesher = ChunkMesher.CollisionOnly(SIZE_IN_CHUNKS, physicsInfo);
            var chunkIO = new ChunkManagerIO(SIZE_IN_CHUNKS, layer);
            var entIO = new EntityManagerIO(entityManager, layer);
            var chunkManager = new ChunkManager(SIZE_IN_CHUNKS, chunkIO, entityManager.MeshCubeTrackers, chunkMesher);
            var housingManager = new HousingManager();
            housingManager.FinishLoading(worldInfo);

            GlobalState.SessionInformation.LastLoadedSave = worldName;

            var logic = CreateLayerLogic(layer);

            WorldPrototype prototype = new WorldPrototype(worldName, layer, entityManager, inventoryManager, chunkManager, worldInfo, logic, new Skybox(), physicsInfo, housingManager);

            error = chunkIO.Load(worldName);
            if (chunkIO.HandleError(error, worldName))
                return null;

            error = entIO.Load(worldName);
            entIO.RemoveSerializedIdsFromFreeList(entityManager.GetFreeList());
            if (entIO.HandleError(error, worldName))
                return null;

            var ChunkLoadManager = new ChunkLoadManager(chunkMesher, prototype.ChunkManager, prototype.EntityManager, chunkIO, entIO);

            ServerState.LoadMessage = "Loading World...\n" +
                "Deserializing...";

            ProfilingHelper.End(Logger, "World loading done.");

            World world = new World(prototype, ChunkLoadManager, worldInfoIO, entIO, chunkIO, SIZE_IN_CHUNKS * Chunk.CHUNK_SIZE);
            prototype.Logic.Initialize(world);
            return world;
        }
    }
}
