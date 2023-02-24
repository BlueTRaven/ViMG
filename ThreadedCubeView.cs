using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        private readonly ChunkManager2 manager;
        private readonly ChunkLoadManager loadManager;

        private ChunkManager2.GetCubeIdDel getCubeId;
        private ChunkManager2.GetCubeDel getCube;
        private ChunkManager2.GetCachedFacesDel getCachedFaces;
        private ChunkManager2.SetCubeDel setCube;

        public ThreadedCubeView(ChunkManager2 manager, ChunkLoadManager loadManager, ChunkManager2.GetCubeIdDel getCubeId, ChunkManager2.GetCubeDel getCube, ChunkManager2.GetCachedFacesDel getCachedFaces, ChunkManager2.SetCubeDel setCube)
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
            lock (manager)
            {
                return getCubeId(position);
            }
        }

        public void GetIds(Span<CubePosition> positions, Span<ushort> ids)
        {
            lock (manager)
            {
                for (int i = 0; i < positions.Length; i++)
                {
                    ids[i] = getCubeId(positions[i]);
                }
            }
        }

        public void GetIds(Span<CubePosition> positions, Span<ushort> ids, SafetyCheck check)
        {
            lock (manager)
            {
                for (int i = 0; i < positions.Length; i++)
                {
                    bool valid = true;
                    if ((check & SafetyCheck.InWorldBounds) == SafetyCheck.InWorldBounds && !manager.IsInWorldBounds(positions[i]))
                        valid = false;
                    if ((check & SafetyCheck.IsLoaded) == SafetyCheck.IsLoaded && !loadManager.IsLoaded(ChunkPosition.CubeChunk(positions[i])))
                        valid = false;

                    if (valid)
                        ids[i] = getCubeId(positions[i]);
                    else ids[i] = 0;
                }
            }
        }

        public Optional<Cube> GetCube(CubePosition position)
        {
            lock (manager)
            {
                return getCube(position);
            }
        }

        public void GetCubes(Span<CubePosition> positions, Span<Cube> cubes)
        {
            lock (manager)
            {
                for (int i = 0; i < positions.Length; i++)
                {
                    cubes[i] = getCube(positions[i]).GetOrDefault(Main.Registry.CubeRegistry.Air);
                }
            }
        }

        public MeshHelper.CubeFace GetFace(CubePosition position)
        {
            lock (manager)
            {
                return getCachedFaces(position);
            }
        }

        public void GetFaces(Span<CubePosition> positions, Span<MeshHelper.CubeFace> faces)
        {
            lock (manager)
            {
                for (int i = 0; i < positions.Length; i++)
                {
                    faces[i] = getCachedFaces(positions[i]);
                }
            }
        }

        public void SetCube(CubePosition position, ushort id)
        {
            lock (manager)
            {
                setCube(position, id);
            }
        }

        public void SetCubes(Span<CubePosition> positions, Span<ushort> ids)
        {
            lock (manager)
            {
                for (int i = 0; i < positions.Length; i++)
                {
                    setCube(positions[i], ids[i]);
                }
            }
        }

        //sets all cubes at positions positions to id.
        public void SetCubes(Span<CubePosition> positions, ushort id)
        {
            lock (manager)
            {
                for (int i = 0; i < positions.Length; i++)
                {
                    setCube(positions[i], id);
                }
            }
        }
    }
}
