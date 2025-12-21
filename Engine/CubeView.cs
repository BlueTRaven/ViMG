using LiteNetLib.Utils;
using SharpDX.MediaFoundation;
using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using ViMG.Cubes;
using ViMG.IMGUIImpl;
using static Engine.Networking.Messages.SyncChunk;

namespace ViMG
{
    //A cube view intended for initialization of the world.
    //This view does not support any multithreading.
    //Therefore it should only be used on contexts where multithreading may not be running or where threads cannot overlap.
    public class CubeView
    {
        private readonly ChunkManager chunkManager;
        private readonly ChunkManagerIO io;

        public CubeView(ChunkManager chunkManager, ChunkManagerIO io)
        {
            this.chunkManager = chunkManager;
            this.io = io;
        }

        public ushort GetId(CubePosition position)
        {
            ReadOnlySpan<ushort> ids = io.GetChunk(ChunkPosition.CubeChunk(position), ChunkManagerIO.GetMode.Read);

            var posInChunkSpace = position.InChunkSpace();
            Util.ThreeDToOneD(new ValuePoint3D(posInChunkSpace), new ValuePoint3D(Chunk.CHUNK_SIZE), out int i);
            var id = ids[i];
            io.ReleaseChunk(ChunkPosition.CubeChunk(position), ChunkManagerIO.GetMode.Read);
            return id;

            //byte[] bytes = io.GetBytes();

            //int cubeOffset = ChunkManagerIO.GetCubeOffset(position);

            //ushort id = Unsafe.ReadUnaligned<ushort>(ref bytes[cubeOffset * sizeof(ushort)]);

            //BitConverter is apparently faster than fixed cast of bytes to ushort
            //return id;
        }

        private struct SortedCubePos
        {
            public int originalIndex;
            public CubePosition cubePos;
        }
        private struct Comparer : IComparer<SortedCubePos>
        {
            public int Compare(SortedCubePos a, SortedCubePos b)
            {
                var chunkPosA = ChunkPosition.CubeChunk(a.cubePos);
                var chunkPosB = ChunkPosition.CubeChunk(b.cubePos);
                if (chunkPosA == chunkPosB) return 0;
                if (chunkPosA.X > chunkPosB.X) return -1;
                return 1;
            }
        }

        private enum GetIdsMethod
        {
            Sorted,
            Unsorted,
            Dict
        };
        private const GetIdsMethod getIdsMethod = GetIdsMethod.Dict;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void GetIds(Span<CubePosition> positions, Span<ushort> ids)
        {
            //GetIdsSorted(positions, ids);
            GetIdsUnsorted(positions, ids);
            //GetIdsDict(positions, ids);
        }

        private unsafe void GetIdsSorted(Span<CubePosition> positions, Span<ushort> ids)
        {
            using var zone = TracyImpl.Tracy.BeginZone(name: "GetIdsSorted");

            Span<SortedCubePos> sortedPositions = stackalloc SortedCubePos[positions.Length];
            using (var zsort = TracyImpl.Tracy.BeginZone(name: "GetIdsSorted: Sort"))
            {
                for (int i = 0; i < positions.Length; i++)
                {
                    sortedPositions[i] = new SortedCubePos
                    {
                        cubePos = positions[i],
                        originalIndex = i,
                    };
                }

                // Positions is sorted before hand to make things more optimal
                // Need to keep track of original index as sometimes that's important, so that's why we copy
                MemoryExtensions.Sort(sortedPositions, new Comparer());
            }

            ChunkPosition cachedChunkPos = new ChunkPosition(-1, -1, -1);
            ReadOnlySpan<ushort> cachedChunkData = null;

            using (var zsort = TracyImpl.Tracy.BeginZone(name: "GetIdsSorted: Get"))
            {
                for (int i = 0; i < positions.Length; i++)
                {
                    CubePosition pos = sortedPositions[i].cubePos;
                    ChunkPosition chunkPos = ChunkPosition.CubeChunk(pos);
                    if (cachedChunkData == null || chunkPos != cachedChunkPos)
                    {
                        if (cachedChunkData != null)
                            io.ReleaseChunk(cachedChunkPos, ChunkManagerIO.GetMode.Read);

                        cachedChunkPos = chunkPos;
                        cachedChunkData = io.GetChunk(chunkPos, ChunkManagerIO.GetMode.Read);
                    }

                    Util.ThreeDToOneD(new ValuePoint3D(pos.InChunkSpace()), new ValuePoint3D(Chunk.CHUNK_SIZE), out int j);
                    ids[sortedPositions[i].originalIndex] = cachedChunkData[j];
                }

                if (cachedChunkData != null)
                    io.ReleaseChunk(cachedChunkPos, ChunkManagerIO.GetMode.Read);
            }
        }

