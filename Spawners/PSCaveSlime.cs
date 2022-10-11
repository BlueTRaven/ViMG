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
    public class PSCaveSlime : PassiveSpawner
    {
        private List<CaveSlime> slimes = new List<CaveSlime>();

        public PSCaveSlime(PassiveSpawnerManager manager, EntityManager entityManager) : base(manager, 3f, 1f / 5f,
            new Rectangle3D(new Vector3(112, 0, 112) * Cube.CUBE_SCALE, new Vector3(512 - 112, 512, 512 - 112) * Cube.CUBE_SCALE))
        {
            entityManager.OnEntityAdded += OnEntityAdded;
            entityManager.OnEntityRemoved += OnEntityRemoved;
        }

        //We rely on this callback for adding entities as we also want to add entities that are loaded.
        //We could call something that finds all the Slimes in the world once the world has finished loading,
        //but that assumes we're using the same "load the world all at once" style that we're doing.
        private void OnEntityAdded(Entity entity)
        {
            if (entity is CaveSlime s)
                slimes.Add(s);
        }

        private void OnEntityRemoved(Entity entity)
        {
            if (entity is CaveSlime s)
                slimes.Remove(s);
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

        public override void Update(double deltaTime, World world)
        {
            base.Update(deltaTime, world);
        }

        protected override void Spawn(World world, CubePosition position)
        {
            if (slimes.Count < GetSpawnCap())
            {
                CaveSlime slime = new CaveSlime(position.InWorldSpace(out bool ok) + new Vector3(0, Cube.CUBE_SCALE, 0));
                world.EntityManager.Add(slime);
            }
        }
    }
}
