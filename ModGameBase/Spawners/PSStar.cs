using Engine;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BrUtility;
using ViMG;
using ViMG.Cubes;
using ViMG.Entities;
using ViMG.Spawners;

namespace ModGameBase.Spawners
{
    public class PSStar : PassiveSpawner
    {
        public PSStar(PassiveSpawnerManager manager) : base(manager, 2f, 1f / 3f, new Rectangle3D(new Vector3(112, 0, 112) * Cube.CUBE_SCALE, new Vector3(512 - 112, 512, 512 - 112) * Cube.CUBE_SCALE))
        {
        }

        protected override bool GetRandomPosition(World world, out CubePosition position)
        {
            // Actual position that this guy spawns in doesn't matter
            position = new CubePosition(0, 0, 0);
            return true;
        }

        public override bool CanAreaSpawn(World world, ChunkManager manager, CubePosition position)
        {
            return world.IsNight() && world.EntityManager.GetAll<ManaStar>().Count == 0;
        }

        protected override void Spawn(World world, CubePosition position)
        {
            world.ChatManager.AddChatMessage("A strange blue star glimmers in the heavens...", Color.Blue);

            world.EntityManager.Add(new ManaStar(new Vector2(GlobalState.random.NextFloat(-70, 70), GlobalState.random.NextFloat(-180, 180))));
        }
    }
}
