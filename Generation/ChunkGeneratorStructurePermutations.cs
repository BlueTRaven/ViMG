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
        private StructureGenerator.StructureGeneratorBatchCollection structureBatchesGOL3DOrangeShroomCaves;

        public ChunkGeneratorStructurePermutations(int layer, int seed = 1337) : base(layer, seed)
        {
        }

        public override void Initialize(World world)
        {
            base.Initialize(world);

            structureBatchesGOL3DOrangeShroomCaves = new StructureGeneratorGOL3DShrooms(Seed, null).Generate(64, 8);
        }

        public override void GenerateChunkBroad(ChunkManager.BroadGenerationState state)
        {
        }

        public override void GenerateChunkDetail(ChunkManager manager, Chunk chunk, ChunkPosition position)
        {
        }

        public override void PostGenerateDetail(ChunkManager manager)
        {
            base.PostGenerateDetail(manager);

            CubePosition testp = new CubePosition(2, 256, 256);

            for (int i = 0; i < 64; i++)
            {
                Structure s = structureBatchesGOL3DOrangeShroomCaves.Get(i);
                ChunkHelper.PlaceStructureWithBlacklist(manager.world, manager, manager.GetChunk(testp), s, testp, Span<ushort>.Empty, Span<ushort>.Empty);
                testp.X += s.size.X + 2;

                if (testp.X > manager.sizeInCubes)
                {
                    testp.X = 0;
                    testp.Z += 40;
                }
            }
        }

        public override Vector3 GetPlayerPosition(World world, ChunkManager chunks)
        {
            return new CubePosition(2, 256, 256).InWorldSpace(null);
        }
    }
}
