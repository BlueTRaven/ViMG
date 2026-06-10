using Engine.ChunkStuff;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using ViMG;
using ViMG.Cubes;
using ViMG.IMGUIImpl;

namespace Engine.Clients
{
    public class ClientCubeView : ICubeGetter
    {
        private readonly ChunkManagerIO io;
        private readonly int sizeInCubes;

        public ClientCubeView(ChunkManagerIO io, int sizeInChunks)
        {
            this.io = io;
            this.sizeInCubes = sizeInChunks * Chunk.CHUNK_SIZE;
        }

        public ushort GetId(CubePosition position)
        {
            if (!io.IsLoaded(ChunkPosition.CubeChunk(position))) return 0;

            ReadOnlySpan<ushort> ids = io.GetChunk(ChunkPosition.CubeChunk(position), ChunkManagerIO.GetMode.Read);

            var posInChunkSpace = position.InChunkSpace();
            Util.ThreeDToOneD(new ValuePoint3D(posInChunkSpace), new ValuePoint3D(Chunk.CHUNK_SIZE), out int i);
            var id = ids[i];
            io.ReleaseChunk(ChunkPosition.CubeChunk(position), ChunkManagerIO.GetMode.Read);
            return id;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void GetIds(Span<CubePosition> positions, Span<ushort> ids)
        {
            GetIdsUnsorted(positions, ids);
        }

        private unsafe void GetIdsUnsorted(Span<CubePosition> positions, Span<ushort> ids)
        {
            using var zone = ViMG.TracyImpl.Tracy.BeginZone(name: "GetIdsUnsorted");

            ChunkPosition cachedChunkPos = new ChunkPosition(-1, -1, -1);
            ReadOnlySpan<ushort> cachedChunkData = null;

            for (int i = 0; i < positions.Length; i++)
            {
                CubePosition pos = positions[i];
                ChunkPosition chunkPos = ChunkPosition.CubeChunk(pos);
                if (cachedChunkData == null || cachedChunkData.Length == 0 || chunkPos != cachedChunkPos)
                {
                    cachedChunkPos = chunkPos;
                    cachedChunkData = io.GetChunk(chunkPos, ChunkManagerIO.GetMode.Read);
                }

                Util.ThreeDToOneD(new ValuePoint3D(pos.InChunkSpace()), new ValuePoint3D(Chunk.CHUNK_SIZE), out int j);
                ids[i] = cachedChunkData[j];
            }

            if (cachedChunkData != null)
                io.ReleaseChunk(cachedChunkPos, ChunkManagerIO.GetMode.Read);
        }

        public void SetId(CubePosition position, ushort id)
        {
            Span<ushort> ids = io.GetChunk(ChunkPosition.CubeChunk(position), ChunkManagerIO.GetMode.Write);
            if (ids == null || ids.Length == 0)
                return;

            var posInChunkSpace = position.InChunkSpace();
            Util.ThreeDToOneD(new ValuePoint3D(posInChunkSpace), new ValuePoint3D(Chunk.CHUNK_SIZE), out int i);

            ids[i] = id;
        }

        public unsafe void GetIdsForChunk(ChunkPosition chunkPosition, Span<ushort> queryIds)
        {
            IMGUIConsole.Assert(queryIds.Length == Chunk.NUM_CUBES_IN_CHUNK);

            CubePosition basePosition = chunkPosition.InCubeSpace();

            var chunkData = io.GetChunk(chunkPosition, ChunkManagerIO.GetMode.Read);
            if (chunkData == null || chunkData.Length == 0)
            {
                queryIds.Fill(0);
                return;
            }

            for (int i = 0; i < Chunk.NUM_CUBES_IN_CHUNK; i++)
            {
                Util.OneDToThreeD(i, new ValuePoint3D(Chunk.CHUNK_SIZE), out var p);
                CubePosition pos = new CubePosition(p.x, p.y, p.z);

                queryIds[i] = chunkData[i];
            }
        }

        public Optional<Cube> GetCube(CubePosition position)
        {
            if (!IsInBounds(position))
                return new Optional<Cube>();

            ushort id = GetId(position);

            return new Optional<Cube>(GlobalState.Registry.CubeRegistry.Get(id));
        }

        public void GetCubes(Span<CubePosition> positions, Span<Cube> cubes, Cube def)
        {
            Span<ushort> ids = stackalloc ushort[positions.Length];
            GetIds(positions, ids);

            var registry = GlobalState.Registry.CubeRegistry.GetIterable();

            for (int i = 0; i < positions.Length; i++)
            {
                if (ids[i] - 1 < 0) cubes[i] = def;
                else cubes[i] = registry[ids[i] - 1];
            }
        }

        public bool IsInBounds(CubePosition position)
        {
            int sign = MathF.Sign(position.Y);

            if (position.Coord == CubePosition.CoordinateSpace.ChunkSpace)
                return false;
            else
            {
                if (sign >= 0)
                {
                    return position.X >= 0 && position.X < sizeInCubes &&
                        position.Y >= 0 && position.Y < sizeInCubes &&
                        position.Z >= 0 && position.Z < sizeInCubes;
                }
                else if (sign == -1)
                {
                    return position.X >= 0 && position.X < sizeInCubes &&
                        position.Y > 0 && position.Y <= sizeInCubes &&
                        position.Z >= 0 && position.Z < sizeInCubes;
                }
                else return false;
            }
        }
    }
}
