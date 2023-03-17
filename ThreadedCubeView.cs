using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ViMG.Cubes;

namespace ViMG
{
    public class ThreadedCubeView
    {
        public enum SafetyCheck
        {
            None = 0,
            InWorldBounds = 1 << 0,
            IsLoaded = 1 << 1,
        }

        //something to lock
        private readonly ChunkManager manager;
        private readonly ChunkLoadManager loadManager;

        private ChunkManager.GetCubeIdDel getCubeId;
        private ChunkManager.GetCubeDel getCube;
        private ChunkManager.GetFacesDel getCachedFaces;
        private ChunkManager.SetCubeDel setCube;

        public ThreadedCubeView(ChunkManager manager, ChunkLoadManager loadManager, ChunkManager.GetCubeIdDel getCubeId, ChunkManager.GetCubeDel getCube, ChunkManager.GetFacesDel getCachedFaces, ChunkManager.SetCubeDel setCube)
        {
            this.manager = manager;
            this.loadManager = loadManager;
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

        public void GetIds(Span<CubePosition> positions, Span<ushort> ids, SafetyCheck check)
        {
            for (int i = 0; i < positions.Length; i++)
            {
                bool valid = true;
                if ((check & SafetyCheck.InWorldBounds) == SafetyCheck.InWorldBounds && !manager.IsInWorldBounds(positions[i]))
                    valid = false;
                if (loadManager != null && (check & SafetyCheck.IsLoaded) == SafetyCheck.IsLoaded && !loadManager.IsLoaded(ChunkPosition.CubeChunk(positions[i])))
                    valid = false;

                if (valid)
                    ids[i] = getCubeId(positions[i]);
                else ids[i] = 0;
            }
        }

        public Optional<Cube> GetCube(CubePosition position)
        {
            return getCube(position);
        }

        public void GetCubes(Span<CubePosition> positions, Span<Cube> cubes)
        {
            for (int i = 0; i < positions.Length; i++)
            {
                cubes[i] = getCube(positions[i]).GetOrDefault(Main.Registry.CubeRegistry.Air);
            }
        }

        //TODO optimize
        public OptionalValue<CubePosition> GetFirstSolidDown(CubePosition start, int searchLimit = 512)
        {
            for (int y = 0; y < manager.SizeInCubes; y++)
            {
                CubePosition pos = new CubePosition(start.X, start.Y - y, start.Z);

                //Null check here is the same as doing out of bounds check.
                Cube cubeAtPos = GetCube(pos).Get();
                if (cubeAtPos != null && cubeAtPos.Solid)
                    return new OptionalValue<CubePosition>(pos);
            }

            return new OptionalValue<CubePosition>();
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

        public void SetCube(CubePosition position, ushort id)
        {
            setCube(position, id);
        }

        public void SetCubes(Span<CubePosition> positions, Span<ushort> ids, int offset = 0, int count = -1)
        {
            if (count == -1)
                count = positions.Length;

            for (int i = offset; i < offset + count; i++)
            {
                setCube(positions[i], ids[i]);
            }
        }

        //sets all cubes at positions positions to id.
        public void SetCubes(Span<CubePosition> positions, ushort id, int offset = 0, int count = -1)
        {
            if (count == -1)
                count = positions.Length;

            for (int i = offset; i < offset + count; i++)
            {
                setCube(positions[i], id);
            }
        }
    }
}
