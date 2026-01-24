using BrUtility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG;
using ViMG.Generation;

namespace ModGameBase.Generation
{
    public class StructureGeneratorList
    {
        public struct StructureGeneration
        {
            public Structure structure;
            public CubePosition generatePos;

            // Do not overwrite cubes already present in the world that have this id
            public ushort[] overwriteWorldBlacklist;
            // Do not write cubes in the structure that have this id
            public ushort[] dontwriteStructureBlacklist;
        }

        private struct ChunkStructure
        {
            public int structureIndex;
            public Rectangle3DI boundsInThisChunk;
        }

        private struct ChunkStructures
        {
            public List<ChunkStructure> structuresInThisChunk;
        }

        public FastList<StructureGeneration> structuresToGen;

        public void GenerateStructures(ChunkManager manager)
        {
            ChunkStructures[] structuresInChunks = new ChunkStructures[32 * 32 * 32];
            //Dictionary<ChunkPosition, ChunkStructures> structuresInChunks = new();

            for (int i = 0; i < structuresToGen.Length; i++)
            {
                ref readonly StructureGeneration structureToGen = ref structuresToGen.Buffer[i];

                CubePosition sizeInCubes = new CubePosition(structureToGen.structure.size.X, structureToGen.structure.size.Y, structureToGen.structure.size.Z, CubePosition.CoordinateSpace.CubeSpace);
                ChunkPosition min = ChunkPosition.CubeChunk(structureToGen.generatePos);
                ChunkPosition max = ChunkPosition.CubeChunk(structureToGen.generatePos + sizeInCubes);

                for (int z = min.Z; z < max.Z; z++)
                {
                    for (int y = min.Y; y < max.Y; y++)
                    {
                        for (int x = min.X; x < max.X; x++)
                        {
                            Util.ThreeDToOneD(new ValuePoint3D(x, y, z), new ValuePoint3D(32), out int j);
                            if (structuresInChunks[j].structuresInThisChunk == null)
                                structuresInChunks[j].structuresInThisChunk = new();

                            ChunkPosition chunkPos = new ChunkPosition(x, y, z);
                            CubePosition chunkPosInCubeSpace = chunkPos.InCubeSpace();
                            CubePosition nextChunkPosInCubeSpace = new ChunkPosition(x + 1, y + 1, z + 1).InCubeSpace();
                            CubePosition posInChunk = structureToGen.generatePos - chunkPosInCubeSpace;
                            CubePosition sizeInChunk = nextChunkPosInCubeSpace - structureToGen.generatePos;


                            structuresInChunks[j].structuresInThisChunk.Add(new ChunkStructure
                            {
                                structureIndex = i,
                                boundsInThisChunk = new Rectangle3DI(new Point3D(posInChunk.X, posInChunk.Y, posInChunk.Z),
                                    new Point3D(sizeInChunk.X, sizeInChunk.Y, sizeInChunk.Z)),
                            });

                            //structures.structuresInThisChunk.Add(new ChunkStructure
                            //{
                            //    structureIndex = i,
                            //    boundsInThisChunk = 
                            //});
                        }
                    }
                }
            }

            Span<ushort> ids = stackalloc ushort[Chunk.NUM_CUBES_IN_CHUNK];
            Span<CubePosition> positions = stackalloc CubePosition[Chunk.NUM_CUBES_IN_CHUNK];
            int idsI = 0;

            for (int i = 0; i < structuresInChunks.Length; i++)
            {
                idsI = 0;

                var chunk = structuresInChunks[i];
                if (chunk.structuresInThisChunk == null) continue;

                Util.OneDToThreeD(i, new ValuePoint3D(32), out var pt);
                ChunkPosition chunkPos = new ChunkPosition(pt.x, pt.y, pt.z);
                CubePosition chunkPosInCubeSpace = chunkPos.InCubeSpace();

                for (int cx = 0; cx < Chunk.CHUNK_SIZE; cx++)
                {
                    for (int cy = 0; cy < Chunk.CHUNK_SIZE; cy++)
                    {
                        for (int cz = 0; cz < Chunk.CHUNK_SIZE; cz++)
                        {
                            for (int j = 0; j < chunk.structuresInThisChunk.Count; j++)
                            {
                                var structureInfo = chunk.structuresInThisChunk[j];
                                if (structureInfo.boundsInThisChunk.Contains(new Point3D(cx, cy, cz)))
                                {
                                    var structure = structuresToGen[structureInfo.structureIndex];
                                    CubePosition positionInCS = new CubePosition(cx, cy, cz) + chunkPosInCubeSpace;
                                    CubePosition positionInStructure = positionInCS - structure.generatePos;
                                    Util.ThreeDToOneD(new ValuePoint3D(positionInStructure), new ValuePoint3D(structure.structure.size), out int indexInStructureData);

                                    bool canWrite = true;
                                    //Allow structure cube to be overwritten (rather, not written) by world.
                                    if (structure.dontwriteStructureBlacklist.Length != 0)
                                    {
                                        for (int blacklistI = 0; blacklistI < structure.dontwriteStructureBlacklist.Length; blacklistI++)
                                        {
                                            if (structure.structure.data[indexInStructureData] == structure.dontwriteStructureBlacklist[blacklistI])
                                                canWrite = false;
                                        }
                                    }

                                    //Allow world cube to be overwritten by structure
                                    if (structure.overwriteWorldBlacklist.Length != 0)
                                    {
                                        int overwritingId = manager.CubeView.GetCube(positionInCS).GetOrDefault(Main.Registry.CubeRegistry.Air).Id;

                                        for (int blacklistI = 0; blacklistI < structure.overwriteWorldBlacklist.Length; blacklistI++)
                                        {
                                            if (structure.overwriteWorldBlacklist[blacklistI] == overwritingId)
                                                canWrite = false;
                                        }
                                    }

                                    if (canWrite)
                                    {
                                        positions[idsI] = positionInCS;
                                        ids[idsI] = structuresToGen[structureInfo.structureIndex].structure.data[indexInStructureData];
                                        idsI += 1;
                                    }
                                }
                            }
                        }
                    }
                }

                manager.CubeView.SetCubes(positions[0..idsI], ids[0..idsI]);
            }

            //List<List<int>> buckets = new();

            //var s = structuresToGen[0];
            //var mn = s.generatePos;
            //var mx = s.generatePos + new CubePosition(s.structure.size.X, s.structure.size.Y, s.structure.size.Z);

            //var mnc = ChunkPosition.CubeChunk(mn);
            //var mxc = ChunkPosition.CubeChunk(mx) + new ChunkPosition(1, 1, 1);

            //for (int x = mnc.X; x < mxc.X; x++)
            //{
            //    for (int y = mnc.Y; y < mxc.Y; y++)
            //    {
            //        for (int z = mnc.Z; z < mxc.Z; z++)
            //        {
            //            ChunkPosition chunkPos = new ChunkPosition(x, y, z);
            //            CubePosition chunkPosInCubeSpace = chunkPos.InCubeSpace();
            //            CubePosition nextChunkPosInCubeSpace = new ChunkPosition(x + 1, y + 1, z + 1).InCubeSpace();
            //            CubePosition posInChunk = s.generatePos - chunkPosInCubeSpace;
            //            CubePosition sizeInChunk = nextChunkPosInCubeSpace - mn;

            //            Rectangle3DI boundsInChunk = new Rectangle3DI(new Point3D(posInChunk.X, posInChunk.Y, posInChunk.Z), 
            //                new Point3D(sizeInChunk.X, sizeInChunk.Y, sizeInChunk.Z));

            //            for (int cx = 0; cx < Chunk.CHUNK_SIZE; cx++)
            //            {
            //                for (int cy = 0; cy < Chunk.CHUNK_SIZE; cy++)
            //                {
            //                    for (int cz = 0; cz < Chunk.CHUNK_SIZE; cz++)
            //                    {
            //                        if (boundsInChunk.Contains(new Point3D(cx, cy, cz)))
            //                        {
            //                            CubePosition positionInCS = new CubePosition(cx, cy, cz) + chunkPosInCubeSpace;
            //                            CubePosition positionInStructure = positionInCS - s.generatePos;
            //                        }
            //                    }
            //                }
            //            }
            //        }
            //    }
            //}
        }
    }
}
