using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Entities;

namespace ViMG.GameStates
{
    public class WorldPrototype
    {
        public ChunkManager ChunkManager;
        public EntityManager EntityManager;
        public WorldInfoIO.WorldInfo WorldInfo;
        public WorldLogics.WorldLogic Logic;
        public Skybox Skybox;

        public string WorldName;
        public int Layer;

        public WorldPrototype(string worldName, int layer, EntityManager entityManager, ChunkManager chunkManager, WorldInfoIO.WorldInfo worldInfo, WorldLogics.WorldLogic logic, Skybox skybox)
        {
            this.WorldName = worldName;
            this.Layer = layer;

            this.EntityManager = entityManager;
            this.ChunkManager = chunkManager;
            WorldInfo = worldInfo;
            Logic = logic;
            Skybox = skybox;
        }

        public void AddEntity(Entity entity)
        {
            //prototype entities should delay adding since initialization relies on World and not WorldPrototype.
            EntityManager.Add(entity, true);
        }
    }
}