        private unsafe void GetIdsUnsorted(Span<CubePosition> positions, Span<ushort> ids)
        {
            using var zone = TracyImpl.Tracy.BeginZone(name: "GetIdsUnsorted");

            ChunkPosition cachedChunkPos = new ChunkPosition(-1, -1, -1);
            ReadOnlySpan<ushort> cachedChunkData = null;

            for (int i = 0; i < positions.Length; i++)
            {
                CubePosition pos = positions[i];
                ChunkPosition chunkPos = ChunkPosition.CubeChunk(pos);
                if (cachedChunkData == null || chunkPos != cachedChunkPos)
                {
                    if (cachedChunkData != null)
                        io.ReleaseChunk(cachedChunkPos, ChunkManagerIO.GetMode.Read);

                    cachedChunkPos = chunkPos;
                    cachedChunkData = io.GetChunk(chunkPos, ChunkManagerIO.GetMode.Read);
                }

                Util.ThreeDToOneD(new ValuePoint3D(pos.InChunkSpace()), new ValuePoint3D(Chunk.CHUNK_SIZE), out int j);
                ids[i] = cachedChunkData[j];
            }

            if (cachedChunkData != null)
                io.ReleaseChunk(cachedChunkPos, ChunkManagerIO.GetMode.Read);
        }

        public unsafe void GetIdsForChunk(ChunkPosition chunkPosition, Span<ushort> queryIds)
        {
            IMGUIConsole.Assert(queryIds.Length == Chunk.NUM_CUBES_IN_CHUNK);

            CubePosition basePosition = chunkPosition.InCubeSpace();

            Span<CubePosition> queryPositions = stackalloc CubePosition[Chunk.NUM_CUBES_IN_CHUNK];
            fixed (CubePosition* queryPositionsPtr = queryPositions)
            {
                for (int z = 0; z < Chunk.CHUNK_SIZE; z++)
                {
                    for (int y = 0; y < Chunk.CHUNK_SIZE; y++)
                    {
                        for (int x = 0; x < Chunk.CHUNK_SIZE; x++)
                        {
                            Util.ThreeDToOneD(new ValuePoint3D(x, y, z), new ValuePoint3D(Chunk.CHUNK_SIZE), out int i);
                            CubePosition pos = basePosition + new CubePosition(x, y, z);
                            queryPositionsPtr[i] = pos;
                        }
                    }
                }
            }

            GetIds(queryPositions, queryIds);
        }

        public Optional<Cube> GetCube(CubePosition position)
        {
            if (!chunkManager.IsInWorldBounds(position))
                return new Optional<Cube>();

            ushort id = GetId(position);

            return new Optional<Cube>(Main.Registry.CubeRegistry.Get(id));
        }

        public void GetCubes(Span<CubePosition> positions, Span<Cube> cubes, Cube def)
        {
            Span<ushort> ids = stackalloc ushort[positions.Length];
            GetIds(positions, ids);

            var registry = Main.Registry.CubeRegistry.GetIterable();

            for (int i = 0; i < positions.Length; i++) 
            {
                if (ids[i] - 1 < 0) cubes[i] = def;
                else cubes[i] = registry[ids[i] - 1];
            }
        }

        private static CubePosition[] adjacentOffsets = new CubePosition[6]
        {
            new CubePosition(-1, 0, 0),
            new CubePosition(1, 0, 0),
            new CubePosition(0, -1, 0),
            new CubePosition(0, 1, 0),
            new CubePosition(0, 0, -1),
            new CubePosition(0, 0, 1)
        };

        private static MeshHelper.CubeFace[] adjacentFaces = new MeshHelper.CubeFace[6]
        {
            MeshHelper.CubeFace.RIGHT,
            MeshHelper.CubeFace.LEFT,
            MeshHelper.CubeFace.DOWN,
            MeshHelper.CubeFace.UP,
            MeshHelper.CubeFace.FRONT,
            MeshHelper.CubeFace.BACK
        };

