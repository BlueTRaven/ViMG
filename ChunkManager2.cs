using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ViMG.Cubes;

namespace ViMG
{
    //Try to stay away from dependance on World if possible
    public class ChunkManager2 : IDisposable
    {
        private readonly struct CubeUpdated
        {
            public readonly CubePosition updated;
            public readonly CubePosition notified;
            public readonly ushort oldId;
            public readonly ushort newId;

            public CubeUpdated(CubePosition updated, CubePosition notified, ushort oldId, ushort newId)
            {
                this.updated = updated;
                this.notified = notified;
                this.oldId = oldId;
                this.newId = newId;
            }
        }

        private readonly struct ChunkMeshTaskState
        {
            public readonly ChunkMeshInfo cmi;
            public readonly ChunkManager2 manager;

            public ChunkMeshTaskState(ChunkMeshInfo cmi, ChunkManager2 manager)
            {
                this.cmi = cmi;
                this.manager = manager;
            }
        }

        private readonly struct ChunkMeshResult
        {
            public readonly ChunkPosition position;
            public readonly ChunkMesh chunkMesh;
            public readonly int meshedVersion;
            public readonly Cube.RenderPass pass;

            public ChunkMeshResult(ChunkPosition position, ChunkMesh mesh, int version, Cube.RenderPass pass)
            {
                this.position = position;
                this.chunkMesh = mesh;
                this.meshedVersion = version;
                this.pass = pass;
            }
        }

        private struct CubeMeshInfo
        {
            public MeshHelper.CubeFace faces;
            public byte meshVersion;
            public byte version;

            public CubeMeshInfo(MeshHelper.CubeFace faces)
            {
                this.faces = faces;

                meshVersion = 0;
                version = 1;
            }
        }

        private struct ChunkMeshInfo
        {
            public ChunkPosition position;
            public ChunkMesh[] meshes;
            public byte meshVersion; //mesh version; if different from version, needs to be re-meshed
            public byte version;

            public ChunkMeshInfo(ChunkPosition position)
            {
                this.position = position;
                meshes = new ChunkMesh[NUM_CHUNK_MESH_PASSES];
                meshVersion = 0;
                version = 1;
            }

            public int GetMeshVersionCode()
            {
                return meshes.GetHashCode() + version;
            }
        };

        private static ChunkPosition[] chunkAdjacents = new ChunkPosition[6]
        {
            new ChunkPosition(-1, 0, 0),
            new ChunkPosition(1, 0, 0),
            new ChunkPosition(0, -1, 0),
            new ChunkPosition(0, 1, 0),
            new ChunkPosition(0, 0, -1),
            new ChunkPosition(0, 0, 1),
        };


        private static CubePosition[] cubeAdjacents = new CubePosition[6]
        {
            new CubePosition(-1, 0, 0),
            new CubePosition(1, 0, 0),
            new CubePosition(0, -1, 0),
            new CubePosition(0, 1, 0),
            new CubePosition(0, 0, -1),
            new CubePosition(0, 0, 1),
        };

        public const int NUM_CHUNK_MESH_PASSES = 5;
        private const int SIZEOF_CHUNK = (sizeof(ushort) * Chunk.CHUNK_SIZE * Chunk.CHUNK_SIZE * Chunk.CHUNK_SIZE);

        public readonly int SizeInChunksXZ;
        public readonly int SizeInCubes;
        private readonly ChunkManagerIO io;
        private readonly ChunkMesher mesher;

        private CubeMeshInfo[] cubeMeshInfos;
        private ChunkMeshInfo[] chunkMeshInfos;

        private Queue<Task<ChunkMeshResult>> meshResults = new Queue<Task<ChunkMeshResult>>();
        private Queue<Task<ChunkMeshResult>> incompleteMeshResults = new Queue<Task<ChunkMeshResult>>();

        private Queue<ChunkPosition> updatedChunkPositions = new Queue<ChunkPosition>();
        private HashSet<ChunkPosition> positionsInQueue = new HashSet<ChunkPosition>();
        private Queue<CubeUpdated> updatedCubePositions = new Queue<CubeUpdated>();

