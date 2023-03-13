using SharpDX.MediaFoundation;
using System;
using System.Collections.Generic;
using System.Linq;
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

        private ChunkManager.GetCubeIdDel getCubeId;
        private ChunkManager.GetCubeDel getCube;
        private ChunkManager.GetFacesDel getCachedFaces;
        private ChunkManager.SetCubeDel setCube;

        public InitializerCubeView(ChunkManager chunkManager, ChunkManager.GetCubeIdDel getCubeId, ChunkManager.GetCubeDel getCube, ChunkManager.GetFacesDel getCachedFaces, ChunkManager.SetCubeDel setCube)
        {
            this.chunkManager = chunkManager;
            this.getCubeId = getCubeId;
            this.getCube = getCube;
            this.getCachedFaces = getCachedFaces;
            this.setCube = setCube;
        }

        public ushort GetId(CubePosition position)
        {
            return getCubeId(position);
        }

        public void GetIds(Span<CubePosition> positions, Span<ushort> ids, int offset = 0, int count = -1)
        {
            if (count == -1)
                count = positions.Length;

            for (int i = offset; i < offset + count; i++)
            {
                ids[i] = getCubeId(positions[i]);
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

            for (int i = offset; i < offset + count; i++)
            {
                cubes[i] = getCube(positions[i]).GetOrDefault(def);
            }
        }

        public MeshHelper.CubeFace GetFace(CubePosition position)
        {
            return getCachedFaces(position);
        }

        public void GetFaces(Span<CubePosition> positions, Span<MeshHelper.CubeFace> faces, int offset = 0, int count = -1)
        {
            if (count == -1)
                count = positions.Length;

            for (int i = offset; i < offset + count; i++)
            {
                faces[i] = getCachedFaces(positions[i]);
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