        public void SetCube(CubePosition position, ushort id, bool markDirty = true)
        {
            Span<ushort> ids = io.GetChunk(ChunkPosition.CubeChunk(position), ChunkManagerIO.GetMode.Write);

            var posInChunkSpace = position.InChunkSpace();
            Util.ThreeDToOneD(new ValuePoint3D(posInChunkSpace), new ValuePoint3D(Chunk.CHUNK_SIZE), out int i);

            ushort oldId = ids[i];
            ids[i] = id;

            io.ReleaseChunk(ChunkPosition.CubeChunk(position), ChunkManagerIO.GetMode.Write);

            if (markDirty)
            {
                chunkManager.MarkCubeMeshInfoDirty(null, position, oldId, id);
                chunkManager.ChunkMesher?.MarkChunkDirty(ChunkPosition.CubeChunk(position));
            }
        }

        public void SetCube(CubePosition position, ushort id, Player player)
        {
            Span<ushort> cubes = io.GetChunk(ChunkPosition.CubeChunk(position), ChunkManagerIO.GetMode.Write);

            var posInChunkSpace = position.InChunkSpace();
            Util.ThreeDToOneD(new ValuePoint3D(posInChunkSpace), new ValuePoint3D(Chunk.CHUNK_SIZE), out int i);

            ushort oldId = cubes[i];
            cubes[i] = id;

            io.ReleaseChunk(ChunkPosition.CubeChunk(position), ChunkManagerIO.GetMode.Write);

            chunkManager.MarkCubeMeshInfoDirty(player, position, oldId, id);
            chunkManager.ChunkMesher?.MarkChunkDirty(ChunkPosition.CubeChunk(position));
        }

        public void SetCubes(Span<CubePosition> positions, Span<ushort> ids)
        {
            ChunkPosition cachedChunkPos = new ChunkPosition(-1, -1, -1);
            Span<ushort> cachedChunkBytes = null;

            for (int i = 0; i < positions.Length; i++)
            {
                CubePosition pos = positions[i];
                ChunkPosition chunkPos = ChunkPosition.CubeChunk(pos);
                if (cachedChunkBytes == null || chunkPos != cachedChunkPos)
                {
                    if (cachedChunkBytes != null)
                        io.ReleaseChunk(cachedChunkPos, ChunkManagerIO.GetMode.Write);

                    cachedChunkPos = chunkPos;
                    cachedChunkBytes = io.GetChunk(chunkPos, ChunkManagerIO.GetMode.Write);
                }

                Util.ThreeDToOneD(new ValuePoint3D(pos.InChunkSpace()), new ValuePoint3D(Chunk.CHUNK_SIZE), out int j);
                cachedChunkBytes[j] = ids[i];
            }

            if (cachedChunkBytes != null)
                io.ReleaseChunk(cachedChunkPos, ChunkManagerIO.GetMode.Write);
        }

        public void SetCubes(Span<CubePosition> positions, ushort id)
        {
            ChunkPosition cachedChunkPos = new ChunkPosition(-1, -1, -1);
            Span<ushort> cachedChunkBytes = null;

            for (int i = 0; i < positions.Length; i++)
            {
                CubePosition pos = positions[i];
                ChunkPosition chunkPos = ChunkPosition.CubeChunk(pos);
                if (cachedChunkBytes == null || chunkPos != cachedChunkPos)
                {
                    if (cachedChunkBytes != null)
                        io.ReleaseChunk(cachedChunkPos, ChunkManagerIO.GetMode.Write);

                    cachedChunkPos = chunkPos;
                    cachedChunkBytes = io.GetChunk(chunkPos, ChunkManagerIO.GetMode.Write);
                }

                Util.ThreeDToOneD(new ValuePoint3D(pos.InChunkSpace()), new ValuePoint3D(Chunk.CHUNK_SIZE), out int j);
                cachedChunkBytes[j] = id;
            }

            if (cachedChunkBytes != null)
                io.ReleaseChunk(cachedChunkPos, ChunkManagerIO.GetMode.Write);
        }

        public OptionalValue<CubePosition> GetFirstSolidDown(CubePosition start)
        {
            for (int y = 0; y < chunkManager.SizeInCubes; y++)
            {
                CubePosition pos = new CubePosition(start.X, start.Y - y, start.Z);

                //Null check here is the same as doing out of bounds check.
                Cube cubeAtPos = GetCube(pos).Get();
                if (cubeAtPos != null && cubeAtPos.Solid)
                    return new OptionalValue<CubePosition>(pos);
            }

            return new OptionalValue<CubePosition>();
        }

