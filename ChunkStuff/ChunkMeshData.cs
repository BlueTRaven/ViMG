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
        public Span<ushort> ids;
        public Span<MeshHelper.CubeFace> faces;

        public ChunkMeshData(Span<CubePosition> positions, Span<ushort> ids, Span<MeshHelper.CubeFace> faces)
        {
            this.positions = positions;
            this.ids = ids;
            this.faces = faces;
        }
    }
}
