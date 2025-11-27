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
            byte[] bytes = io.GetBytes();

            int cubeOffset = ChunkManagerIO.GetCubeOffset(position);

            ushort id = Unsafe.ReadUnaligned<ushort>(ref bytes[cubeOffset * sizeof(ushort)]);

            //BitConverter is apparently faster than fixed cast of bytes to ushort
            return id;
        }

        public unsafe void GetIds(Span<CubePosition> positions, Span<ushort> ids, int offset = 0, int count = -1)
        {
            using var zone = TracyImpl.Tracy.BeginZone();

            if (count == -1)
                count = positions.Length;

            byte[] idBytes = io.GetBytes();

            fixed (ushort* idsPtr = ids) 
            {
                for (int i = offset; i < offset + count; i++)
                {
                    int cubeOffset = ChunkManagerIO.GetCubeOffset(positions[i]);
                    idsPtr[i] = Unsafe.ReadUnaligned<ushort>(ref idBytes[cubeOffset * sizeof(ushort)]);
                }
            }
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

        public void GetCubes(Span<CubePosition> positions, Span<Cube> cubes, Cube def, int offset = 0, int count = -1)
        {
            if (count == -1)
                count = positions.Length;

            byte[] idBytes = io.GetBytes();
            var registry = Main.Registry.CubeRegistry.GetIterable();

            for (int i = offset; i < offset + count; i++)
            {
                int cubeOffset = ChunkManagerIO.GetCubeOffset(positions[i]);
                ushort id = Unsafe.ReadUnaligned<ushort>(ref idBytes[cubeOffset * sizeof(ushort)]);

                if (id - 1 < 0)
                    cubes[i] = def;
                else cubes[i] = registry[id - 1];
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
            byte[] bytes = io.GetBytes();

            ChunkPosition chunkPos = ChunkPosition.CubeChunk(position);
            CubePosition positionChS = position.InChunkSpace(chunkPos);
            Util.ThreeDToOneD(new ValuePoint3D(positionChS.X, positionChS.Y, positionChS.Z), new ValuePoint3D(Chunk.CHUNK_SIZE), out int ci);

            int cubeOffset = ChunkManagerIO.GetCubeOffset(position);

            ushort oldId = Unsafe.ReadUnaligned<ushort>(ref bytes[cubeOffset * sizeof(ushort)]);
            Unsafe.WriteUnaligned<ushort>(ref bytes[cubeOffset * sizeof(ushort)], id);

            if (markDirty)
            {
                chunkManager.MarkCubeMeshInfoDirty(null, position, oldId, id);
                chunkManager.ChunkMesher?.MarkChunkDirty(chunkPos);
            }
        }

        public void SetCube(CubePosition position, ushort id, Player player)
        {
            byte[] bytes = io.GetBytes();

            ChunkPosition chunkPos = ChunkPosition.CubeChunk(position);
            CubePosition positionChS = position.InChunkSpace(chunkPos);
            Util.ThreeDToOneD(new ValuePoint3D(positionChS.X, positionChS.Y, positionChS.Z), new ValuePoint3D(Chunk.CHUNK_SIZE), out int ci);

            int cubeOffset = ChunkManagerIO.GetCubeOffset(position);

            ushort oldId = Unsafe.ReadUnaligned<ushort>(ref bytes[cubeOffset * sizeof(ushort)]);
            Unsafe.WriteUnaligned<ushort>(ref bytes[cubeOffset * sizeof(ushort)], id);

            chunkManager.MarkCubeMeshInfoDirty(player, position, oldId, id);
            chunkManager.ChunkMesher?.MarkChunkDirty(chunkPos);
        }

        public void SetCubes(Span<CubePosition> positions, Span<ushort> ids, int offset = 0, int count = -1)
        {
            byte[] bytes = io.GetBytes();

            for (int i = 0; i < positions.Length; i++)
            {
                var position = positions[i];
                CubePosition positionChS = position.InChunkSpace();
                Util.ThreeDToOneD(new ValuePoint3D(positionChS.X, positionChS.Y, positionChS.Z), new ValuePoint3D(Chunk.CHUNK_SIZE), out int ci);

                int cubeOffset = ChunkManagerIO.GetCubeOffset(position);

                Unsafe.WriteUnaligned<ushort>(ref bytes[cubeOffset * sizeof(ushort)], ids[i]);
            } 
        }

        public void SetCubes(Span<CubePosition> positions, ushort id, int offset = 0, int count = -1)
        {
            byte[] bytes = io.GetBytes();

            for (int i = 0; i < positions.Length; i++)
            {
                var position = positions[i];
                CubePosition positionChS = position.InChunkSpace();
                Util.ThreeDToOneD(new ValuePoint3D(positionChS.X, positionChS.Y, positionChS.Z), new ValuePoint3D(Chunk.CHUNK_SIZE), out int ci);

                int cubeOffset = ChunkManagerIO.GetCubeOffset(position);

                Unsafe.WriteUnaligned<ushort>(ref bytes[cubeOffset * sizeof(ushort)], id);
            }
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

        public struct PalettizedChunk
        {
            public ChunkPosition position;
            public PalettizeType type;
            public ushort[] palette;
            public byte[]? data;
        }

        public PalettizedChunk Palettize(ChunkPosition chunkPosition, Span<ushort> ids)
        {
            var chunk = new PalettizedChunk
            {
                type = PalettizeType.AllOneId,
                position = chunkPosition,
            };

            IMGUIConsole.Assert(ids.Length == Chunk.NUM_CUBES_IN_CHUNK);

            //Span<ushort> ids = stackalloc ushort[Chunk.NUM_CUBES_IN_CHUNK];
            GetIdsForChunk(chunkPosition, ids);
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

        public ushort[] Depaletteize(PalettizedChunk chunk)
        {
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
