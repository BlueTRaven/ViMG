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
    public class ChunkGeneratorCatacombs : ChunkGenerator
    {
        private Cube stone;

        public ChunkGeneratorCatacombs(int layer, int seed = 1337) : base(layer, seed)
        {
        }

        public override void Initialize(int sizeInCubesXZ, int sizeInChunksY)
        {
            base.Initialize(sizeInCubesXZ, sizeInChunksY);

            stone = GlobalState.Registry.CubeRegistry.Get("stone_crypt");
        }

        public override void GenerateChunkBroad(ChunkGeneratorTasker.BroadGenerationState state)
        {
            const int INTERVALXZ = 85;
            const int INTERVALY = 128;
            const float SIZE = 12;

            float ease(float x)
            {
                return MathF.Pow(x, 4);
            }

            for (int x = 0; x < Chunk.CHUNK_SIZE; x++)
            {
                for (int y = 0; y < Chunk.CHUNK_SIZE; y++)
                {
                    for (int z = 0; z < Chunk.CHUNK_SIZE; z++)
                    {
                        CubePosition chunkSpacePos = new CubePosition(x, y, z, CubePosition.CoordinateSpace.ChunkSpace);
                        CubePosition cubeSpacePos = chunkSpacePos.InCubeSpace(state.position);

                        CubePosition xzHoleSpacePos = new CubePosition(cubeSpacePos.X % INTERVALXZ, cubeSpacePos.Y % INTERVALXZ, cubeSpacePos.Z % INTERVALXZ);
                        CubePosition xFacingHole = new CubePosition(0, INTERVALXZ / 2, INTERVALXZ / 2);
                        CubePosition zFacingHole = new CubePosition(INTERVALXZ / 2, INTERVALXZ / 2, 0);

                        Vector2 xDistance = new Vector2(xzHoleSpacePos.Z - xFacingHole.Z, xzHoleSpacePos.Y - xFacingHole.Y);
                        Vector2 zDistance = new Vector2(xzHoleSpacePos.X - zFacingHole.X, xzHoleSpacePos.Y - zFacingHole.Y);

                        CubePosition yHoleSpacePos = new CubePosition(cubeSpacePos.X % INTERVALY, cubeSpacePos.Y % INTERVALY, cubeSpacePos.Z % INTERVALY);
                        CubePosition yFacingHole = new CubePosition(INTERVALY / 2, 0, INTERVALY / 2);

                        Vector2 yDistance = new Vector2(yHoleSpacePos.X - yFacingHole.X, yHoleSpacePos.Z - yFacingHole.Z);

                        float simplex = (noise.GetNoise(cubeSpacePos.X, cubeSpacePos.Y, cubeSpacePos.Z) + 1f) / 2f;
                        float attenuationX = ease(xDistance.Length() / SIZE);
                        float attenuationY = ease(yDistance.Length() / SIZE);
                        float attenuationZ = ease(zDistance.Length() / SIZE);

                        if ((simplex * attenuationX < 0.25f && xDistance.Length() < SIZE) || 
                            (simplex * attenuationZ < 0.25f && zDistance.Length() < SIZE) || 
                            (simplex * attenuationY < 0.25f && yDistance.Length() < SIZE)) 
                            state.world.ChunkManager.CubeView.SetCube(cubeSpacePos, 0, false);
                        else state.world.ChunkManager.CubeView.SetCube(cubeSpacePos, stone.Id, false);
                    }
                }
            }
        }

        public override void GenerateChunkDetail(WorldPrototype world, ChunkPosition position)
        {
        }

        public override Vector3 GetPlayerPosition(ChunkManager chunkManager)
        {
            return new Vector3(256 * Cube.CUBE_SCALE, 511 * Cube.CUBE_SCALE, 256 * Cube.CUBE_SCALE);
        }
    }
}
