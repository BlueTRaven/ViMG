using Engine.Common.Entities;
using Engine.Networking;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using ViMG;
using ViMG.ChunkStuff;
using ViMG.Cubes;
using ViMG.IMGUIImpl;

namespace Engine.ChunkStuff
{
    public class CopiedChunkManager
    {
        private static Engine.Logger Logger = Engine.Logger.InitLogger("CopiedChunkManager", true, Engine.Logger.LogLevel.Warn);

        [InlineArray(3 * 3 * 3)]
        public struct CopyChunkArr
        {
            private ushort[]? _elem0;

            public ushort[]? this[int i]
            {
                get => this[i];
                set => this[i] = value;
            }

            // Requires relative chunk coordinates - (0, 0, 0) = center, (-1, -1, -1) = top front left
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ushort[]? Get(ChunkPosition position)
            {
                Util.ThreeDToOneD(new ValuePoint3D(position + new ChunkPosition(1, 1, 1)), new ValuePoint3D(3), out int i);
                return this[i];
            }
        }

        // a copied chunk for actual use
        public struct CopiedChunkData
        {
            //private readonly CopyChunkArr arr;

            private readonly BasicState[]? entities;
            public readonly ChunkPosition ChunkPosition;

            private ICubeGetter chunkManager;

            public readonly int generation;

