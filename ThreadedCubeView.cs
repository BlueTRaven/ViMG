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
        //something to lock
        private object manager = new object();

        private ChunkManager2.GetCubeIdDel getCubeId;
        private ChunkManager2.GetCubeDel getCube;
        private ChunkManager2.GetCachedFacesDel getCachedFaces;
        private ChunkManager2.SetCubeDel setCube;

        public ThreadedCubeView(ChunkManager2.GetCubeIdDel getCubeId, ChunkManager2.GetCubeDel getCube, ChunkManager2.GetCachedFacesDel getCachedFaces, ChunkManager2.SetCubeDel setCube)
        {
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
    }
}
