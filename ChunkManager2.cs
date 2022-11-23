using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Cubes;

namespace ViMG
{
    //Try to stay away from dependance on World if possible
    public class ChunkManager2
    {
        private struct C
        {
            public ChunkPosition position;
            public ChunkMesh[] meshes;
            public int meshVersion; //mesh version; if different from version, needs to be re-meshed
            public int version;

            public C(ChunkPosition position)
            {
                this.position = position;
                meshes = new ChunkMesh[NUM_CHUNK_MESH_PASSES];
                meshVersion = -1;
                version = 0;
            }

            public int GetMeshVersionCode()
            {
                return meshes.GetHashCode() + version;
            }
        };

        private static ChunkPosition[] adjacents = new ChunkPosition[6]
        {
            new ChunkPosition(-1, 0, 0),
            new ChunkPosition(1, 0, 0),
            new ChunkPosition(0, -1, 0),
            new ChunkPosition(0, 1, 0),
            new ChunkPosition(0, 0, -1),
            new ChunkPosition(0, 0, 1),
        };

        public const int NUM_CHUNK_MESH_PASSES = 5;
        private const int SIZEOF_CHUNK = (sizeof(ushort) * Chunk.CHUNK_SIZE * Chunk.CHUNK_SIZE * Chunk.CHUNK_SIZE);

        public readonly int SizeInChunksXZ;
        public readonly int SizeInCubes;
        private readonly ChunkManagerIO io;
        private readonly ChunkMesher mesher;

        private C[] chunks;

        public ChunkManager2(int sizeInChunksXZ, ChunkManagerIO io, GraphicsDevice device)
        {
            this.SizeInChunksXZ = sizeInChunksXZ;
            this.SizeInCubes = sizeInChunksXZ * Chunk.CHUNK_SIZE;
            this.io = io;

            chunks = new C[sizeInChunksXZ * sizeInChunksXZ * sizeInChunksXZ];

            for (int i = 0; i < sizeInChunksXZ * sizeInChunksXZ * sizeInChunksXZ; i++)
            {
                Util.OneDToThreeD(i, new ValuePoint3D(sizeInChunksXZ), out ValuePoint3D point);
                chunks[i] = new C(new ChunkPosition(point.x, point.y, point.z));
            }

            mesher = new ChunkMesher(device);
        }

        //Update queue of chunks to mesh
        public void Update(World world, ChunkLoadManager loadManager)
        {
            IEnumerable<ChunkPosition> loadedPositions = loadManager.GetLoaded();
            if (loadedPositions != null && loadedPositions.Count() > 0)
            {
                foreach (ChunkPosition pos in loadedPositions)
                {
                    ref C c = ref GetC(pos);

                    if (c.version != c.meshVersion)
                    {
                        for (int i = 0; i < 6; i++)
                        {
                            ChunkPosition adj = pos + adjacents[i];

                            if (loadManager.IsLoaded(adj))
                                MeshChunk(world, ref c);
                        }
                    }
                }
            }
        }

        private void MeshChunk(World world, ref C c)
        {
            ProfilingHelper.Start("Meshing chunk at x {0} y {1} z {2}...", c.position.X, c.position.Y, c.position.Z);
            c.meshVersion = c.version;

            UnloadMesh(ref c);

            //First one must have forceUpdate = true,
            //but all subsequent mesh generations should be false.
            c.meshes[(int)Cube.RenderPass.Opaque] = mesher.GenerateChunk(world, this, c.position, Cube.RenderPass.Opaque, true);
            c.meshes[(int)Cube.RenderPass.Transparent] = mesher.GenerateChunk(world, this, c.position, Cube.RenderPass.Transparent, false);
            c.meshes[(int)Cube.RenderPass.DepthOnly] = mesher.GenerateChunk(world, this, c.position, Cube.RenderPass.DepthOnly, false);
            c.meshes[(int)Cube.RenderPass.Fluid] = null;   //TODO fluids?
            c.meshes[(int)Cube.RenderPass.Air] = mesher.GenerateChunk(world, this,c.position, Cube.RenderPass.Air, false);

            ProfilingHelper.End("Done.");
        }

        //TODO: separate out visual stuff, not sure how yet
        public MeshHelper.CubeFace GetClearSides(CubePosition position, World world)
        {
            Cube cube = GetCube(position).GetOrDefault(Main.Registry.CubeRegistry.Air);

            if (cube.Transparency == Cube.TransparencyValue.Invisible)
                return MeshHelper.CubeFace.NONE;

            MeshHelper.CubeFace faces = MeshHelper.CubeFace.NONE;

            if (HasClearSide(position.X - 1, position.Y, position.Z, cube, world))
                faces |= MeshHelper.CubeFace.LEFT;
            if (HasClearSide(position.X + 1, position.Y, position.Z, cube, world))
                faces |= MeshHelper.CubeFace.RIGHT;

            if (HasClearSide(position.X, position.Y - 1, position.Z, cube, world))
                faces |= MeshHelper.CubeFace.DOWN;
            if (HasClearSide(position.X, position.Y + 1, position.Z, cube, world))
                faces |= MeshHelper.CubeFace.UP;

            if (HasClearSide(position.X, position.Y, position.Z - 1, cube, world))
                faces |= MeshHelper.CubeFace.FRONT;
            if (HasClearSide(position.X, position.Y, position.Z + 1, cube, world))
                faces |= MeshHelper.CubeFace.BACK;

            return faces;
        }