        public ChunkManager2(int sizeInChunksXZ, ChunkManagerIO io, GraphicsDevice device)
        {
            this.SizeInChunksXZ = sizeInChunksXZ;
            this.SizeInCubes = sizeInChunksXZ * Chunk.CHUNK_SIZE;
            this.io = io;

            chunkMeshInfos = new ChunkMeshInfo[sizeInChunksXZ * sizeInChunksXZ * sizeInChunksXZ];

            cubeMeshInfos = new CubeMeshInfo[chunkMeshInfos.Length * Chunk.NUM_CUBES_IN_CHUNK];

            for (int i = 0; i < sizeInChunksXZ * sizeInChunksXZ * sizeInChunksXZ; i++)
            {
                Util.OneDToThreeD(i, new ValuePoint3D(sizeInChunksXZ), out ValuePoint3D point);
                chunkMeshInfos[i] = new ChunkMeshInfo(new ChunkPosition(point.x, point.y, point.z));
            }

            Array.Fill(cubeMeshInfos, new CubeMeshInfo(MeshHelper.CubeFace.NONE));

            mesher = new ChunkMesher(device);
        }

        //Update queue of chunks to mesh
        public void Update(World world, ChunkLoadManager loadManager)
        {
            const int MAX_MESH_PER_FRAME = 20;
            int meshedInThisFrame = 0;

            while (updatedChunkPositions.Count > 0 && meshedInThisFrame < MAX_MESH_PER_FRAME)
            {
                ChunkPosition position = updatedChunkPositions.Dequeue();
                positionsInQueue.Remove(position);

                ref ChunkMeshInfo c = ref GetChunkMeshInfo(position);

                if (c.version != c.meshVersion)
                {
                    c.meshVersion = c.version;

                    MeshChunk(world, ref c);

                    meshedInThisFrame++;
                }
            }

            const int MAX_UPDATE_PER_FRAME = 20;
            int updatedThisFrame = 0; 

            while (updatedCubePositions.Count > 0 && updatedThisFrame < MAX_UPDATE_PER_FRAME)
            {
                CubeUpdated updated = updatedCubePositions.Dequeue();

                if (updated.notified == updated.updated)
                {
                    world.OnCubeUpdate(updated.updated, updated.newId);
                    world.EntityManager.GetEntityTrackingPosition(updated.updated).GetOrDefault(null)?.TrackingCubeUpdated(world, this, updated.newId);
                }
                else
                    GetCube(updated.notified).GetOrDefault(Main.Registry.CubeRegistry.Air).OnAdjacentUpdated(world, this, updated.notified, updated.updated, updated.newId);

                updatedThisFrame++;
            }

            int numMeshResultsToTryThisFrame = meshResults.Count;

            while (meshResults.Count > 0 && numMeshResultsToTryThisFrame > 0)
            {
                var task = meshResults.Dequeue();

                if (task.IsCompleted)
                {
                    if (!task.IsCompletedSuccessfully)
                        throw new Exception("???");

                    var meshResult = task.Result;

                    ref ChunkMeshInfo c = ref GetChunkMeshInfo(meshResult.position);

                    if (meshResult.meshedVersion == c.version)
                    {
                        //Unload the old mesh now
                        UnloadMesh(ref c);

                        c.meshVersion = c.version;

                        if (!meshResult.chunkMesh.IsEmpty && meshResult.chunkMesh.IBO.GraphicsDevice == null)
                            throw new Exception("???");

                        c.meshes[(int)meshResult.pass] = meshResult.chunkMesh;
                    }
                    else
                    {
                        //version has changed while we're meshing - discard the old mesh, as a new one should already be queued.
                        if (!meshResult.chunkMesh.IsEmpty && meshResult.chunkMesh.VBO != null)
                        {
                            meshResult.chunkMesh.VBO.Dispose();
                            meshResult.chunkMesh.IBO.Dispose();
                        }
                    }

                    numMeshResultsToTryThisFrame--;
                }
                else meshResults.Enqueue(task);
            }
        }

