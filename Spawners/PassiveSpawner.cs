using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViMG.Spawners
{
    public abstract class PassiveSpawner
    {
        protected float spawnChance;
        private readonly float checkTime;
        private float checkTimer;

        public PassiveSpawner(float checkTime)
        {
            this.checkTime = checkTime;
            this.checkTimer = checkTime;
        }

        public void Update(GameTime gt)
        {
            checkTimer -= (float)gt.ElapsedGameTime.TotalSeconds;

            if (checkTimer <= 0)
            {
                checkTimer = checkTime;
                Spawn();
            }
        }

        protected abstract void Spawn();

        public abstract bool CanAreaSpawn(ChunkManager manager, Chunk chunk, ChunkPosition position);
    }
}