        //TODO: separate out visual stuff, not sure how yet
        private bool HasClearSide(int x, int y, int z, Cube currentCube, World world)
        {
            Cube adjacentCube = GetCube(new CubePosition(x, y, z)).GetOrDefault(Main.Registry.CubeRegistry.Air);

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
            else return false;
        }

        private void UnloadMesh(ref C c)
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

        private ref C GetC(ChunkPosition pos)
        {
            Util.ThreeDToOneD(new ValuePoint3D(pos.X, pos.Y, pos.Z), new ValuePoint3D(SizeInChunksXZ), out int i);
            return ref chunks[i];
        }

        public int GetMeshVersionCode(ChunkPosition position)
        {
            return GetC(position).GetMeshVersionCode();
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
            GetC(position).version++;
        }

        public ChunkMesh GetMesh(ChunkPosition position, Cube.RenderPass pass)
        {
            return GetC(position).meshes[(int)pass];
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

        //TODO: could probably get rid of position.InChunkSpace call somehow.
        public void SetCube(CubePosition position, ushort id)
        {
            byte[] bytes = io.GetBytes();

            ChunkPosition chunkPos = ChunkPosition.CubeChunk(position);
            Util.ThreeDToOneD(new ValuePoint3D(chunkPos.X, chunkPos.Y, chunkPos.Z), new ValuePoint3D(SizeInChunksXZ), out int chi);
            int chunkOffset = SIZEOF_CHUNK * chi;
            CubePosition positionChS = position.InChunkSpace(chunkPos);
            Util.ThreeDToOneD(new ValuePoint3D(positionChS.X, positionChS.Y, positionChS.Z), new ValuePoint3D(Chunk.CHUNK_SIZE), out int ci);
            ci *= sizeof(ushort);
            ci += chunkOffset;

            bytes[ci++] = (byte)id;
            bytes[ci++] = (byte)(id >> 8);

            chunks[chi].version++;
        }

        //TODO: could probably get rid of position.InChunkSpace call somehow.
        public ushort GetCubeId(CubePosition position)
        {
            byte[] bytes = io.GetBytes();

            ChunkPosition chunkPos = ChunkPosition.CubeChunk(position);
            Util.ThreeDToOneD(new ValuePoint3D(chunkPos.X, chunkPos.Y, chunkPos.Z), new ValuePoint3D(SizeInChunksXZ), out int chi);
            int chunkOffset = SIZEOF_CHUNK * chi;
            CubePosition positionChS = position.InChunkSpace(chunkPos);
            Util.ThreeDToOneD(new ValuePoint3D(positionChS.X, positionChS.Y, positionChS.Z), new ValuePoint3D(Chunk.CHUNK_SIZE), out int ci);
            ci *= sizeof(ushort);
            ci += chunkOffset;

            byte a = bytes[ci++];
            byte b = bytes[ci++];

            //bitwise operators are not defined for ushort, so we're forced to cast... fun.
            int id = 0;
            id |= b << 8;
            id |= a << 0;

            return (ushort)id;
        }

        public Optional<Cube> GetCube(Vector3 position)
        {
            return GetCube(CubePosition.FromWorldSpace(position));
        }

        public Optional<Cube> GetCube(CubePosition position)
        {
            ushort id = GetCubeId(position);

            Cube cube = Main.Registry.CubeRegistry.Get(id);

            return new Optional<Cube>(cube);
        }

        public void UnloadMesh(ChunkPosition position)
        {
            ref C c = ref GetC(position);

            for (int i = 0; i < NUM_CHUNK_MESH_PASSES; i++)
            {
                if (c.meshes[i] != null && c.meshes[i] != ChunkMesh.Empty)
                {
                    c.meshes[i].VBO.Dispose();
                    c.meshes[i].IBO.Dispose();

                    c.meshes[i] = null;
                }
            }
        }

        public void UnloadAllMeshes()
        {
            for (int j = 0; j < SizeInChunksXZ * SizeInChunksXZ * SizeInChunksXZ; j++)
            {
                for (int k = 0; k < NUM_CHUNK_MESH_PASSES; k++)
                {
                    ChunkMesh mesh = chunks[j].meshes[k];
                    if (mesh != null && mesh != ChunkMesh.Empty)
                    {
                        mesh.VBO.Dispose();
                        mesh.IBO.Dispose();

                        chunks[j].meshes[k] = null;
                    }
                }
            }
        }
    }
}
