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
    public class PSSKeleton : PassiveSpawner
    {
        private List<Skeleton> skeletons = new List<Skeleton>();

        public PSSKeleton(EntityManager entityManager) : base(0.5f, 1f / 10f,
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
            if (entity is Skeleton s)
                skeletons.Add(s);
        }

        private void OnEntityRemoved(Entity entity)
        {
            if (entity is Skeleton s)
                skeletons.Remove(s);
        }

        public override bool CanAreaSpawn(ChunkManager manager, Chunk chunk, CubePosition position)
        {
            //Don't spawn during the day, and don't spawn when the player is looking at the given position.
            if (!chunk.GetWorld().IsNight() || Main.camera.FrustumContains(position.InWorldSpace(null)))
                return false;

            if (position.Y < 181)
                return false;

            Cube c = chunk.GetData().GetCube(position).GetOrDefault(Main.Registry.CubeRegistry.Air);
            if (c == Main.Registry.CubeRegistry.Get("dirt") || c == Main.Registry.CubeRegistry.Get("grass") || 
                c == Main.Registry.CubeRegistry.Get("stone"))
                return true;

            return false;
        }

        protected override void Spawn(World world, CubePosition position)
        {
            if (skeletons.Count < 32)
            {
                int minR = 112;
                int maxR = world.sizeInCubes - 112;

                if (position.X < minR || position.Z < minR || position.X > maxR || position.Z > maxR)
                    return;

                Skeleton slime = new Skeleton(position.InWorldSpace(null) + new Vector3(0, Cube.CUBE_SCALE, 0));
                world.EntityManager.Add(slime);
            }
        }
    }
}
