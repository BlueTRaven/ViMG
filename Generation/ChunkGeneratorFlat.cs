using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Cubes;

namespace ViMG.Generation
{
    public class ChunkGeneratorFlat : ChunkGenerator
    {
        public ChunkGeneratorFlat(int layer) : base(layer)
        {

        }

        public override Vector3 GetPlayerPosition(World world, ChunkManager chunks)
        {
            int x = Main.random.Next(world.sizeInCubes / 2 - 4, world.sizeInCubes / 2 + 4);
            int z = Main.random.Next(world.sizeInCubes / 2 - 4, world.sizeInCubes / 2 + 4);

            CubePosition playerPos = CubePosition.FromWorldSpace(new Vector3(world.sizeInCubes * Cube.CUBE_SCALE / 2f, world.sizeInCubes * Cube.CUBE_SCALE, world.sizeInCubes * Cube.CUBE_SCALE / 2f));
            playerPos.X = x;
            playerPos.Z = z;
            playerPos.Y = world.sizeInCubes;

            return world.GetFirstSolidDown(playerPos.InWorldSpace(null)).InWorldSpace(null) + new Vector3(0, Cube.CUBE_SCALE * 3, 0);
        }

        public override void GenerateChunkBroad(ChunkManager.BroadGenerationState state)
        {
            var cubes = state.chunk.GetData().GetAll();

            for (int x = 0; x < Chunk.CHUNK_SIZE; x++)
            {
                for (int y = 0; y < Chunk.CHUNK_SIZE; y++)
                {
                    for (int z = 0; z < Chunk.CHUNK_SIZE; z++)
                    {
                        var pos = new CubePosition(x, y, z, CubePosition.CoordinateSpace.ChunkSpace);
                        pos = pos.InCubeSpace(state.chunk);

                        ushort id = 0;
                        if (pos.Y < 256)
                            id = 1;

                        cubes[x + Chunk.CHUNK_SIZE * (y + Chunk.CHUNK_SIZE * z)] = id;
                    }
                }
            }

            state.chunk.GetData().GenStep = ChunkData.GenerationStep.Done;
        }

        public override void GenerateChunkDetail(ChunkManager manager, Chunk chunk, ChunkPosition position)
        {
            HandleCascaded();
        }
    }
}
