using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Cubes;

namespace ViMG.ChunkStuff
{
    public struct CopiedChunkData
    {
        public const int WHD = Chunk.CHUNK_SIZE + 2;
        public const int SIZE = WHD * WHD * WHD;
        public ushort[] ids;
        public object[] entityMeshingDatas;

        //Note that this represents the topleftfront of the Chunk. It does NOT include the padding.
        //I.e. padding left, front, top is -1.
        public CubePosition basePosition;

        public object GetEntityMeshingData(CubePosition position)
        {
            //Add one since padding is -1
            Util.ThreeDToOneD(new ValuePoint3D(position.X + 1, position.Y + 1, position.Z + 1), new ValuePoint3D(WHD), out int i);
            return entityMeshingDatas[i];
        }

        public ushort GetId(CubePosition position)
        {
            //Add one since padding is -1
            Util.ThreeDToOneD(new ValuePoint3D(position.X + 1, position.Y + 1, position.Z + 1), new ValuePoint3D(WHD), out int i);
            return ids[i];
        }

        public void GetIds(Span<CubePosition> positions, Span<ushort> faces, int offset = 0, int count = -1)
        {
            if (count == -1)
                count = positions.Length;

            for (int i = offset; i < offset + count; i++)
            {
                faces[i] = GetId(positions[i]);
            }
        }

        public Optional<Cube> GetCube(CubePosition position)
        {
            //Add one since padding is -1
            Util.ThreeDToOneD(new ValuePoint3D(position.X + 1, position.Y + 1, position.Z + 1), new ValuePoint3D(WHD), out int i);
            return new Optional<Cube>(Main.Registry.CubeRegistry.Get(ids[i]));
        }

        public MeshHelper.CubeFace GetFace(CubePosition position)
        {
            Cube cube = GetCube(position).GetOrDefault(Main.Registry.CubeRegistry.Air);

            if (cube.Transparency == Cube.TransparencyValue.Invisible)
                return MeshHelper.CubeFace.NONE;

            MeshHelper.CubeFace faces = MeshHelper.CubeFace.NONE;

            if (HasClearSide(position.X + 1, position.Y, position.Z, cube))
                faces |= MeshHelper.CubeFace.LEFT;
            if (HasClearSide(position.X - 1, position.Y, position.Z, cube))
                faces |= MeshHelper.CubeFace.RIGHT;

            if (HasClearSide(position.X, position.Y - 1, position.Z, cube))
                faces |= MeshHelper.CubeFace.DOWN;
            if (HasClearSide(position.X, position.Y + 1, position.Z, cube))
                faces |= MeshHelper.CubeFace.UP;

            if (HasClearSide(position.X, position.Y, position.Z - 1, cube))
                faces |= MeshHelper.CubeFace.FRONT;
            if (HasClearSide(position.X, position.Y, position.Z + 1, cube))
                faces |= MeshHelper.CubeFace.BACK;

            return faces;
        }

        //TODO: separate out visual stuff, not sure how yet
        private bool HasClearSide(int x, int y, int z, Cube currentCube)
        {
            Cube adjacentCube = GetCube(new CubePosition(x, y, z)).GetOrDefault(Main.Registry.CubeRegistry.Air);

            if (currentCube.Transparency != Cube.TransparencyValue.Air)
            {
                switch (adjacentCube.Transparency)
                {
                    case (Cube.TransparencyValue.Transparent):
                    case (Cube.TransparencyValue.Invisible):
                    case (Cube.TransparencyValue.Air):
                        return true;
                    case (Cube.TransparencyValue.TransparentOccludesSiblings):
                        return currentCube != adjacentCube;
                    default:
                        return false;
                }

            }
            else if (currentCube.Transparency == Cube.TransparencyValue.Air)
                return currentCube != adjacentCube;

            return false;
        }

        public void GetFaces(Span<CubePosition> positions, Span<MeshHelper.CubeFace> faces, int offset = 0, int count = -1)
        {
            if (count == -1)
                count = positions.Length;

            for (int i = offset; i < offset + count; i++)
            {
                faces[i] = GetFace(positions[i]);
            }
        }
    }
}