            public CopiedChunkData(ICubeGetter chunkManager, BasicState[]? entities, ChunkPosition chunkPosition, int generation)
            {
                this.chunkManager = chunkManager;
                //this.arr = arr;
                this.entities = entities;
                this.ChunkPosition = chunkPosition;
                this.generation = generation;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ushort GetId(CubePosition position)
            {
                IMGUIConsole.Assert(position.Coord == CubePosition.CoordinateSpace.ChunkSpace);

                if (chunkManager.IsInBounds(position.InCubeSpace(ChunkPosition)))
                    return chunkManager.GetId(position.InCubeSpace(ChunkPosition));
                else return 0;

                //accessing a different chunk
                //if (position.X < 0 || position.Y < 0 || position.Z < 0 ||
                //position.X >= Chunk.CHUNK_SIZE || position.Y >= Chunk.CHUNK_SIZE || position.Z >= Chunk.CHUNK_SIZE)
                //{
                //    int chx = (int)MathF.Floor(position.X / (float)Chunk.CHUNK_SIZE);
                //    int chy = (int)MathF.Floor(position.Y / (float)Chunk.CHUNK_SIZE);
                //    int chz = (int)MathF.Floor(position.Z / (float)Chunk.CHUNK_SIZE);

                    //int cx = (position.X + Chunk.CHUNK_SIZE) % Chunk.CHUNK_SIZE;
                    //int cy = (position.Y + Chunk.CHUNK_SIZE) % Chunk.CHUNK_SIZE;
                    //int cz = (position.Z + Chunk.CHUNK_SIZE) % Chunk.CHUNK_SIZE;
                    //Util.ThreeDToOneD(new ValuePoint3D(cx, cy, cz), new ValuePoint3D(Chunk.CHUNK_SIZE), out int i);
                    //var chunkData = arr.Get(new ChunkPosition(chx, chy, chz));
                    //if (chunkData == null) return 0;
                    
                    //var val = chunkData[i];
                    //return val;
                //}
                //else
                //{
                    //return chunkManager.GetId(position.InCubeSpace(ChunkPosition));
                    //Util.ThreeDToOneD(new ValuePoint3D(position), new ValuePoint3D(Chunk.CHUNK_SIZE), out int i);
                    //return arr.Get(new ChunkPosition(0, 0, 0))?[i] ?? 0;
                //}
            }

            public void GetIds(Span<CubePosition> positions, Span<ushort> ids)
            {
                Debug.Assert(positions.Length == ids.Length);
                for (int i = 0; i < positions.Length; i++)
                {
                    ids[i] = GetId(positions[i]);
                }
            }

            public ushort[] GetAllIds()
            {
                var arr = new ushort[Chunk.NUM_CUBES_IN_CHUNK];
                chunkManager.GetIdsForChunk(ChunkPosition, arr.AsSpan());
                return arr;
                //return arr.Get(new ChunkPosition());
            }

            public Optional<Cube> GetCube(CubePosition position)
            {
                //Add one since padding is -1
                var id = GetId(position);
                return new Optional<Cube>(GlobalState.Registry.CubeRegistry.Get(id));
            }

            public MeshHelper.CubeFace GetFace(CubePosition position)
            {
                //using var zone = TracyImpl.Tracy.BeginZone();

                Cube cube = GetCube(position).GetOrDefault(GlobalState.Registry.CubeRegistry.Air);

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

            private bool HasClearSide(int x, int y, int z, Cube currentCube)
            {
                Cube adjacentCube = GetCube(new CubePosition(x, y, z, CubePosition.CoordinateSpace.ChunkSpace)).GetOrDefault(GlobalState.Registry.CubeRegistry.Air);

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
                //using var zone = ViMG.TracyImpl.Tracy.BeginZone();

                Debug.Assert(positions.Length == faces.Length);
                for (int i = 0; i < positions.Length; i++)
                {
                    faces[i] = GetFace(positions[i]);
                }
            }

            public BasicState GetEntity(CubePosition position)
            {
                if (entities != null)
                {
                    Util.ThreeDToOneD(new ValuePoint3D(position), new ValuePoint3D(Chunk.CHUNK_SIZE), out int i);
                    return entities[i];
                }
                else return new();
            }
        }

        private class CopiedChunk
        {
            public ChunkPosition chunkPosition;
            public int generation;
            public int currentGeneration;
            public required BasicState[]? trackers;
        }

        private struct CopyTaskParams
        {
            public required ICubeGetter view;
            public required ChunkManagerIO chunkIO;
            public required CubeTrackers cubeTrackers;
            public required IGetEntity getEntity;
            public required ChunkPosition chunkPosition;
        }

        private struct CopyMultiTaskParams
        {
            public required ICubeGetter view;
            public required ChunkManagerIO chunkIO;
            public required CubeTrackers cubeTrackers;
            public required IGetEntity getEntity;
            public required ChunkPosition[] chunkPositions;
        }

        private struct CopyTaskResult
        {
            public ChunkPosition position;
            //public ushort[] data;
            public BasicState[]? trackers;
        }

        private struct CopyMultiTaskResult
        {
            public CopyTaskResult[] results;
        }

        [ConsoleCommandVar("chunk_max_cached", "maximum number of cached chunks. Higher numbers = faster chunk meshing, increased memory consumption.\n" +
            "Setting this number too low may not work and it will automatically be reset to a higher number.")]
        public static int MaxCachedChunks = 100;

        public readonly ICubeGetter cubeView;
        public readonly ChunkManagerIO chunkIO;
        private readonly CubeTrackers cubeTrackers;
        private readonly int sizeInChunks;
        private readonly Dictionary<ChunkPosition, CopiedChunk> copiedChunks = [];
        private readonly List<Task<CopyTaskResult>> tasks = [];

        //private ChunkPosition?[] oldChunkPositions;
        //private int oldChunkPositionsHead = 0;

        public CopiedChunkManager(ICubeGetter cubeView, ChunkManagerIO chunkIO, CubeTrackers cubeTrackers, int sizeInChunks)
        {
            this.cubeView = cubeView;
            this.chunkIO = chunkIO;
            this.cubeTrackers = cubeTrackers;
            this.sizeInChunks = sizeInChunks;

            //oldChunkPositions = new ChunkPosition?[MaxCachedChunks];
            //Array.Fill(oldChunkPositions, null);
        }

        public void StartCopyChunk(ChunkPosition chunkPosition, IGetEntity getEntity)
        {
            for (int i = 0; i < 3 * 3 * 3;  i++)
            {
                Util.OneDToThreeD(i, new ValuePoint3D(3), out var point);
                var realPos = chunkPosition + new ChunkPosition(point.x - 1, point.y - 1, point.z - 1);
                
                if (IsInWorldBounds(realPos))
                {
                    ActuallyStartCopyChunk(realPos, getEntity);
                }
            }
        }

        private bool IsInWorldBounds(ChunkPosition position)
        {
            return position.X >= 0 && position.X < sizeInChunks &&
                    position.Y >= 0 && position.Y < sizeInChunks &&
                    position.Z >= 0 && position.Z < sizeInChunks;
        }

        private void ActuallyStartCopyChunk(ChunkPosition chunkPosition, IGetEntity getEntity)
        {
            bool forceCopy = false;
            if (!copiedChunks.TryGetValue(chunkPosition, out var chunk))
            {
                forceCopy = true;
            }

            if (forceCopy || chunk.currentGeneration != chunk.generation)
            {
                copiedChunks[chunkPosition] = new CopiedChunk()
                {
                    chunkPosition = chunkPosition,
                    currentGeneration = chunk?.generation ?? 0,
                    generation = chunk?.generation ?? 0,
                    trackers = null,
                };

                var state = new CopyTaskParams
                {
                    view = cubeView,
                    chunkIO = chunkIO,
                    getEntity = getEntity,
                    cubeTrackers = cubeTrackers,
                    chunkPosition = chunkPosition,
                };
                var task = new Task<CopyTaskResult>(CopyChunk, state);
                tasks.Add(task);
            }
        }

        public void FinishCopyChunks()
        {
            if (MaxCachedChunks < tasks.Count)
            {
                Logger.Log(Logger.LogLevel.Info, "Didn't have enough room to copy all chunks - some would be evicted before we could make use of them! {0} / {1}\n" +
                    "MaxCachedChunks has been set to {1}.", MaxCachedChunks, tasks.Count);
                MaxCachedChunks = tasks.Count;
            }

            //if (oldChunkPositions.Length != MaxCachedChunks)
            //{
            //    Array.Resize(ref oldChunkPositions, MaxCachedChunks);
            //}

            foreach (var task in tasks)
            { 
                if (GlobalState.MULTITHREAD_MESHING)
                    task.Start();
                else task.RunSynchronously();
            }

            foreach (var task in tasks)
            {
                task.Wait();

                //if (oldChunkPositions[oldChunkPositionsHead] != null)
                //{
                //    var oldPosition = copiedChunks[oldChunkPositions[oldChunkPositionsHead].Value];
                //    oldPosition = new CopiedChunk()
                //    {
                //        chunkPosition = oldPosition.chunkPosition,
                //        currentGeneration = oldPosition.generation,
                //        generation = oldPosition.generation + 1,
                //        data = null,
                //    };
                //}
                //oldChunkPositions[oldChunkPositionsHead] = task.Result.position;
                //oldChunkPositionsHead += 1;
                //oldChunkPositionsHead %= MaxCachedChunks;

                //copiedChunks[task.Result.position].data = task.Result.data;
                copiedChunks[task.Result.position].trackers = task.Result.trackers;
            }

            tasks.Clear();
        }

        public CopiedChunkData GetCopy(ChunkPosition position)
        {
            if (IsInWorldBounds(position))
            {
                return new CopiedChunkData(cubeView, copiedChunks[position].trackers, position, copiedChunks[position].generation);
                //CopyChunkArr arr = new();
                //for (int i = 0; i < 3 * 3 * 3; i++)
                //{
                //    Util.OneDToThreeD(i, new ValuePoint3D(3), out var point);
                //    var realPos = position + new ChunkPosition(point.x - 1, point.y - 1, point.z - 1);
                //    //var realPos = position + chunkAdjacents[i];
                //    if (IsInWorldBounds(realPos))
                //    {
                //        Debug.Assert(copiedChunks[realPos].currentGeneration == copiedChunks[realPos].generation, "Generation mismatch. Make sure to call StartCopyChunk and FinishCopyChunks.");
                //        arr[i] = copiedChunks[realPos].data!;
                //    }
                //    else arr[i] = null;
                //}

                //return new CopiedChunkData(cubeView, arr, copiedChunks[position].trackers, position, copiedChunks[position].generation);
            }

            return new CopiedChunkData();
        }

        private CopyTaskResult CopyChunk(object? state)
        {
            using var zone = ViMG.TracyImpl.Tracy.BeginZone();

            CopyTaskParams args = (CopyTaskParams)state!;

            //ushort[] ids = new ushort[Chunk.NUM_CUBES_IN_CHUNK];
            //args.view.GetIdsForChunk(args.chunkPosition, ids);
            BasicState[]? trackers = null;

            var worldTrackers = args.cubeTrackers.Get(args.chunkPosition).cubeTrackers;
            if (worldTrackers != null) {
                // Might need some sort of interface that allows us to take EntityReference -> return BasicState
                // This needs to be an interface because this will be used on both client/server
                trackers = new BasicState[Chunk.NUM_CUBES_IN_CHUNK];
                for (int i = 0; i < trackers.Length; i++)
                {
                    trackers[i] = args.getEntity.GetByRef(ref worldTrackers[i]);
                }
            }

            return new CopyTaskResult
            {
                position = args.chunkPosition,
                //data = null,
                trackers = trackers,
            };
        }

        //private CopyTaskResult CopyChunkMulti(object? state)
        //{
        //    using var zone = ViMG.TracyImpl.Tracy.BeginZone();

        //    CopyTaskParams args = (CopyTaskParams)state!;

        //    args.view.GetIdsForChunk(args.chunkPosition, args.data);
        //    BasicState[]? trackers = null;

        //    var worldTrackers = args.cubeTrackers.Get(args.chunkPosition).cubeTrackers;
        //    if (worldTrackers != null)
        //    {
        //        // Might need some sort of interface that allows us to take EntityReference -> return BasicState
        //        // This needs to be an interface because this will be used on both client/server
        //        trackers = new BasicState[Chunk.NUM_CUBES_IN_CHUNK];
        //        for (int i = 0; i < trackers.Length; i++)
        //        {
        //            trackers[i] = args.getEntity.GetByRef(ref worldTrackers[i]);
        //        }
        //    }

        //    return new CopyTaskResult
        //    {
        //        position = args.chunkPosition,
        //        data = args.data,
        //        trackers = trackers,
        //    };
        //}

        public void MarkAllDirty() 
        {
            foreach (var chunk in copiedChunks.Values)
            {
                chunk.generation += 1;
            }
        }

        public void MarkDirty(ChunkPosition chunkPosition)
        {
            if (copiedChunks.TryGetValue(chunkPosition, out var chunk))
            {
                chunk.generation += 1;
            }
        }
    }
}
