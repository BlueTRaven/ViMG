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

        public WorldPrototype(EntityManager entityManager, ChunkManager chunkManager, WorldInfoIO.WorldInfo worldInfo)
        {
            this.EntityManager = entityManager;
            this.ChunkManager = chunkManager;
            WorldInfo = worldInfo;
        }
    }
}
