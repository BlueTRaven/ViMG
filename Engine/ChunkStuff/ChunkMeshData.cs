using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViMG.ChunkStuff
{
    public ref struct ChunkMeshData
    {
        public Span<CubePosition> positions;
        public Span<MeshHelper.CubeFace> faces;

        public ChunkMeshData(Span<CubePosition> positions, Span<MeshHelper.CubeFace> faces)
        {
            this.positions = positions;
            this.faces = faces;
        }
    }
}
