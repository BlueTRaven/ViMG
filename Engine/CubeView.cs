using Engine.ChunkStuff;
using LiteNetLib.Utils;
using Microsoft.Xna.Framework;
using SharpDX.MediaFoundation;
using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using ViMG.Cubes;
using ViMG.IMGUIImpl;
using static Engine.Networking.Messages.SyncChunk;
using static ViMG.World;

namespace ViMG
{
    public class CubeView : ICubeGetter
    {
        private readonly ChunkManager chunkManager;
        private readonly ChunkManagerIO io;

        public CubeView(ChunkManager chunkManager, ChunkManagerIO io)
        {
            this.chunkManager = chunkManager;
            this.io = io;
        }

        public ushort GetId(CubePosition position)
        {
            //var palChunk = io.GetPalettizedChunk(ChunkPosition.CubeChunk(position));
            //if (palChunk != null)
            //{
            //    // If the chunk is still palettized, don't force it to be unpalettized
            //    return palChunk.Value.GetId(position);
            //}
            //else
            {
                ReadOnlySpan<ushort> ids = io.GetChunk(ChunkPosition.CubeChunk(position), ChunkManagerIO.GetMode.Read);

                var posInChunkSpace = position.InChunkSpace();
                Util.ThreeDToOneD(new ValuePoint3D(posInChunkSpace), new ValuePoint3D(Chunk.CHUNK_SIZE), out int i);
                var id = ids[i];
                io.ReleaseChunk(ChunkPosition.CubeChunk(position), ChunkManagerIO.GetMode.Read);
                return id;
            }
        }

        private struct SortedCubePos
        {
            public int originalIndex;
            public CubePosition cubePos;
        }
        private struct Comparer : IComparer<SortedCubePos>
        {
            public int Compare(SortedCubePos a, SortedCubePos b)
            {
                var chunkPosA = ChunkPosition.CubeChunk(a.cubePos);
                var chunkPosB = ChunkPosition.CubeChunk(b.cubePos);
                if (chunkPosA == chunkPosB) return 0;
                if (chunkPosA.X > chunkPosB.X) return -1;
                return 1;
            }
        }

        private enum GetIdsMethod
        {
            Sorted,
            Unsorted,
            Dict
        };
        private const GetIdsMethod getIdsMethod = GetIdsMethod.Dict;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void GetIds(Span<CubePosition> positions, Span<ushort> ids)
        {
            //GetIdsSorted(positions, ids);
            GetIdsUnsorted(positions, ids);
            //GetIdsDict(positions, ids);
        }

        private unsafe void GetIdsSorted(Span<CubePosition> positions, Span<ushort> ids)
        {
            using var zone = TracyImpl.Tracy.BeginZone(name: "GetIdsSorted");

            Span<SortedCubePos> sortedPositions = stackalloc SortedCubePos[positions.Length];
            using (var zsort = TracyImpl.Tracy.BeginZone(name: "GetIdsSorted: Sort"))
            {
                for (int i = 0; i < positions.Length; i++)
                {
                    sortedPositions[i] = new SortedCubePos
                    {
                        cubePos = positions[i],
                        originalIndex = i,
                    };
                }

                // Positions is sorted before hand to make things more optimal
                // Need to keep track of original index as sometimes that's important, so that's why we copy
                MemoryExtensions.Sort(sortedPositions, new Comparer());
            }

            ChunkPosition cachedChunkPos = new ChunkPosition(-1, -1, -1);
            ReadOnlySpan<ushort> cachedChunkData = null;

            using (var zsort = TracyImpl.Tracy.BeginZone(name: "GetIdsSorted: Get"))
            {
                for (int i = 0; i < positions.Length; i++)
                {
                    CubePosition pos = sortedPositions[i].cubePos;
                    ChunkPosition chunkPos = ChunkPosition.CubeChunk(pos);
                    if (cachedChunkData == null || chunkPos != cachedChunkPos)
                    {
                        if (cachedChunkData != null)
                            io.ReleaseChunk(cachedChunkPos, ChunkManagerIO.GetMode.Read);

                        cachedChunkPos = chunkPos;
                        cachedChunkData = io.GetChunk(chunkPos, ChunkManagerIO.GetMode.Read);
                    }

                    Util.ThreeDToOneD(new ValuePoint3D(pos.InChunkSpace()), new ValuePoint3D(Chunk.CHUNK_SIZE), out int j);
                    ids[sortedPositions[i].originalIndex] = cachedChunkData[j];
                }

                if (cachedChunkData != null)
                    io.ReleaseChunk(cachedChunkPos, ChunkManagerIO.GetMode.Read);
            }
        }

