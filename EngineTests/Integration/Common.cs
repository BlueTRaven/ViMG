using Engine;
using Engine.Common;
using Engine.Items;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Threading.Tasks;
using ViMG;
using ViMG.Entities;

namespace EngineTests.Integration
{
    public static class Common
    {
        public static Logger Logger = Logger.InitLogger("TestCommon", true, Logger.LogLevel.Debug);
        public static EntityManager.EntityReference CreateEntity(CancellationToken ct, HeadlessRunner runner, CreateEntityParams parameters)
        {
            var ret = runner.PostFnAndWait(ct, CreateEntityFn, parameters).output;
            Assert.IsNotNull(ret);
            Assert.IsInstanceOfType(ret, typeof(EntityManager.EntityReference));
            return (EntityManager.EntityReference)ret;
        }

        public struct CreateEntityParams
        {
            public int id;
            public Vector3 position;
        }

        private static object? CreateEntityFn(HeadlessRunner runner, object? o)
        {
            Assert.IsNotNull(o);
            Assert.IsInstanceOfType<CreateEntityParams>(o);
            int id = ((CreateEntityParams)o).id;
            Vector3 position = ((CreateEntityParams)o).position;

            var world = GlobalState.GameStateManager.TheIsland.GetWorld();
            Assert.IsNotNull(world);

            var entityType = GlobalState.Registry.EntityRegistry.Get(id);
            Assert.IsNotNull(entityType);

            Entity? n = entityType.New();
            Assert.IsNotNull(n);

            n.Position = position;

            world.EntityManager.Add(n);

            var reference = world.EntityManager.GetReference(n);
            Assert.AreNotEqual(reference, new EntityManager.EntityReference { id = -1, generation = -1 });
            return reference;
        }

        public static bool UnloadEntity(CancellationToken ct, HeadlessRunner runner, EntityManager.EntityReference reference)
        {
            var ret = runner.PostFnAndWait(ct, UnloadEntityFn, reference).output;
            Assert.IsNotNull(ret);
            Assert.IsInstanceOfType<bool>(ret);
            return (bool)ret;
        }

        private static object? UnloadEntityFn(HeadlessRunner runner, object? o)
        {
            Assert.IsNotNull(o);
            Assert.IsInstanceOfType<EntityManager.EntityReference>(o);
            EntityManager.EntityReference reference = (EntityManager.EntityReference)o;

            var world = GlobalState.GameStateManager.TheIsland.GetWorld();
            Assert.IsNotNull(world);

            var entity = world.EntityManager.GetByRefServer(ref reference);
            
            if (entity != null)
            {
                world.EntityManager.Unload(entity);

                Assert.IsNull(world.EntityManager.GetByRefServer(ref reference));
                return true;
            }
            return false;
        }

        public static void UnloadAllEntities(CancellationToken ct, HeadlessRunner runner)
        {
            runner.PostFnAndWait(ct, UnloadAllEntitiesFn, null);
        }

        private static object? UnloadAllEntitiesFn(HeadlessRunner runner, object? o)
        {
            var world = GlobalState.GameStateManager.TheIsland.GetWorld();
            Assert.IsNotNull(world);

            world.EntityManager.UnloadAll();

            return null;
        }

        public static void ValidateAllUnloaded(CancellationToken ct, HeadlessRunner runner)
        {
            runner.PostFnAndWait(ct, ValidateAllUnloadedFn, null);
        }

        private static object? ValidateAllUnloadedFn(HeadlessRunner runner, object? o)
        {
            var world = GlobalState.GameStateManager.TheIsland.GetWorld();
            Assert.IsNotNull(world);

            Assert.IsEmpty(world.EntityManager.GetEntities());
            foreach (var h in world.HitboxManager.GetAll())
            {
                if (h.active)
                {
                    Logger.Error("Hitbox {0} is active when it should not be", h.index);
                    Assert.Fail(string.Format("Hitbox {0} is active when it should not be", h.index));
                }
            }

            var invMax = InventoryManager.InvMax;
            for (int i = 0; i < invMax; i++)
            {
                var reference = world.InventoryManager.GetReference(i);
                if (world.InventoryManager.Get(reference) != null)
                {
                    Logger.Error("Inventory {0} {1} is active when it should not be", reference.id, reference.generation);
                    Assert.Fail(string.Format("Inventory {0} {1} is active when it should not be", reference.id, reference.generation));
                }
            }

            return null;
        }

        public static ChunkPosition GetSpawnChunk(CancellationToken ct, HeadlessRunner runner)
        {
            var ret = runner.PostFnAndWait(ct, GetSpawnChunkFn, null).output;
            Assert.IsNotNull(ret);
            Assert.IsInstanceOfType<ChunkPosition>(ret);

            return (ChunkPosition)ret;
        }

        private static object? GetSpawnChunkFn(HeadlessRunner runner, object? o)
        {
            var world = GlobalState.GameStateManager.TheIsland.GetWorld();
            Assert.IsNotNull(world);

            return ChunkPosition.WorldSpaceChunk(world.WorldInfo.spawnPosition);
        }

        public static void ForceLoadChunk(CancellationToken ct, HeadlessRunner runner, ChunkPosition position)
        {
            runner.PostFnAndWait(ct, ForceLoadChunkFn, position);
        }

        private static object? ForceLoadChunkFn(HeadlessRunner runner, object? o)
        {
            Assert.IsNotNull(o);
            Assert.IsInstanceOfType<ChunkPosition>(o);
            ChunkPosition spawnChunk = (ChunkPosition)o;

            var world = GlobalState.GameStateManager.TheIsland.GetWorld();
            Assert.IsNotNull(world);

            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    for (int z = -1; z <= 1; z++)
                    {
                        world.ChunkLoadManager.LoadChunk(world, spawnChunk + new ChunkPosition(x, y, z), -1, true);
                    }
                }
            }
            return null;
        }

        public struct RunParams
        {
            public string[] args;
            public HeadlessRunner runner;
        }

        public static void Run(object? o)
        {
            RunParams p = o as RunParams? ?? throw new Exception();
            GlobalState.Args.ParseArgs(p.args);
            p.runner.Run();
        }
    }
}
