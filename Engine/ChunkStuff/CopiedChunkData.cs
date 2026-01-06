using BepuUtilities.Memory;
using BrUtility;
using Engine.Networking;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using ViMG.Cubes;
using ViMG.IMGUIImpl;

namespace ViMG.ChunkStuff
{
    public class CopiedChunkData
    {
        public const int PADDING = 1;
        public const int WHD = Chunk.CHUNK_SIZE + (PADDING * 2);
        public const int SIZE = WHD * WHD * WHD;
        public ushort[] PaddingIds;
        public ushort[] Ids;
        public Buffer<byte>[] EntityMeshingDatas;
        private int[] entityMeshingDatas2Mapping;
        private FastList<BasicState> entityMeshingDatas2;
        //public BasicState[] EntityMeshingDatas2;

        //Note that this represents the topleftfront of the Chunk. It does NOT include the padding.
        //I.e. padding left, front, top is -1.
        public CubePosition BasePosition;

        private int _refcount;
        public int refcount
        {
            get => _refcount; set
            {
                if (value > refmax)
                    refmax = value;
                _refcount = value;
            }
        }
        private int refmax = 0;
        public bool render = false;
        public bool collision = false;
        public bool loadAroundTarget = false;
        private bool valid;

        public readonly int Index;

        public CopiedChunkData(int index)
        {
            valid = false;
            this.Index = index;
        }

        public void Take(CubePosition position)
        {
            IMGUIConsole.Assert(!valid);
            this.BasePosition = position;

            if (Ids == null)
            {
                PaddingIds = new ushort[SIZE];
                Ids = new ushort[Chunk.NUM_CUBES_IN_CHUNK];
            }
            if (EntityMeshingDatas == null)
                EntityMeshingDatas = new Buffer<byte>[SIZE];
            if (entityMeshingDatas2 == null)
            {
                entityMeshingDatas2 = new();
                entityMeshingDatas2Mapping = new int[SIZE];
                Array.Fill(entityMeshingDatas2Mapping, -1);
            }

            valid = true;
        }

        public void Return(BufferPool pool)
        {
            refcount -= 1;
            IMGUIConsole.Assert(refcount >= 0);
            if (refcount == 0)
            {
                IMGUIConsole.Assert(valid);

                for (int i = 0; i < EntityMeshingDatas.Length; i++)
                    if (EntityMeshingDatas[i].Allocated)
                        pool.Return(ref EntityMeshingDatas[i]);

                entityMeshingDatas2.Clear();
                Array.Fill(entityMeshingDatas2Mapping, -1);
                render = false;
                collision = false;
                loadAroundTarget = false;
                valid = false;
                refmax = 0;
            }
        }

        public bool GetValid()
        {
            return valid;
        }

        public unsafe T GetEntityMeshingData<T>(CubePosition position) where T : unmanaged
        {
            using var zone = TracyImpl.Tracy.BeginZone();

            //Add one since padding is -1
            Util.ThreeDToOneD(new ValuePoint3D(position.X + 1, position.Y + 1, position.Z + 1), new ValuePoint3D(WHD), out int i);
            if (!EntityMeshingDatas[i].Allocated)
                return default;
            else return *EntityMeshingDatas[i].As<T>().Memory;
        }

        public BasicState GetEntityMeshingData2(CubePosition position)
        {
            Util.ThreeDToOneD(new ValuePoint3D(position.X + 1, position.Y + 1, position.Z + 1), new ValuePoint3D(WHD), out int i);
            int mdi = entityMeshingDatas2Mapping[i];
            if (mdi == -1) return new();
            return entityMeshingDatas2[mdi];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ushort GetId(CubePosition position)
        {
            IMGUIConsole.Assert(position.Coord == CubePosition.CoordinateSpace.ChunkSpace);

            if (position.X < 0 || position.Y < 0 || position.Z < 0 ||
                position.X >= Chunk.CHUNK_SIZE || position.Y >= Chunk.CHUNK_SIZE || position.Z >= Chunk.CHUNK_SIZE)
            {
                //Add one since padding is -1
                Util.ThreeDToOneD(new ValuePoint3D(position.X + 1, position.Y + 1, position.Z + 1), new ValuePoint3D(WHD), out int i);
                return PaddingIds[i];
            } else
            {
                Util.ThreeDToOneD(new ValuePoint3D(position.X, position.Y, position.Z), new ValuePoint3D(Chunk.CHUNK_SIZE), out int i);
                return Ids[i];
            }
        }

        public void GetIds(Span<CubePosition> positions, Span<ushort> ids, int offset = 0, int count = -1)
        {
            //using var zone = TracyImpl.Tracy.BeginZone();

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
            var id = GetId(position);
            return new Optional<Cube>(Main.Registry.CubeRegistry.Get(id));
        }

        public MeshHelper.CubeFace GetFace(CubePosition position)
        {
            //using var zone = TracyImpl.Tracy.BeginZone();

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
            Cube adjacentCube = GetCube(new CubePosition(x, y, z, CubePosition.CoordinateSpace.ChunkSpace)).GetOrDefault(Main.Registry.CubeRegistry.Air);

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

        public void GetFaces(Span<CubePosition> positions, Span<MeshHelper.CubeFace> faces)
        {
            //using var zone = TracyImpl.Tracy.BeginZone();

            Debug.Assert(positions.Length == faces.Length);
            for (int i = 0; i < positions.Length; i++)
            {
                faces[i] = GetFace(positions[i]);
            }
        }
    }
}