        private unsafe void GetIdsUnsorted(Span<CubePosition> positions, Span<ushort> ids)
        {
            using var zone = TracyImpl.Tracy.BeginZone(name: "GetIdsUnsorted");

            ChunkPosition cachedChunkPos = new ChunkPosition(-1, -1, -1);
            ReadOnlySpan<ushort> cachedChunkData = null;

            for (int i = 0; i < positions.Length; i++)
            {
                CubePosition pos = positions[i];
                ChunkPosition chunkPos = ChunkPosition.CubeChunk(pos);
                if (cachedChunkData == null || chunkPos != cachedChunkPos)
                {
                    cachedChunkPos = chunkPos;
                    cachedChunkData = io.GetChunk(chunkPos, ChunkManagerIO.GetMode.Read);
                }

                Util.ThreeDToOneD(new ValuePoint3D(pos.InChunkSpace()), new ValuePoint3D(Chunk.CHUNK_SIZE), out int j);
                ids[i] = cachedChunkData[j];
            }
        }

        public unsafe void GetIdsForChunk(ChunkPosition chunkPosition, Span<ushort> queryIds)
        {
            IMGUIConsole.Assert(queryIds.Length == Chunk.NUM_CUBES_IN_CHUNK);

            CubePosition basePosition = chunkPosition.InCubeSpace();

            var chunkData = io.GetChunk(chunkPosition, ChunkManagerIO.GetMode.Read);
            for (int i = 0; i < Chunk.NUM_CUBES_IN_CHUNK; i++)
            {
                Util.OneDToThreeD(i, new ValuePoint3D(Chunk.CHUNK_SIZE), out var p);
                CubePosition pos = new CubePosition(p.x, p.y, p.z);

                if (chunkData == null)
                    queryIds[i] = 0;
                else queryIds[i] = chunkData[i];
            }
        }

        public Optional<Cube> GetCube(CubePosition position)
        {
            if (!chunkManager.IsInWorldBounds(position))
                return new Optional<Cube>();

            ushort id = GetId(position);

            return new Optional<Cube>(Main.Registry.CubeRegistry.Get(id));
        }

