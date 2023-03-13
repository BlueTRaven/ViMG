using BepuPhysics;
using BepuPhysics.Collidables;
using BepuUtilities.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.ChunkStuff;
using ViMG.Cubes;

namespace ViMG
{
    public class ChunkCollisionMesher
    {
        private struct CollisionMeshInfo
        {
            public TypedIndex collidableShapeIndex; //TODO move elsewhere
            public StaticHandle collidableStaticHandle;
            public Mesh collidableMesh;

            public byte version;
            public byte meshVersion;
            public bool hasMesh;
        }

        private CollisionMeshInfo[] meshes;
        private Queue<ChunkPosition> dirtyChunkPositions = new Queue<ChunkPosition>();
        private HashSet<ChunkPosition> dirtyChunkKnown = new HashSet<ChunkPosition>();

        private readonly ChunkMesher mesher;
        private readonly int sizeInChunks;

        private readonly Physics.PhysicsInfo physicsInfo;

        public ChunkCollisionMesher(Physics.PhysicsInfo physicsInfo, ChunkMesher mesher, int sizeInChunks)
        {
            this.physicsInfo = physicsInfo;
            meshes = new CollisionMeshInfo[sizeInChunks * sizeInChunks * sizeInChunks];
            this.mesher = mesher;
            this.sizeInChunks = sizeInChunks;
        }

        public void Update(World world)
        {
            int currentNum = 0;
            while (dirtyChunkPositions.Count > 0 && currentNum < 4)
            {
                ChunkPosition position = dirtyChunkPositions.Dequeue();

                if (dirtyChunkKnown.Contains(position))
                {
                    dirtyChunkKnown.Remove(position);

                    ref CollisionMeshInfo c = ref GetChunkMeshInfo(position);

                    if (c.version != c.meshVersion || !c.hasMesh)
                    {
                        if (c.hasMesh)
                            Unload(world, position);
                        MeshChunk(world, position);
                    }
                }

                currentNum++;
            }
        }

        public void Flush(World world)
        {
            int max = dirtyChunkPositions.Count;
            world.GameStateManager.TheIsland.ProgressMax = max;

            while (dirtyChunkPositions.Count > 0)
            {
                ChunkPosition position = dirtyChunkPositions.Dequeue();

                if (dirtyChunkKnown.Contains(position))
                {
                    world.GameStateManager.TheIsland.ProgressMin = max - dirtyChunkKnown.Count;

                    dirtyChunkKnown.Remove(position);

                    ref CollisionMeshInfo c = ref GetChunkMeshInfo(position);

                    if (c.version != c.meshVersion || !c.hasMesh)
                    {
                        if (c.hasMesh)
                            Unload(world, position);
                        MeshChunk(world, position);
                    }
                }
            }
        }

        private CopiedChunkData MakeCopy(World world, ChunkPosition position)
        {
            CubePosition basePosition = position.InCubeSpace();
            CopiedChunkData copied = new CopiedChunkData(basePosition);

            Span<CubePosition> queryPositions = stackalloc CubePosition[CopiedChunkData.SIZE];
            Span<ushort> resultIds = stackalloc ushort[CopiedChunkData.SIZE];

            for (int x = -1; x <= Chunk.CHUNK_SIZE; x++)
            {
                for (int y = -1; y <= Chunk.CHUNK_SIZE; y++)
                {
                    for (int z = -1; z <= Chunk.CHUNK_SIZE; z++)
                    {
                        Util.ThreeDToOneD(new ValuePoint3D(x + 1, y + 1, z + 1), new ValuePoint3D(CopiedChunkData.WHD), out int i);
                        CubePosition pos = basePosition + new CubePosition(x, y, z);
                        queryPositions[i] = pos;
                    }
                }
            }

            world.ChunkManager.InitializerView.GetIds(queryPositions, copied.ids);
            world.EntityManager.GetEntityMeshingDatas(queryPositions, copied.entityMeshingDatas);

            return copied;
        }

