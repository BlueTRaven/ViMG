using BrUtility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViMG.ChunkStuff
{
    public class CopiedChunkPool  
    {
        //FastList has an expanding buffer that makes it ideal for pools
        private static FastList<CopiedChunkData> pooledCopies = new FastList<CopiedChunkData>();
        private static int lastUsedCopy;

        private static CopiedChunkData TakeFromPool(CubePosition basePosition)
        {
            CopiedChunkData copied = null;

            for (int i = 0; i < pooledCopies.Length; i++)
            {
                int ri = (lastUsedCopy + i) % pooledCopies.Length;

                if (pooledCopies.Buffer[ri] == null)
                    pooledCopies.Buffer[ri] = new CopiedChunkData(ri);

                if (!pooledCopies[ri].GetValid())
                {
                    copied = pooledCopies[ri];
                    lastUsedCopy = ri;

                    copied.Take(basePosition);

                    break;
                }
            }

            //just allocate one in case I guess
            if (copied == null || !copied.GetValid())
            {
                copied = new CopiedChunkData(pooledCopies.Length);
                copied.Take(basePosition);

                pooledCopies.Add(copied);
            }

            return copied;
        }

        public static CopiedChunkData MakeCopy(World world, ChunkPosition position)
        {
            CubePosition basePosition = position.InCubeSpace();

            CopiedChunkData copied = TakeFromPool(basePosition);

            Span<CubePosition> queryPositions = stackalloc CubePosition[CopiedChunkData.SIZE];

            for (int z = -1; z <= Chunk.CHUNK_SIZE; z++)
            {
                for (int y = -1; y <= Chunk.CHUNK_SIZE; y++)
                {
                    for (int x = -1; x <= Chunk.CHUNK_SIZE; x++)
                    {
                        Util.ThreeDToOneD(new ValuePoint3D(x + 1, y + 1, z + 1), new ValuePoint3D(CopiedChunkData.WHD), out int i);
                        CubePosition pos = basePosition + new CubePosition(x, y, z);

                        if (pos.X < 0)
                            pos.X = 0;
                        if (pos.Y < 0)
                            pos.Y = 0;
                        if (pos.Z < 0)
                            pos.Z = 0;

                        if (pos.X >= world.sizeInCubes)
                            pos.X = world.sizeInCubes - 1;
                        if (pos.Y >= world.sizeInCubes)
                            pos.Y = world.sizeInCubes - 1;
                        if (pos.Z >= world.sizeInCubes)
                            pos.Z = world.sizeInCubes - 1;

                        queryPositions[i] = pos;
                    }
                }
            }

            world.ChunkManager.InitializerView.GetIds(queryPositions, copied.Ids);
            world.EntityManager.GetEntityMeshingDatas(queryPositions, copied.EntityMeshingDatas);

            return copied;
        }
    }
}