        public void GetCubes(Span<CubePosition> positions, Span<Cube> cubes, Cube def)
        {
            Span<ushort> ids = stackalloc ushort[positions.Length];
            GetIds(positions, ids);

            var registry = Main.Registry.CubeRegistry.GetIterable();

            for (int i = 0; i < positions.Length; i++) 
            {
                if (ids[i] - 1 < 0) cubes[i] = def;
                else cubes[i] = registry[ids[i] - 1];
            }
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

        public void SetCube(CubePosition position, ushort id, bool markDirty = true)
        {
            Span<ushort> ids = io.GetChunk(ChunkPosition.CubeChunk(position), ChunkManagerIO.GetMode.Write);

            var posInChunkSpace = position.InChunkSpace();
            Util.ThreeDToOneD(new ValuePoint3D(posInChunkSpace), new ValuePoint3D(Chunk.CHUNK_SIZE), out int i);

            ushort oldId = ids[i];
            ids[i] = id;

            if (markDirty)
            {
                chunkManager.MarkCubeDirty(null, position, oldId, id);
                chunkManager.ChunkMesher?.MarkChunkDirty(ChunkPosition.CubeChunk(position));
            }
        }

        public void SetCube(CubePosition position, ushort id, Player player)
        {
            Span<ushort> cubes = io.GetChunk(ChunkPosition.CubeChunk(position), ChunkManagerIO.GetMode.Write);

            var posInChunkSpace = position.InChunkSpace();
            Util.ThreeDToOneD(new ValuePoint3D(posInChunkSpace), new ValuePoint3D(Chunk.CHUNK_SIZE), out int i);

            ushort oldId = cubes[i];
            cubes[i] = id;

            io.ReleaseChunk(ChunkPosition.CubeChunk(position), ChunkManagerIO.GetMode.Write);

            chunkManager.MarkCubeDirty(player, position, oldId, id);
            chunkManager.ChunkMesher?.MarkChunkDirty(ChunkPosition.CubeChunk(position));
        }

        public void SetCubes(Span<CubePosition> positions, Span<ushort> ids)
        {
            ChunkPosition cachedChunkPos = new ChunkPosition(-1, -1, -1);
            Span<ushort> cachedChunkBytes = null;

            for (int i = 0; i < positions.Length; i++)
            {
                CubePosition pos = positions[i];
                ChunkPosition chunkPos = ChunkPosition.CubeChunk(pos);
                if (cachedChunkBytes == null || chunkPos != cachedChunkPos)
                {
                    if (cachedChunkBytes != null)
                        io.ReleaseChunk(cachedChunkPos, ChunkManagerIO.GetMode.Write);

                    cachedChunkPos = chunkPos;
                    cachedChunkBytes = io.GetChunk(chunkPos, ChunkManagerIO.GetMode.Write);
                }

                Util.ThreeDToOneD(new ValuePoint3D(pos.InChunkSpace()), new ValuePoint3D(Chunk.CHUNK_SIZE), out int j);
                cachedChunkBytes[j] = ids[i];
            }

            if (cachedChunkBytes != null)
                io.ReleaseChunk(cachedChunkPos, ChunkManagerIO.GetMode.Write);
        }

        public void SetCubes(Span<CubePosition> positions, ushort id)
        {
            ChunkPosition cachedChunkPos = new ChunkPosition(-1, -1, -1);
            Span<ushort> cachedChunkBytes = null;

            for (int i = 0; i < positions.Length; i++)
            {
                CubePosition pos = positions[i];
                ChunkPosition chunkPos = ChunkPosition.CubeChunk(pos);
                if (cachedChunkBytes == null || chunkPos != cachedChunkPos)
                {
                    if (cachedChunkBytes != null)
                        io.ReleaseChunk(cachedChunkPos, ChunkManagerIO.GetMode.Write);

                    cachedChunkPos = chunkPos;
                    cachedChunkBytes = io.GetChunk(chunkPos, ChunkManagerIO.GetMode.Write);
                }

                Util.ThreeDToOneD(new ValuePoint3D(pos.InChunkSpace()), new ValuePoint3D(Chunk.CHUNK_SIZE), out int j);
                cachedChunkBytes[j] = id;
            }

            if (cachedChunkBytes != null)
                io.ReleaseChunk(cachedChunkPos, ChunkManagerIO.GetMode.Write);
        }

        public OptionalValue<CubePosition> GetFirstSolidDown(CubePosition start)
        {
            for (int y = 0; y < chunkManager.SizeInCubes; y++)
            {
                CubePosition pos = new CubePosition(start.X, start.Y - y, start.Z);

                //Null check here is the same as doing out of bounds check.
                Cube cubeAtPos = GetCube(pos).Get();
                if (cubeAtPos != null && cubeAtPos.Solid)
                    return new OptionalValue<CubePosition>(pos);
            }

            return new OptionalValue<CubePosition>();
        }

        public bool IsInBounds(CubePosition position)
        {
            return chunkManager.IsInWorldBounds(position);
        }

        public static bool RaycastCallbackTouchable(Vector3 position, object? ctx)
        {
            ICubeGetter cubeView = ctx as ICubeGetter ?? throw new Exception();
            return cubeView.GetCube(CubePosition.FromWorldSpace(position)).GetOrDefault(Main.Registry.CubeRegistry.Air).Touchable;
        }

        public static bool RaycastCallbackSolid(Vector3 position, object? ctx)
        {
            ICubeGetter cubeView = ctx as ICubeGetter ?? throw new Exception();
            return cubeView.GetCube(CubePosition.FromWorldSpace(position)).GetOrDefault(Main.Registry.CubeRegistry.Air).Solid;
        }

        public static bool RaycastCallbackSolidNoRope(Vector3 position, object? ctx)
        {
            ICubeGetter cubeView = ctx as ICubeGetter ?? throw new Exception();
            var cube = cubeView.GetCube(CubePosition.FromWorldSpace(position)).GetOrDefault(Main.Registry.CubeRegistry.Air);
            return cube.Solid && cube.Collision != Cube.CollisionValue.Rope;
        }

        // Basically an impl of Bresenham's. Works in world space.
        public static RaycastResult Raycast(Vector3 start, Vector3 end, Func<Vector3, object?, bool> callback, object? ctx)
        {
            if (float.IsNaN(end.X) || float.IsNaN(end.Y) || float.IsNaN(end.Z))
                return new RaycastResult();

            RaycastResult result = new RaycastResult();

            const float ONE_CUBE = Cube.CUBE_SCALE;

            result.start = start;
            result.end = end;

            float x1 = start.X / ONE_CUBE;
            float y1 = start.Y / ONE_CUBE;
            float z1 = start.Z / ONE_CUBE;
            float x2 = end.X / ONE_CUBE;
            float y2 = end.Y / ONE_CUBE;
            float z2 = end.Z / ONE_CUBE;

            int i = (int)x1;
            int j = (int)y1;
            int k = (int)z1;

            int iend = (int)x2;
            int jend = (int)y2;
            int kend = (int)z2;

            int di = ((x1 < x2) ? 1 : ((x1 > x2) ? -1 : 0));
            int dj = ((y1 < y2) ? 1 : ((y1 > y2) ? -1 : 0));
            int dk = ((z1 < z2) ? 1 : ((z1 > z2) ? -1 : 0));

            float deltatx = 1.0f / Math.Abs(x2 - x1);
            float deltaty = 1.0f / Math.Abs(y2 - y1);
            float deltatz = 1.0f / Math.Abs(z2 - z1);

            float minx = (int)x1, maxx = minx + 1;
            float tx = ((x1 > x2) ? (x1 - minx) : (maxx - x1)) * deltatx;
            float miny = (int)y1, maxy = miny + 1;
            float ty = ((y1 > y2) ? (y1 - miny) : (maxy - y1)) * deltaty;
            float minz = (int)z1, maxz = minz + 1;
            float tz = ((z1 > z2) ? (z1 - minz) : (maxz - z1)) * deltatz;

            Vector3 hitPos = new Vector3(x1 * ONE_CUBE, y1 * ONE_CUBE, z1 * ONE_CUBE);

            while (true)
            {
                if (callback(hitPos, ctx))
                {
                    result.hasHit = true;
                    result.hit = hitPos;
                    return result;
                }

                if (tx <= ty && tx <= tz)
                {
                    if (i == iend)
                    {
                        result.hit = result.end;
                        break;
                    }
                    tx += deltatx;
                    i += di;

                    if (di == 1) hitPos.X += ONE_CUBE;
                    if (di == -1) hitPos.X -= ONE_CUBE;

                    result.normal = new Vector3(-di, 0, 0);
                }
                else if (ty <= tz)
                {
                    if (j == jend)
                    {
                        result.hit = result.end;
                        break;
                    }
                    ty += deltaty;
                    j += dj;

                    if (dj == 1) hitPos.Y += ONE_CUBE;
                    if (dj == -1) hitPos.Y -= ONE_CUBE;

                    result.normal = new Vector3(0, -dj, 0);
                }
                else
                {
                    if (k == kend)
                    {
                        result.hit = result.end;
                        break;
                    }
                    tz += deltatz;
                    k += dk;

                    if (dk == 1) hitPos.Z += ONE_CUBE;
                    if (dk == -1) hitPos.Z -= ONE_CUBE;

                    result.normal = new Vector3(0, 0, -dk);
                }
            }

            return result;
        }
    }
}