        public void MeshChunk(World world, ChunkPosition position)
        {
            Util.ThreeDToOneD(new ValuePoint3D(position.X, position.Y, position.Z), new ValuePoint3D(sizeInChunks), out int i);
            CollisionMeshInfo meshInfo = meshes[i];

            if (!Main.DO_COLLISION_MESHING)
            {
                meshInfo.meshVersion = meshInfo.version;

                meshes[i] = meshInfo;

                return;
            }

            CopiedChunkData copy = MakeCopy(world, position);

            Span<CubePosition> positions = stackalloc CubePosition[Chunk.CHUNK_SIZE * Chunk.CHUNK_SIZE * Chunk.CHUNK_SIZE];
            Span<MeshHelper.CubeFace> faces = stackalloc MeshHelper.CubeFace[Chunk.CHUNK_SIZE * Chunk.CHUNK_SIZE * Chunk.CHUNK_SIZE];

            int cpi = 0;
            for (int x = 0; x < Chunk.CHUNK_SIZE; x++)
            {
                for (int y = 0; y < Chunk.CHUNK_SIZE; y++)
                {
                    for (int z = 0; z < Chunk.CHUNK_SIZE; z++)
                    {
                        CubePosition pos = new CubePosition(x, y, z, CubePosition.CoordinateSpace.ChunkSpace);
                        //pos = pos.InCubeSpace(cmi.position);

                        positions[cpi] = pos;
                        cpi++;
                    }
                }
            }

            copy.GetFaces(positions, faces);

            (List<VertexCube> verts, List<int> indices) opaques = mesher.GenerateChunk(copy, faces, position, Cube.RenderPass.Opaque);

            if (opaques.verts.Count > 0)
            {
                meshInfo.collidableMesh = GenerateMesh(opaques.verts, opaques.indices);

                meshInfo.collidableShapeIndex = physicsInfo.Simulation.Shapes.Add(meshInfo.collidableMesh);
                meshInfo.collidableStaticHandle = physicsInfo.Simulation.Statics.Add(
                    new StaticDescription(System.Numerics.Vector3.Zero, System.Numerics.Quaternion.Identity, meshInfo.collidableShapeIndex));

                meshInfo.hasMesh = true;
            }
            else meshInfo.collidableMesh = default;

            meshInfo.meshVersion = meshInfo.version;

            meshes[i] = meshInfo;
        }

        //TODO this should eventually make its own mesh instead of using the opaque render pass mesh
        public Mesh GenerateMesh(List<VertexCube> vertices, List<int> indices)
        {
            lock (physicsInfo.GlobalBufferPool)
            {
                physicsInfo.GlobalBufferPool.Take<Triangle>(indices.Count / 3, out var triangleBuffer);

                for (int i = 0; i < indices.Count; i += 3)
                {
                    int a = indices[i];
                    int b = indices[i + 1];
                    int c = indices[i + 2];

                    triangleBuffer[i / 3] = new Triangle(vertices[a].Position.ToNumerics(), vertices[b].Position.ToNumerics(),
                        vertices[c].Position.ToNumerics());
                }

                var collidableMesh = new Mesh(triangleBuffer, System.Numerics.Vector3.One, physicsInfo.GlobalBufferPool);

                return collidableMesh;
            }
        }

        public bool IsMeshed(ChunkPosition position)
        {
            return GetChunkMeshInfo(position).version == GetChunkMeshInfo(position).meshVersion;
        }

        public void Unload(World world, ChunkPosition position)
        {
            if (dirtyChunkKnown.Contains(position))
                dirtyChunkKnown.Remove(position);

            ref CollisionMeshInfo mesh = ref GetChunkMeshInfo(position);

            if (mesh.hasMesh)
            {
                physicsInfo.Simulation.Shapes.Remove(mesh.collidableShapeIndex);
                physicsInfo.Simulation.Statics.Remove(mesh.collidableStaticHandle);
                mesh.collidableMesh.Dispose(physicsInfo.GlobalBufferPool);
                mesh.collidableMesh = default;

                mesh.hasMesh = false;
            }
        }

        public void MarkDirty(ChunkPosition position)
		{
			GetChunkMeshInfo(position).version++;

			if (!dirtyChunkKnown.Contains(position))
			{
				dirtyChunkPositions.Enqueue(position);
				dirtyChunkKnown.Add(position);
			}
		}

        private ref CollisionMeshInfo GetChunkMeshInfo(ChunkPosition pos)
        {
            Util.ThreeDToOneD(new ValuePoint3D(pos.X, pos.Y, pos.Z), new ValuePoint3D(sizeInChunks), out int i);
            return ref meshes[i];
        }
    }
}
