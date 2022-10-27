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
    public class PSStoneBeetle : PassiveSpawner
    {
        private List<StoneBeetle> beetles = new List<StoneBeetle>();

        public PSStoneBeetle(PassiveSpawnerManager manager, EntityManager entityManager) : base(manager, 3f, 1f / 5f,
            new Rectangle3D(new Vector3(112, 0, 112) * Cube.CUBE_SCALE, new Vector3(512 - 112, 512, 512 - 112) * Cube.CUBE_SCALE))
        {
            entityManager.OnEntityAdded += OnEntityAdded;
            entityManager.OnEntityRemoved += OnEntityRemoved;
        }

        private void OnEntityAdded(Entity entity)
        {
            if (entity is StoneBeetle s)
                beetles.Add(s);
        }

        private void OnEntityRemoved(Entity entity)
        {
            if (entity is StoneBeetle s)
                beetles.Remove(s);
        }

        public override bool CanAreaSpawn(World world, ChunkManager manager, Chunk chunk, CubePosition position)
        {
            if (!chunk.Initialized)// || Main.camera.FrustumContains(position.InWorldSpace(null)))
                return false;

            if (position.Y > 160)
                return false;

            Cube c = chunk.GetData().GetCube(position).GetOrDefault(Main.Registry.CubeRegistry.Air);
            if (c == Main.Registry.CubeRegistry.Get("dirt") || c == Main.Registry.CubeRegistry.Get("stone"))
                return true;

            return false;
        }

        protected override void Spawn(World world, CubePosition position)
        {
            if (beetles.Count < GetSpawnCap())
            {
                StoneBeetle snake = new StoneBeetle(position.InWorldSpace(null) + new Vector3(0, Cube.CUBE_SCALE, 0));
                world.EntityManager.Add(snake);
            }
        }
    }
}
