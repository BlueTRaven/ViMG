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
    public class InitializerCubeView
    {
        private readonly ChunkManager chunkManager;
        private readonly ChunkManagerIO io;
        private ChunkManager.GetCubeIdDel getCubeId;
        private ChunkManager.GetCubeDel getCube;
        private ChunkManager.GetFacesDel getCachedFaces;
        private ChunkManager.SetCubeDel setCube;

        public InitializerCubeView(ChunkManager chunkManager, ChunkManagerIO io, ChunkManager.GetCubeIdDel getCubeId, ChunkManager.GetCubeDel getCube, ChunkManager.GetFacesDel getCachedFaces, ChunkManager.SetCubeDel setCube)
        {
            this.chunkManager = chunkManager;
            this.io = io;
            this.getCubeId = getCubeId;
            this.getCube = getCube;
            this.getCachedFaces = getCachedFaces;
            this.setCube = setCube;
        }

        public ushort GetId(CubePosition position)
        {
            return getCubeId(position);
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
            return getCube(position);
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

        public MeshHelper.CubeFace GetFace(CubePosition position)
        {
            return getCachedFaces(position);
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

        public void GetFaces(Span<CubePosition> positions, Span<MeshHelper.CubeFace> faces, int offset = 0, int count = -1)
        {
            if (count == -1)
                count = positions.Length;

            byte[] idBytes = io.GetBytes();
            var registry = Main.Registry.CubeRegistry.GetIterable();

            for (int i = offset; i < offset + count; i++)
            {
                int cubeOffset = ChunkManagerIO.GetCubeOffset(positions[i]);
                ushort id = Unsafe.ReadUnaligned<ushort>(ref idBytes[cubeOffset * sizeof(ushort)]);

                Cube cube = registry[id];

                faces[i] = MeshHelper.CubeFace.NONE;

                for (int j = 0; j < 6; j++)
                {
                    int adjacentOffset = ChunkManagerIO.GetCubeOffset(positions[i] + adjacentOffsets[j]);
                    ushort adjacentId = Unsafe.ReadUnaligned<ushort>(ref idBytes[adjacentOffset * sizeof(ushort)]);

                    Cube adjacentCube = registry[adjacentId];

                    if (cube.Transparency != Cube.TransparencyValue.Air)
                    {
                        switch (adjacentCube.Transparency)
                        {
                            case (Cube.TransparencyValue.Transparent):
                            case (Cube.TransparencyValue.Invisible):
                            case (Cube.TransparencyValue.Air):
                                faces[i] |= adjacentFaces[j];
                                break;
                            case (Cube.TransparencyValue.TransparentOccludesSiblings):
                                if (cube != adjacentCube)
                                    faces[i] |= adjacentFaces[j];
                                break;
                            default:
                                break;
                        }

                    }
                    else if (cube.Transparency == Cube.TransparencyValue.Air && cube != adjacentCube)
                        faces[i] |= adjacentFaces[j];
                }
            }
        }

        public void SetCube(CubePosition position, ushort id, bool markDirty = false)
        {
            setCube(position, id, markDirty);
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
