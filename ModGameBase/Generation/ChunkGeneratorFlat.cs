using Engine;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Cubes;
using ViMG.GameStates;

namespace ViMG.Generation
{
    public class ChunkGeneratorFlat : ChunkGenerator
    {
        public ChunkGeneratorFlat(int layer) : base(layer)
        {

        }

        public override Vector3 GetPlayerPosition(ChunkManager chunkManager)
        {
            int x = GlobalState.random.Next(chunkManager.SizeInCubes / 2 - 4, chunkManager.SizeInCubes / 2 + 4);
            int z = GlobalState.random.Next(chunkManager.SizeInCubes / 2 - 4, chunkManager.SizeInCubes / 2 + 4);

            CubePosition playerPos = CubePosition.FromWorldSpace(new Vector3(chunkManager.SizeInCubes * Cube.CUBE_SCALE / 2f,
                chunkManager.SizeInCubes * Cube.CUBE_SCALE, chunkManager.SizeInCubes * Cube.CUBE_SCALE / 2f));
            playerPos.X = x;
            playerPos.Z = z;
            playerPos.Y = chunkManager.SizeInCubes;

            //TODO this should use initializer view
            return chunkManager.CubeView.GetFirstSolidDown(playerPos + new CubePosition(0, 3, 0)).GetOrDefault(playerPos).InWorldSpace();
        }

        public override void GenerateChunkBroad(ChunkGeneratorTasker.BroadGenerationState state)
        {
            for (int x = 0; x < Chunk.CHUNK_SIZE; x++)
            {
                for (int y = 0; y < Chunk.CHUNK_SIZE; y++)
                {
                    for (int z = 0; z < Chunk.CHUNK_SIZE; z++)
                    {
                        var pos = new CubePosition(x, y, z, CubePosition.CoordinateSpace.ChunkSpace);
                        pos = pos.InCubeSpace(state.position);

                        ushort id = 0;
                        if (pos.Y < 256)
                            id = 1;

                        state.world.ChunkManager.CubeView.SetCube(pos, id);
                    }
                }
            }
        }

        public override void GenerateChunkDetail(WorldPrototype world, ChunkPosition position)
        {
        }
    }
}
