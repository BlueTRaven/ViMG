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

        //[StructLayout(LayoutKind.Sequential, Pack = 1)]
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
        public readonly ChunkMesher Mesher;

        private CubeMeshInfo[] cubeMeshInfos;
        private Queue<CubeUpdated> updatedCubePositions = new Queue<CubeUpdated>();

        public bool LockSet;    //If true, a lock on the manager must first be obtained before setting a cube.
        public bool LockGet;    //If true, a lock on the manager must first be obtained before getting a cube.

        public ChunkManager2(int sizeInChunksXZ, ChunkManagerIO io, GraphicsDevice device)
        {
            this.SizeInChunksXZ = sizeInChunksXZ;
            this.SizeInCubes = sizeInChunksXZ * Chunk.CHUNK_SIZE;
            this.io = io;

            cubeMeshInfos = new CubeMeshInfo[sizeInChunksXZ * sizeInChunksXZ * sizeInChunksXZ * Chunk.NUM_CUBES_IN_CHUNK];

            Array.Fill(cubeMeshInfos, new CubeMeshInfo(MeshHelper.CubeFace.NONE));

            Mesher = new ChunkMesher(device, sizeInChunksXZ);

            int size = Marshal.SizeOf<CubeMeshInfo>();
        }

        //Update queue of chunks to mesh
        public void Update(World world, ChunkLoadManager loadManager)
        {
            Mesher.Update(world, this);

            const int MAX_UPDATE_PER_FRAME = 20;
            int updatedThisFrame = 0; 

            //Notify anyone who might want to know that a cube was updated. This includes adjacents.
            while (updatedCubePositions.Count > 0 && updatedThisFrame < MAX_UPDATE_PER_FRAME)
            {
                CubeUpdated updated = updatedCubePositions.Dequeue();

                if (updated.notified == updated.updated)
                {
                    world.OnCubeUpdate(updated.updated, updated.newId);
                    world.EntityManager.GetEntityTrackingPosition(updated.updated).GetOrDefault(null)?.TrackingCubeUpdated(world, this, updated.newId);
                }
                else GetCube(updated.notified).GetOrDefault(Main.Registry.CubeRegistry.Air).OnAdjacentUpdated(world, this, updated.notified, updated.updated, updated.newId);

                updatedThisFrame++;
            }
        }

        public void Unload(ChunkPosition pos)
        {
            Mesher.UnloadMesh(pos);
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

        public void MarkChunkDirty(ChunkPosition position)
        {
            Mesher.MarkDirty(position);
        }

        public ChunkMesh GetMesh(ChunkPosition position, Cube.RenderPass pass)
        {
            return Mesher.GetMesh(position, pass);
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

        public MeshHelper.CubeFace GetFaces(CubePosition position)
        {
            return GetClearSides(position);
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

                    MarkChunkDirty(ChunkPosition.CubeChunk(adjacentPosition));

                    updatedCubePositions.Enqueue(new CubeUpdated(position, adjacentPosition, oldId, updatedId));
                }
            }
        }

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
                //MarkCubeMeshInfoDirty(position, oldId, id);
                GetCubeMeshInfo(position).version++;
                MarkChunkDirty(chunkPos);

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

        public void Dispose()
        {
            Mesher.UnloadAllMeshes();
            //UnloadAllMeshes();

            cubeMeshInfos = null;
            //chunkMeshInfos = null;
        }
    }
}