        public void MeshChunk(World world, ChunkPosition position)
        {
            ref ChunkMeshInfo c = ref GetChunkMeshInfo(position);

            if (c.version != c.meshVersion)
                MeshChunk(world, ref c);
        }

        private void MeshChunk(World world, ref ChunkMeshInfo c)
        {
            //TODO: wrapper task for proper Locking
            Task<ChunkMeshResult> task = new Task<ChunkMeshResult>((object obj) =>
            {
                ChunkMeshTaskState state = (ChunkMeshTaskState)obj;

                ChunkMesh mesh;
                //Prevent setting for the duration
                lock (state.manager)
                {
                    state.manager.LockSet = true;
                    mesh = mesher.GenerateChunk(world, this, state.cmi.position, Cube.RenderPass.Opaque, true);
                    state.manager.LockSet = false;
                }

                if (!mesh.IsEmpty && mesh.IBO.GraphicsDevice == null)
                    throw new Exception("???");
                return new ChunkMeshResult(state.cmi.position, mesh, state.cmi.version, Cube.RenderPass.Opaque);
            }, new ChunkMeshTaskState(c, this));
            task.Start();

            meshResults.Enqueue(task);
            //First one must have forceUpdate = true,
            //but all subsequent mesh generations should be false.
            //c.meshes[(int)Cube.RenderPass.Opaque] = mesher.GenerateChunk(world, this, c.position, Cube.RenderPass.Opaque, true);
            //c.meshes[(int)Cube.RenderPass.Transparent] = mesher.GenerateChunk(world, this, c.position, Cube.RenderPass.Transparent, false);
            //c.meshes[(int)Cube.RenderPass.DepthOnly] = mesher.GenerateChunk(world, this, c.position, Cube.RenderPass.DepthOnly, false);
            //c.meshes[(int)Cube.RenderPass.Fluid] = null;   //TODO fluids?
            //c.meshes[(int)Cube.RenderPass.Air] = mesher.GenerateChunk(world, this,c.position, Cube.RenderPass.Air, false);

            //ProfilingHelper.AddBatch();
        }

        //TODO: separate out visual stuff, not sure how yet
        private MeshHelper.CubeFace GetClearSides(CubePosition position)
        {
            Cube cube = GetCube(position).GetOrDefault(Main.Registry.CubeRegistry.Air);

            if (cube.Transparency == Cube.TransparencyValue.Invisible)
                return MeshHelper.CubeFace.NONE;

            MeshHelper.CubeFace faces = MeshHelper.CubeFace.NONE;

            if (HasClearSide(position.X - 1, position.Y, position.Z, cube))
                faces |= MeshHelper.CubeFace.LEFT;
            if (HasClearSide(position.X + 1, position.Y, position.Z, cube))
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
            CubePosition pos = new CubePosition(x, y, z);
            if (IsInWorldBounds(pos))
            {
                Cube adjacentCube = GetCube(pos).GetOrDefault(Main.Registry.CubeRegistry.Air);

                if (currentCube.Transparency != Cube.TransparencyValue.Air)
                {
                    if (adjacentCube.Transparency == Cube.TransparencyValue.Transparent ||
                        adjacentCube.Transparency == Cube.TransparencyValue.Invisible ||
                        adjacentCube.Transparency == Cube.TransparencyValue.Air)
                        return true;
                    if (adjacentCube.Transparency == Cube.TransparencyValue.TransparentOccludesSiblings)
                    {
                        if (currentCube == adjacentCube)
                            return false;
                        else return true;
                    }
                    else return false;
                }
                else if (currentCube.Transparency == Cube.TransparencyValue.Air)
                {
                    if (currentCube == adjacentCube)
                        return false;
                    else return true;
                }
            }
            
            return false;
        }

