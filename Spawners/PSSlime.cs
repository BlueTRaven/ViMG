using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Cubes;
using ViMG.Entities;

namespace ViMG.Spawners
{
    public class PSSlime : PassiveSpawner
    {
        private List<Slime> slimes = new List<Slime>();
        private List<Slime> toRemove = new List<Slime>();

        public PSSlime() : base(0.5f, 1f / 32f)
        {
        }

        public override bool CanAreaSpawn(ChunkManager manager, Chunk chunk, CubePosition position)
        {
            return true;
        }

        public override void Update(double deltaTime, World world)
        {
            base.Update(deltaTime, world);

            foreach (Slime slime in slimes)
            {
                if (slime.Dead)
                    toRemove.Add(slime);
            }

            foreach (Slime slime in toRemove)
            {
                slimes.Remove(slime);
            }

            toRemove.Clear();
        }

        protected override void Spawn(World world)
        {
            if (slimes.Count < 32)
            {
                int minR = 112;
                int maxR = world.sizeInCubes - 112;
                var pos = world.ChunkManager.GetFirstSolidDown(new Vector3(Main.random.Next(minR, maxR), world.sizeInCubes, Main.random.Next(minR, maxR)) * Cube.CUBE_SCALE);

                if (pos.HasValue() && CanAreaSpawn(world.GetChunkManager(), world.GetChunkManager().GetChunk(pos.Get()), pos.Get()))
                {
                    Slime slime = new Slime(pos.Get().InWorldSpace(out bool ok) + new Vector3(0, Cube.CUBE_SCALE, 0));
                    slimes.Add(slime);

                    world.EntityManager.Add(slime);
                }
            }
        }
    }
}
