using BepuPhysics;
using BepuUtilities.Memory;
using Engine.Items;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Entities;
using ViMG.Physics;
using ViMG.WorldLogics;

namespace ViMG.GameStates
{
    public class WorldPrototype
    {
        public ChunkManager ChunkManager;
        public EntityManager EntityManager;
        public InventoryManager InventoryManager;
        public WorldInfoIO.WorldInfo WorldInfo;
        public WorldLogic Logic;
        public Skybox? Skybox;
        public PhysicsInfo PhysicsInfo;
        public WorldFlags Flags;
        public HousingManager HousingManager;

        public string WorldName;
        public int Layer;

        public WorldPrototype(string worldName, int layer, EntityManager entityManager, InventoryManager inventoryManager, ChunkManager chunkManager, WorldInfoIO.WorldInfo worldInfo, 
            WorldLogics.WorldLogic logic, Skybox? skybox, PhysicsInfo physicsInfo, HousingManager housingManager)
        {
            this.WorldName = worldName;
            this.Layer = layer;

            this.EntityManager = entityManager;
            this.InventoryManager = inventoryManager;
            this.ChunkManager = chunkManager;
            WorldInfo = worldInfo;
            Logic = logic;
            Skybox = skybox;

            this.PhysicsInfo = physicsInfo;

            this.HousingManager = housingManager;
        }

        public void AddEntity(Entity entity)
        {
            //prototype entities should delay adding since initialization relies on World and not WorldPrototype.
            EntityManager.Add(entity, true);
        }
    }
}