        private void UnloadMesh(ref ChunkMeshInfo c)
        {
            for (int i = 0; i < NUM_CHUNK_MESH_PASSES; i++)
            {
                if (c.meshes[i] != null && c.meshes[i] != ChunkMesh.Empty)
                {
                    c.meshes[i].VBO.Dispose();
                    c.meshes[i].IBO.Dispose();
                }
            }
        }

        private ref ChunkMeshInfo GetChunkMeshInfo(ChunkPosition pos)
        {
            Util.ThreeDToOneD(new ValuePoint3D(pos.X, pos.Y, pos.Z), new ValuePoint3D(SizeInChunksXZ), out int i);
            return ref chunkMeshInfos[i];
        }

        public int GetMeshVersionCode(ChunkPosition position)
        {
            return GetChunkMeshInfo(position).GetMeshVersionCode();
        }

        public bool IsInWorldBounds(Vector3 position)
        {
            return IsInWorldBounds(CubePosition.FromWorldSpace(position));
        }

        public bool IsInWorldBounds(CubePosition position)
        {
            int sign = MathF.Sign(position.Y);

            //TODO: layer stuff
            //int layer = LayerFromPos(position);

            //if (layer > discoveredLayers)
                //return false;

            //position = LayerRelativePosition(position);

            if (position.Coord == CubePosition.CoordinateSpace.ChunkSpace)
                return false;
            else
            {
                //positive sign/0 (Sign(0) == 0)
                //Below 0, the y axis has its inclusivity/exclusivity reversed (aka, min exclusive, max inclusive, instead of the opposite).
                //Note that this only applies to the y axis and no other.
                if (sign >= 0)
                {
                    return position.X >= 0 && position.X < SizeInCubes &&
                        position.Y >= 0 && position.Y < SizeInCubes &&
                        position.Z >= 0 && position.Z < SizeInCubes;
                }
                else if (sign == -1)
                {
                    return position.X >= 0 && position.X < SizeInCubes &&
                        position.Y > 0 && position.Y <= SizeInCubes &&
                        position.Z >= 0 && position.Z < SizeInCubes;
                }
                else return false;
            }
        }

        public bool IsInWorldBounds(ChunkPosition position)
        {
            return position.X >= 0 && position.X < SizeInChunksXZ &&
                    position.Y >= 0 && position.Y < SizeInChunksXZ &&
                    position.Z >= 0 && position.Z < SizeInChunksXZ;
        }

        public void MarkDirty(ChunkPosition position)
        {
            GetChunkMeshInfo(position).version++;
        }

        public ChunkMesh GetMesh(ChunkPosition position, Cube.RenderPass pass)
        {
            ChunkMesh mesh = GetChunkMeshInfo(position).meshes[(int)pass];
            if (mesh != null && !mesh.IsEmpty && mesh.IBO.IsDisposed)
                throw new Exception("???");
            return mesh;
        }

        public MeshHelper.CubeFace GetCachedFaces(CubePosition position)
        {
            ref CubeMeshInfo meshInfo = ref GetCubeMeshInfo(position);

            if (meshInfo.version != meshInfo.meshVersion)
            {
                meshInfo.meshVersion = meshInfo.version;

                meshInfo.faces = GetClearSides(position);
            }

            return meshInfo.faces;
        }

        private ref CubeMeshInfo GetCubeMeshInfo(CubePosition position)
        {
            Util.ThreeDToOneD(new ValuePoint3D(position.X, position.Y, position.Z), new ValuePoint3D(SizeInCubes), out int i);
            return ref cubeMeshInfos[i];
        }

        public OptionalValue<CubePosition> GetFirstSolidDown(Vector3 start)
        {
            CubePosition startPos = CubePosition.FromWorldSpace(start);

            for (int y = 0; y < SizeInCubes; y++)
            {
                CubePosition pos = new CubePosition(startPos.X, startPos.Y - y, startPos.Z);
                if (IsInWorldBounds(pos) && GetCubeId(pos) != 0)
                    return new OptionalValue<CubePosition>(pos);
            }

            return new OptionalValue<CubePosition>();
        }

