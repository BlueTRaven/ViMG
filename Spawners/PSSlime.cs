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
        private List<SlimeBig> bigSlimes = new List<SlimeBig>();

        public PSSlime(PassiveSpawnerManager manager, EntityManager entityManager) : base(manager, 0.5f, 1f / 5f, 
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
            if (entity is Slime s)
                slimes.Add(s);

            if (entity is SlimeBig sb)
                bigSlimes.Add(sb);
        }

        private void OnEntityRemoved(Entity entity)
        {
            if (entity is Slime s)
                slimes.Remove(s);
            if (entity is SlimeBig sb)
                bigSlimes.Remove(sb);
        }

        public override bool CanAreaSpawn(World world, ChunkManager manager, Chunk chunk, CubePosition position)
        {
            //Don't spawn at night, and don't spawn when the player is looking at the given position.
            if (!chunk.Initialized || world.IsNight())// || Main.camera.FrustumContains(position.InWorldSpace(null)))
                return false;

            if (position.Y < 181)
                return false;

            Cube c = chunk.GetData().GetCube(position).GetOrDefault(Main.Registry.CubeRegistry.Air);
            if (c == Main.Registry.CubeRegistry.Get("dirt") || c == Main.Registry.CubeRegistry.Get("grass"))
                return true;

            return false;
        }

        public override void Update(double deltaTime, World world)
        {
            base.Update(deltaTime, world);
        }

        protected override void Spawn(World world, CubePosition position)
        {
            if (bigSlimes.Count < 4 && Main.random.Next(0, 4) == 0)
            {
                SlimeBig bigSlime = new SlimeBig(position.InWorldSpace(null) + new Vector3(0, Cube.CUBE_SCALE * 2, 0));
                world.EntityManager.Add(bigSlime);
                return;
            }

            if (slimes.Count < GetSpawnCap())
            {
                Slime slime = new Slime(position.InWorldSpace(out bool ok) + new Vector3(0, Cube.CUBE_SCALE, 0));
                world.EntityManager.Add(slime);
            }
        }
    }
}
