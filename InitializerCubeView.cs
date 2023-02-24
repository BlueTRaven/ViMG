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
        private ChunkManager.GetCubeIdDel getCubeId;
        private ChunkManager.GetCubeDel getCube;
        private ChunkManager.GetCachedFacesDel getCachedFaces;
        private ChunkManager.SetCubeDel setCube;

        public InitializerCubeView(ChunkManager.GetCubeIdDel getCubeId, ChunkManager.GetCubeDel getCube, ChunkManager.GetCachedFacesDel getCachedFaces, ChunkManager.SetCubeDel setCube)
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