        public enum PalettizeType
        {
            AllOneId,
            _1bit,
            _2bit,
            _4bit,
            _8bit,
            _16bit,
        }

        public static int ExpectedPaletteMax(PalettizeType t)
        {
            return t switch
            {
                PalettizeType.AllOneId => 1,
                PalettizeType._1bit => 2,
                PalettizeType._2bit => 4,
                PalettizeType._4bit => 16,
                PalettizeType._8bit => 256,
                PalettizeType._16bit => throw new NotImplementedException(),
            };
        }

        public static int ExpectedLen(PalettizeType t)
        {
            return t switch
            {
                PalettizeType.AllOneId => 3,
                PalettizeType._1bit => 512,
                PalettizeType._2bit => 1024,
                PalettizeType._4bit => 2048,
                PalettizeType._8bit => 4096,
                _ => 0,
            };
        }

        public struct PalettizedChunk : INetSerializable
        {
            public ChunkPosition position;
            public PalettizeType type;
            public ushort[] palette;
            public byte[]? data;

            public void Save(Stream stream)
            {
                List<byte> bytes = new List<byte>();
                SaveHelper.SaveInt32(bytes, (int)type);
                SaveHelper.SaveInt32(bytes, palette.Length);
                for (int i = 0; i < palette.Length; i++)
                    SaveHelper.SaveUInt16(bytes, palette[i]);

                if (type != PalettizeType.AllOneId)
                {
                    SaveHelper.SaveInt32(bytes, data.Length);
                    SaveHelper.SaveBytesFlat(bytes, data);
                }

                stream.Write(bytes.ToArray());
            }

            public void Load(Span<byte> bytes)
            {
                int offset = 0;
                type = (PalettizeType)SaveHelper.LoadInt32(bytes, ref offset);
                Debug.Assert(type <= PalettizeType._8bit && type >= 0);
                int palLen = SaveHelper.LoadInt32(bytes, ref offset);
                Debug.Assert(palLen <= ExpectedPaletteMax(type));
                palette = new ushort[palLen];
                for (int i = 0; i < palLen; i++)
                    palette[i] = SaveHelper.LoadUInt16(bytes, ref offset);

                if (type != PalettizeType.AllOneId)
                {
                    int dataLen = SaveHelper.LoadInt32(bytes, ref offset);
                    Debug.Assert(dataLen == ExpectedLen(type));
                    data = SaveHelper.LoadBytes(bytes, dataLen, ref offset).ToArray();
                }
            }

            public void Deserialize(NetDataReader reader)
            {
            }

            public void Serialize(NetDataWriter writer)
            {
                writer.Put((int)type);
                writer.PutArray(palette);
                if (type != CubeView.PalettizeType.AllOneId)
                    writer.PutBytesWithLength(data, 0, (ushort)data.Length);
            }
        }

