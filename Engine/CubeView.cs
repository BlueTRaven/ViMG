using SharpDX.MediaFoundation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using ViMG.Cubes;

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
                chunkManager.MarkCubeMeshInfoDirty(position, oldId, id);
                chunkManager.ChunkMesher?.MarkChunkDirty(chunkPos);
            }
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
    }
}