        public OptionalValue<CubePosition> GetFirstSolidDown(CubePosition start)
        {
            for (int y = 0; y < SizeInCubes; y++)
            {
                CubePosition pos = new CubePosition(start.X, start.Y - y, start.Z);

                /*if (pos.Y < 0)
					Console.WriteLine("aaa");*/
                //Null check here is the same as doing out of bounds check.
                Cube cubeAtPos = GetCube(pos).Get();
                if (cubeAtPos != null && (cubeAtPos.Touchable && cubeAtPos.Collision == Cube.CollisionValue.Collidable))
                    return new OptionalValue<CubePosition>(pos);
            }

            return new OptionalValue<CubePosition>();
        }

        private void MarkCubeMeshInfoDirty(CubePosition position, ushort oldId, ushort updatedId)
        {
            GetCubeMeshInfo(position).version++;

            for (int i = 0; i < 6; i++)
            {
                CubePosition adjacentPosition = position + cubeAdjacents[i];

                if (IsInWorldBounds(adjacentPosition))
                {
                    GetCubeMeshInfo(adjacentPosition).version++;

                    GetChunkMeshInfo(ChunkPosition.CubeChunk(adjacentPosition)).version++;

                    ChunkPosition adjacentChunkPos = ChunkPosition.CubeChunk(adjacentPosition);

                    if (!positionsInQueue.Contains(adjacentChunkPos))
                    {
                        updatedChunkPositions.Enqueue(adjacentChunkPos);
                        positionsInQueue.Add(adjacentChunkPos);
                    }

                    updatedCubePositions.Enqueue(new CubeUpdated(position, adjacentPosition, oldId, updatedId));
                }
            }
        }

        public bool LockSet;
        public bool LockGet;
        public void Lock()
        {
            Monitor.Enter(this);
        }

        public void Unlock()
        {
            Monitor.Exit(this);
        }

        //TODO: could probably get rid of position.InChunkSpace call somehow.
        public unsafe void SetCube(CubePosition position, ushort id, bool markDirty = true)
        {
            byte[] bytes = io.GetBytes();

            ChunkPosition chunkPos = ChunkPosition.CubeChunk(position);
            Util.ThreeDToOneD(new ValuePoint3D(chunkPos.X, chunkPos.Y, chunkPos.Z), new ValuePoint3D(SizeInChunksXZ), out int chi);
            int chunkOffset = Chunk.NUM_CUBES_IN_CHUNK * chi;
            CubePosition positionChS = position.InChunkSpace(chunkPos);
            Util.ThreeDToOneD(new ValuePoint3D(positionChS.X, positionChS.Y, positionChS.Z), new ValuePoint3D(Chunk.CHUNK_SIZE), out int ci);
            //int cbi = ci * sizeof(ushort);
            //cbi += chunkOffset;

            ushort oldId;

            if (LockSet)
            {
                lock (this)
                {
                    fixed (byte* bytesRaw = &bytes[0])
                    {
                        ushort* asIds = (ushort*)bytesRaw;

                        oldId = asIds[ci + chunkOffset];
                        asIds[ci + chunkOffset] = id;
                    }
                }
            }
            else
            {
                fixed (byte* bytesRaw = &bytes[0])
                {
                    ushort* asIds = (ushort*)bytesRaw;

                    oldId = asIds[ci + chunkOffset];
                    asIds[ci + chunkOffset] = id;
                }
            }

            /*bytes[cbi++] = (byte)id;
            bytes[cbi++] = (byte)(id >> 8);*/

            if (markDirty)
            {
                MarkCubeMeshInfoDirty(position, oldId, id);
                chunkMeshInfos[chi].version++;

                if (!positionsInQueue.Contains(chunkPos))
                {
                    updatedChunkPositions.Enqueue(chunkPos);
                    positionsInQueue.Add(chunkPos);
                }

                updatedCubePositions.Enqueue(new CubeUpdated(position, position, oldId, id));
            }

        }