        public static PalettizedChunk Palettize(ChunkPosition chunkPosition, Span<ushort> ids)
        {
            using var zone = TracyImpl.Tracy.BeginZone();

            var chunk = new PalettizedChunk
            {
                type = PalettizeType.AllOneId,
                position = chunkPosition,
            };

            IMGUIConsole.Assert(ids.Length == Chunk.NUM_CUBES_IN_CHUNK);

            //Span<ushort> ids = stackalloc ushort[Chunk.NUM_CUBES_IN_CHUNK];
            //GetIdsForChunk(chunkPosition, ids);
            Span<int> usedIds = stackalloc int[Main.Registry.CubeRegistry.Count];
            for (int i = 0; i < usedIds.Length; i++)
                usedIds[i] = -1;
            Span<ushort> uniqueIds = stackalloc ushort[Chunk.NUM_CUBES_IN_CHUNK];
            Span<ushort> palettizedIds = stackalloc ushort[Chunk.NUM_CUBES_IN_CHUNK];

            ushort uniqueIdsLen = 0;

            for (int i = 0; i < ids.Length; i++)
            {
                ushort id = ids[i];
                if (usedIds[id] < 0)
                {
                    usedIds[id] = uniqueIdsLen;
                    uniqueIds[uniqueIdsLen] = id;
                    uniqueIdsLen += 1;
                }
                IMGUIConsole.Assert(usedIds[id] >= 0);
                palettizedIds[i] = (ushort)usedIds[id];
            }

            chunk.palette = uniqueIds[0..uniqueIdsLen].ToArray();
            if (uniqueIdsLen == 1)
            {
                return chunk;
            }
            else
            {
                int bitmask = 0;
                int bits = 0;
                List<byte> bytes = new List<byte>();
                if (uniqueIdsLen <= 2)
                {
                    bitmask = 0b1;
                    bits = 1;
                    chunk.type = PalettizeType._1bit;
                }
                else if (uniqueIdsLen <= 4)
                {
                    bitmask = 0b11;
                    bits = 2;
                    chunk.type = PalettizeType._2bit;

                }
                else if (uniqueIdsLen <= 16)
                {
                    bitmask = 0b1111;
                    bits = 4;
                    chunk.type = PalettizeType._4bit;
                }
                else if (uniqueIdsLen <= 256)
                {
                    bitmask = 255;
                    bits = 8;
                    chunk.type = PalettizeType._8bit;
                }
                else
                {
                    IMGUIConsole.Assert(false, "Unimplemented");
                }

                int numfit = sizeof(byte) * 8 / bits;
                byte currentByte = 0;
                int placeInByte = 0;

                for (int i = 0; i < ids.Length; i++)
                {
                    IMGUIConsole.Assert(placeInByte < 8);
                    IMGUIConsole.Assert(palettizedIds[i] <= bitmask);
                    currentByte |= (byte)((palettizedIds[i] & bitmask) << placeInByte);

                    placeInByte += bits;
                    if (placeInByte >= 8)
                    {
                        placeInByte = 0;
                        bytes.Add(currentByte);
                        currentByte = 0;
                    }
                }

                chunk.data = bytes.ToArray();
            }

            //Console.WriteLine("Chunk {0} Was: {1}", chunk.position, chunk.type.ToString());
            //Console.WriteLine("Size: {0} in palette, {1} bytes", chunk.palette.Length, chunk.data.Length);
            //Console.WriteLine("Palette: ");
            //foreach (ushort id in chunk.palette)
            //{
            //    Console.WriteLine(Main.Registry.CubeRegistry.GetOrDefault(id, Main.Registry.CubeRegistry.Air));
            //}

            return chunk;
        }

        public static ushort[] Depaletteize(PalettizedChunk chunk)
        {
            using var zone = TracyImpl.Tracy.BeginZone();

            var ids = new ushort[Chunk.NUM_CUBES_IN_CHUNK];
            if (chunk.type == PalettizeType.AllOneId)
            {
                Array.Fill(ids, chunk.palette[0]);
            } 
            else
            {
                byte bitmask = 0;
                int bits = 0;
                if (chunk.type == PalettizeType._1bit)
                {
                    bitmask = 0b1;
                    bits = 1;
                }
                else if (chunk.type == PalettizeType._2bit)
                {
                    bitmask = 0b11;
                    bits = 2;
                }
                else if (chunk.type == PalettizeType._4bit)
                {
                    bitmask = 0b1111;
                    bits = 4;
                }
                else if (chunk.type == PalettizeType._8bit)
                {
                    bitmask = 255;
                    bits = 8;
                }
                else
                {
                    IMGUIConsole.Assert(false, "Unimplemented");
                }

                int numfit = sizeof(byte) * 8 / bits;
                int currentByteIndex = 0;
                byte currentByte = chunk.data[currentByteIndex];
                currentByteIndex += 1;
                int placeInByte = 0;

                for (int i = 0; i < ids.Length; i++)
                {
                    IMGUIConsole.Assert((currentByteIndex - 1) == i / numfit);
                    IMGUIConsole.Assert(placeInByte < 8);

                    ushort paletteId = (ushort)((currentByte >> placeInByte) & bitmask);
                    IMGUIConsole.Assert(paletteId <= bitmask);

                    ids[i] = chunk.palette[paletteId];
                    placeInByte += bits;
                    if (placeInByte >= 8 && i != ids.Length - 1)
                    {
                        placeInByte = 0;
                        currentByte = chunk.data[currentByteIndex];
                        currentByteIndex += 1;
                    }
                }
            }

            return ids;
        }

        public void TestPalettize(ChunkPosition position)
        {
            Span<ushort> ids = stackalloc ushort[Chunk.NUM_CUBES_IN_CHUNK];
            GetIdsForChunk(position, ids);
            var chunk = Palettize(position, ids);
            var newIds = Depaletteize(chunk);

            for (int i = 0; i < Chunk.NUM_CUBES_IN_CHUNK; i++)
            {
                IMGUIConsole.Assert(ids[i] == newIds[i]);
            }
        }
    }
}
