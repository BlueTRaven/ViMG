using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViMG.Generation
{
    public class ChunkGeneratorStructurePermutations : ChunkGenerator
    {
        private StructureGenerator.StructureGeneratorBatchCollection batches;

        private StructureGenerator generator;

        public ChunkGeneratorStructurePermutations(int layer, StructureGenerator generator, int seed = 1337) : base(layer, seed)
        {
            this.generator = generator;
        }

        public override void Initialize(int sizeInCubesXZ, int sizeInChunksY)
        {
            base.Initialize(sizeInCubesXZ, sizeInChunksY);

            batches = generator.Generate(64, 8);
            //a = new StructureGeneratorGOL3DShrooms(Seed, null).Generate(64, 8);
        }

        public override void GenerateChunkBroad(ChunkGeneratorTasker.BroadGenerationState state)
        {
        }

        public override void GenerateChunkDetail(ChunkManager2 manager, ChunkPosition position)
        {
        }

        public override void PostGenerateDetail(World world, ChunkManager2 manager)
        {
            base.PostGenerateDetail(world, manager);

            CubePosition startPosition = new CubePosition(2, 256, 256);

            for (int i = 0; i < 64; i++)
            {
                Structure s = batches.Get(i);
                ChunkHelper.PlaceStructureWithBlacklist(manager, s, startPosition, Span<ushort>.Empty, Span<ushort>.Empty, true);
                startPosition.X += s.size.X + 2;

                if (startPosition.X > manager.SizeInCubes)
                {
                    startPosition.X = 0;
                    startPosition.Z += batches.LockAndGetMax().z;
                }
            }
        }

        public override Vector3 GetPlayerPosition(World world, ChunkManager2 chunks)
        {
            return new CubePosition(2, 256, 256).InWorldSpace();
        }
    }
}
