using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using ViMG.Cubes;

namespace ViMG.ChunkStuff
{
    public class CopiedChunkData
    {
        public const int WHD = Chunk.CHUNK_SIZE + 2;
        public const int SIZE = WHD * WHD * WHD;
        public ushort[] Ids;
        public object[] EntityMeshingDatas;

        //Note that this represents the topleftfront of the Chunk. It does NOT include the padding.
        //I.e. padding left, front, top is -1.
        public CubePosition BasePosition;

        private bool valid;

        public readonly int Index;

        public CopiedChunkData(int index)
        {
            valid = false;
            this.Index = index;
        }

        public void Take(CubePosition position)
        {
            this.BasePosition = position;

            if (Ids == null)
                Ids = new ushort[SIZE];
            if (EntityMeshingDatas == null)
                EntityMeshingDatas = new object[SIZE];

            valid = true;
        }

        public void Return()
        {
            //remove references, since they might stick around for too long otherwise
            for (int i = 0; i < EntityMeshingDatas.Length; i++)
                EntityMeshingDatas[i] = null;

            valid = false;
        }

        public bool GetValid()
        {
            return valid;
        }

        public object GetEntityMeshingData(CubePosition position)
        {
            //Add one since padding is -1
            Util.ThreeDToOneD(new ValuePoint3D(position.X + 1, position.Y + 1, position.Z + 1), new ValuePoint3D(WHD), out int i);
            return EntityMeshingDatas[i];
        }

        public ushort GetId(CubePosition position)
        {
            //Add one since padding is -1
            Util.ThreeDToOneD(new ValuePoint3D(position.X + 1, position.Y + 1, position.Z + 1), new ValuePoint3D(WHD), out int i);
            return Ids[i];
        }

        public void GetIds(Span<CubePosition> positions, Span<ushort> ids, int offset = 0, int count = -1)
        {
            if (count == -1)
                count = positions.Length;

            for (int i = offset; i < offset + count; i++)
            {
                ids[i] = GetId(positions[i]);
            }
        }

        public Optional<Cube> GetCube(CubePosition position)
        {
            //Add one since padding is -1
            Util.ThreeDToOneD(new ValuePoint3D(position.X + 1, position.Y + 1, position.Z + 1), new ValuePoint3D(WHD), out int i);
            return new Optional<Cube>(Main.Registry.CubeRegistry.Get(Ids[i]));
        }

        public MeshHelper.CubeFace GetFace(CubePosition position)
        {
            Cube cube = GetCube(position).GetOrDefault(Main.Registry.CubeRegistry.Air);

            //TODO re-enable air
            if (cube.Transparency == Cube.TransparencyValue.Invisible || cube.Transparency == Cube.TransparencyValue.Air)
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

        private static CubePosition[] adjacentOffsets = new CubePosition[6]
        {
            new CubePosition(-1, 0, 0),
            new CubePosition(1, 0, 0),
            new CubePosition(0, -1, 0),
            new CubePosition(0, 1, 0),
            new CubePosition(0, 0, -1),
            new CubePosition(0, 0, 1)
        };

        private static MeshHelper.CubeFace[] adjacentFaces = new MeshHelper.CubeFace[6]
        {
            MeshHelper.CubeFace.RIGHT,
            MeshHelper.CubeFace.LEFT,
            MeshHelper.CubeFace.DOWN,
            MeshHelper.CubeFace.UP,
            MeshHelper.CubeFace.FRONT,
            MeshHelper.CubeFace.BACK
        };

        public void GetFaces(Span<CubePosition> positions, Span<MeshHelper.CubeFace> faces, int offset = 0, int count = -1)
        {
            if (count == -1)
                count = positions.Length;

            //var registry = Main.Registry.CubeRegistry.GetIterable();

            for (int i = offset; i < offset + count; i++)
            {
                faces[i] = GetFace(positions[i]);
                /*Util.ThreeDToOneD(new ValuePoint3D(positions[i].X + 1, positions[i].Y + 1, positions[i].Z + 1), new ValuePoint3D(WHD), out int posIndex);
                Cube cube;
                if (Ids[posIndex] == 0)
                    cube = Main.Registry.CubeRegistry.Air;
                else cube = registry[Ids[posIndex] - 1];

                faces[i] = MeshHelper.CubeFace.NONE;

                for (int k = 0; k < 6; k++)
                {
                    CubePosition adjacentPosition = positions[i] + adjacentOffsets[k];
                    Util.ThreeDToOneD(new ValuePoint3D(adjacentPosition.X + 1, adjacentPosition.Y + 1, adjacentPosition.Z + 1), new ValuePoint3D(WHD), out int adjPosIndex);
                    Cube adjacentCube;
                    if (Ids[adjPosIndex] == 0)
                        adjacentCube = Main.Registry.CubeRegistry.Air;
                    else adjacentCube = registry[Ids[adjPosIndex] - 1];

                    if (cube.Transparency != Cube.TransparencyValue.Air)
                    {
                        switch (adjacentCube.Transparency)
                        {
                            case (Cube.TransparencyValue.Transparent):
                            case (Cube.TransparencyValue.Invisible):
                            case (Cube.TransparencyValue.Air):
                                faces[i] |= adjacentFaces[k];
                                break;
                            case (Cube.TransparencyValue.TransparentOccludesSiblings):
                                if (cube != adjacentCube)
                                    faces[i] |= adjacentFaces[k];
                                break;
                            default:
                                break;
                        }

                    }
                    else if (cube.Transparency == Cube.TransparencyValue.Air && cube != adjacentCube)
                        faces[i] |= adjacentFaces[k];
                }*/
            }
        }
    }
}