        public ushort GetCubeId(CubePosition position)
        {
            byte[] bytes = io.GetBytes();

            //NOTE: we can't just index directly into bytes (as a ushort)
            //This is because we store cube ids weirdly. We do not store them flat, one after another; instead, we store them as a chunk, then as another chunk, etc.
            //This may introduce problems here, but I don't think I want to change that behavior
            //as it may help later down the line of we want to, say, introduce streaming. Streaming individual cubes?
            //Pretty useless. Chunks, however, are a much more useful streamable object.

            //Get chunk position...
            int chx = position.X / Chunk.CHUNK_SIZE;
            int chy = position.Y / Chunk.CHUNK_SIZE;
            int chz = position.Z / Chunk.CHUNK_SIZE;
            //use it to find offset in byte array
            int chunkOffset = chx + SizeInChunksXZ * (chy + SizeInChunksXZ * chz);
            chunkOffset *= Chunk.NUM_CUBES_IN_CHUNK;

            //Get chunk relative cube position...
            //https://stackoverflow.com/questions/11040646/faster-modulus-in-c-c
            //Faster mod when denominator is a power of 2.
            //NOTE: if Chunk.CHUNK_SIZE changes and no longer is a power of two, THIS WILL BREAK EVERYTHING!
            int csx = position.X & (Chunk.CHUNK_SIZE - 1);
            int csy = position.Y & (Chunk.CHUNK_SIZE - 1);
            int csz = position.Z & (Chunk.CHUNK_SIZE - 1);

            int cubeOffset = csx + Chunk.CHUNK_SIZE * (csy + Chunk.CHUNK_SIZE * csz);
            cubeOffset += chunkOffset;

            ushort id;

            if (LockGet)
            {
                lock (this)
                {
                    id = BitConverter.ToUInt16(bytes, cubeOffset * sizeof(ushort));
                }
            }
            else
            {
                id = BitConverter.ToUInt16(bytes, cubeOffset * sizeof(ushort));
            }

            //BitConverter is apparently faster than fixed cast of bytes to ushort
            return id;
            /*fixed (byte* bytesRaw = &bytes[0])
            {
                ushort* asIds = (ushort*)bytesRaw;
                return asIds[ci];
            }*/
        }

        public Optional<Cube> GetCube(Vector3 position)
        {
            return GetCube(CubePosition.FromWorldSpace(position));
        }

        //Really minor cache speedup
        private int cachedId;
        private Cube cachedCube;
        public Optional<Cube> GetCube(CubePosition position)
        {
            if (!IsInWorldBounds(position))
                return new Optional<Cube>();

            ushort id = GetCubeId(position);

            Cube cube;
            if (id == cachedId)
                cube = cachedCube;
            else
            {
                cachedCube = Main.Registry.CubeRegistry.Get(id);
                cube = cachedCube;
                cachedId = id;
            }

            return new Optional<Cube>(cube);
        }

        public void UnloadMesh(ChunkPosition position)
        {
            ref ChunkMeshInfo c = ref GetChunkMeshInfo(position);

            for (int i = 0; i < NUM_CHUNK_MESH_PASSES; i++)
            {
                if (c.meshes[i] != null && c.meshes[i] != ChunkMesh.Empty)
                {
                    c.meshes[i].VBO.Dispose();
                    c.meshes[i].IBO.Dispose();

                    c.meshes[i] = null;
                }
            }

            c.version++;
        }

        public void UnloadAllMeshes()
        {
            for (int j = 0; j < SizeInChunksXZ * SizeInChunksXZ * SizeInChunksXZ; j++)
            {
                for (int k = 0; k < NUM_CHUNK_MESH_PASSES; k++)
                {
                    ChunkMesh mesh = chunkMeshInfos[j].meshes[k];
                    if (mesh != null && mesh != ChunkMesh.Empty)
                    {
                        mesh.VBO.Dispose();
                        mesh.IBO.Dispose();

                        chunkMeshInfos[j].meshes[k] = null;
                    }
                }
            }
        }

        public void Dispose()
        {
            UnloadAllMeshes();

            cubeMeshInfos = null;
            chunkMeshInfos = null;
        }
    }
}
