using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Cubes;

namespace ViMG
{
    public class CubeQuery
    {
        private readonly Rectangle3DI queryRange;
        private readonly Chunk[] chunksToSearch;
        private readonly Cube[] search;

        private int currentSearchIndex;
        private List<CubePosition> queryReturnValue;

        public CubeQuery(World world, Rectangle3DI queryRange, Cube[] search)
        {
            this.queryRange = queryRange;
            this.search = search;

            queryReturnValue = new List<CubePosition>();

            ChunkPosition near = ChunkPosition.CubeChunk(new CubePosition(queryRange.Position.X, queryRange.Position.Y, queryRange.Position.Z));
            ChunkPosition far = ChunkPosition.CubeChunk(new CubePosition(queryRange.FarPosition.X, queryRange.FarPosition.Y, queryRange.FarPosition.Z));

            chunksToSearch = new Chunk[(far.X - near.X) * (far.Y - near.Y) * (far.Z - near.Z)];

            int i = 0;
            /*for (int x = near.X; x < far.X - near.X; x++)
            {
                for (int y = near.Y; y < far.Y - near.Y; y++)
                {
                    for (int z = near.Z; z < far.Z - near.Z; z++)
                    {
                        chunksToSearch[i++] = world.ChunkManager.GetChunk(new ChunkPosition(x, y, z));
                    }
                }
            }*/
        }

        public void Update()
        {
            if (currentSearchIndex < chunksToSearch.Length)
            {
                if (chunksToSearch[currentSearchIndex] != null && chunksToSearch[currentSearchIndex].Initialized)
                    LoopChunk(chunksToSearch[currentSearchIndex++]);
            }
        }

        public bool IsFinished()
        {
            return currentSearchIndex >= chunksToSearch.Length;
        }

        public IReadOnlyList<CubePosition> Get()
        {
            return queryReturnValue;
        }

        private void LoopChunk(Chunk chunk)
        {
            /*ushort[] data = chunk.GetData().GetAll();

            for (int x = 0; x < Chunk.CHUNK_SIZE; x++)
            {
                for (int y = 0; y < Chunk.CHUNK_SIZE; y++)
                {
                    for (int z = 0; z < Chunk.CHUNK_SIZE; z++)
                    {
                        Util.ThreeDToOneD(new ValuePoint3D(x, y, z), new ValuePoint3D(Chunk.CHUNK_SIZE, Chunk.CHUNK_SIZE, Chunk.CHUNK_SIZE), out int i);

                        CubePosition posInCubeSpace = new CubePosition(x, y, z, CubePosition.CoordinateSpace.ChunkSpace).InCubeSpace(chunk);

                        if (IsInBounds(posInCubeSpace) && IsQueriedCube(data[i]))
                        {
                            queryReturnValue.Add(posInCubeSpace);
                        }
                    }
                }
            }*/
        }

        private bool IsQueriedCube(ushort id)
        {
            for (int i = 0; i < search.Length; i++)
            {
                if (search[i].Id == id)
                    return true;
            }

            return false;
        }

        private bool IsInBounds(CubePosition pos)
        {
            return pos.X >= queryRange.Position.X && pos.X <= queryRange.FarPosition.X &&
                pos.Y >= queryRange.Position.Y && pos.Y <= queryRange.FarPosition.Y &&
                pos.Z >= queryRange.Position.Z && pos.Y <= queryRange.FarPosition.Z;
        }
    }
}
