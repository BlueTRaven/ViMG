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
        private ChunkManager2.GetCubeIdDel getCubeId;
        private ChunkManager2.GetCubeDel getCube;
        private ChunkManager2.GetCachedFacesDel getCachedFaces;
        private ChunkManager2.SetCubeDel setCube;

        public InitializerCubeView(ChunkManager2.GetCubeIdDel getCubeId, ChunkManager2.GetCubeDel getCube, ChunkManager2.GetCachedFacesDel getCachedFaces, ChunkManager2.SetCubeDel setCube)
        {
            this.getCubeId = getCubeId;
            this.getCube = getCube;
            this.getCachedFaces = getCachedFaces;
            this.setCube = setCube;
        }

        public Optional<Cube> GetCube(CubePosition position)
        {
            return getCube(position);
        }

        public void SetCube(CubePosition position, ushort id, bool markDirty = false)
        {
            setCube(position, id, markDirty);
        }
    }
}
