using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Cubes;

namespace ViMG
{
    public abstract class CubeView
    {
        protected readonly ChunkManager2 manager;

        public CubeView(ChunkManager2 manager)
        {
            this.manager = manager;
        }

        public abstract Optional<Cube> GetCube(CubePosition position);

        public abstract void SetCube(CubePosition position, ushort id);
    }
}
