using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using ViMG;
using ViMG.Cubes;
using ViMG.IMGUIImpl;

namespace Engine.ChunkStuff
{
    public interface ICubeGetter
    {
        ushort GetId(CubePosition position);

        void GetIds(Span<CubePosition> positions, Span<ushort> ids);

        Optional<Cube> GetCube(CubePosition position);

        void GetCubes(Span<CubePosition> positions, Span<Cube> cubes, Cube def);

        void GetIdsForChunk(ChunkPosition position, Span<ushort> ids);
    }
}
