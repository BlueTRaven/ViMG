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
    public class InitializerCubeView
    {
        private readonly ChunkManager2 manager;

        public InitializerCubeView(ChunkManager2 manager)
        {
            this.manager = manager;
        }

        public Optional<Cube> GetCube(CubePosition position)
        {
            return manager.GetCube(position);
        }

        public void SetCube(CubePosition position, ushort id)
        {
            manager.SetCube(position, id, false);
        }
    }
}
