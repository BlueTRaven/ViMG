using Engine;
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
        private List<Skeleton2> skeletons = new List<Skeleton2>();

        public PSSKeleton(PassiveSpawnerManager manager, EntityManager entityManager) : base(manager, 0.5f, 1f / 10f,
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
            if (entity is Skeleton2 s)
                skeletons.Add(s);
        }

        private void OnEntityRemoved(Entity entity)
        {
            if (entity is Skeleton2 s)
                skeletons.Remove(s);
        }

        public override bool CanAreaSpawn(World world, ChunkManager manager, CubePosition position)
        {
            //Don't spawn during the day
            if (!world.ChunkLoadManager.IsLoaded(ChunkPosition.CubeChunk(position)) || !world.IsNight())
                return false;

            if (position.Y < 181)
                return false;

            Cube c = manager.CubeView.GetCube(position).GetOrDefault(GlobalState.Registry.CubeRegistry.Air);
            if (c == GlobalState.Registry.CubeRegistry.Get("dirt") || c == GlobalState.Registry.CubeRegistry.Get("grass") || 
                c == GlobalState.Registry.CubeRegistry.Get("stone"))
                return true;

            return false;
        }

        protected override void Spawn(World world, CubePosition position)
        {
            if (skeletons.Count < GetSpawnCap())
            {
                int minR = 112;
                int maxR = world.sizeInCubes - 112;

                if (position.X < minR || position.Z < minR || position.X > maxR || position.Z > maxR)
                    return;

                Skeleton2 slime = new Skeleton2(position.InWorldSpace() + new Vector3(0, Cube.CUBE_SCALE, 0));
                world.EntityManager.Add(slime);
            }
        }
    }
}
