using BrUtility;
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
    public class PSMerchant : PassiveSpawner
    {
        bool spawned = false;

        public PSMerchant(PassiveSpawnerManager manager, EntityManager entityManager) : base(manager, 2f, 1f / 3f,
            new Rectangle3D(new Vector3(112, 0, 112) * Cube.CUBE_SCALE, new Vector3(512 - 112, 512, 512 - 112) * Cube.CUBE_SCALE))
        {
        }

        protected override bool GetRandomPosition(World world, out CubePosition position)
        {
            if (world.WorldInfo.flags.Flags.HasFlag(WorldLogics.WorldFlags.FlagValues.MERCHANT_SPAWNED))
            {
                position = new CubePosition();
                return false;
            }

            Vector2 islandCenter = new Vector2(world.sizeInCubes * Cube.CUBE_SCALE / 2f, world.sizeInCubes * Cube.CUBE_SCALE / 2f);
            Vector2 angleVector = GlobalState.random.NextAngle() * 124 * Cube.CUBE_SCALE;

            Vector3 startPosition = new Vector3(islandCenter.X + angleVector.X, world.sizeInCubes * Cube.CUBE_SCALE, islandCenter.Y + angleVector.Y);

            var p = world.ChunkManager.CubeView.GetFirstSolidDown(CubePosition.FromWorldSpace(startPosition));

            position = p.Get();

            return p.HasValue();
        }

        public override bool CanAreaSpawn(World world, ChunkManager manager, CubePosition position)
        {
            return true;
        }

        protected override void Spawn(World world, CubePosition position)
        {
            world.WorldInfo.flags.Flags |= WorldLogics.WorldFlags.FlagValues.MERCHANT_SPAWNED;

            world.ChatManager.AddChatMessage("A figure washes up on the island's shore...", Color.Purple);

            world.ChunkLoadManager.LoadChunk(world, ChunkPosition.CubeChunk(position));

            world.EntityManager.Add(new TestNPC(position.InWorldSpace()));
        }
    }
}
