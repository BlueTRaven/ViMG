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

        public PassiveSpawner(float checkTime, float spawnChance)
        {
            this.checkTime = checkTime;
            this.checkTimer = checkTime;

            this.spawnChance = spawnChance;
        }

        public virtual void Update(double deltaTime, World world)
        {
            checkTimer -= (float)deltaTime;

            if (checkTimer <= 0)
            {
                checkTimer = checkTime;

                if (Main.random.NextDouble() < spawnChance)
                    Spawn(world);
            }
        }

        protected abstract void Spawn(World world);

        public abstract bool CanAreaSpawn(ChunkManager manager, Chunk chunk, CubePosition position);
    }
}
