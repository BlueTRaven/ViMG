using BepuPhysics;
using BepuPhysics.Collidables;
using BepuUtilities.Memory;
using BrUtility;
using BrUtility.Ported;
using Microsoft.Xna.Framework;
using SharpDX.MediaFoundation.DirectX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.ChunkStuff;
using ViMG.Cubes;
using ViMG.GameStates;

namespace ViMG
{
    public class ChunkCollisionMesher
    {
        private const int MAX_ACTIVE_MESH_BATCH_TASKS = 20;
        private const int MAX_CHUNKS_TO_MESH_PER_BATCH_TASK = 20;

        //Represents a chunk mesh batch, including everything about a chunk that is necessary to mesh it, or to get the info required to do so.
        private struct CollisionMeshBatch
        {
            public ChunkPosition[] positions;
            public BufferPool[] pools;
            public byte[] versions;

            public CopiedChunkData[] copies;
            public int num;

            public readonly bool isUsed;

            public CollisionMeshBatch(CopiedChunkData[] copies)
            {
                positions = new ChunkPosition[MAX_CHUNKS_TO_MESH_PER_BATCH_TASK];
                pools = new BufferPool[MAX_CHUNKS_TO_MESH_PER_BATCH_TASK];
                versions = new byte[MAX_CHUNKS_TO_MESH_PER_BATCH_TASK];

                this.copies = copies;
                this.num = 0;

                isUsed = true;
            }
        }

        //The state of a chunk batch task state.
        private readonly struct BatchCollisionMeshTaskState
        {
            public readonly CollisionMeshBatch batch;
            public readonly ChunkMesher mesher;

            public BatchCollisionMeshTaskState(CollisionMeshBatch batch, ChunkMesher mesher)
            {
                this.batch = batch;
                this.mesher = mesher;
            }
        }

        //The result of a chunk batch task.
        private readonly struct BatchCollisionMeshTaskResult
        {
            public readonly ChunkPosition[] positions;
            public readonly Mesh[] meshes;
            public readonly BufferPool[] pools;
            public readonly byte[] versions;

            public readonly CopiedChunkData[] copies;
            //number of meshes included in the batch
            public readonly int num;

            public BatchCollisionMeshTaskResult(ChunkPosition[] positions, Mesh[] meshes, BufferPool[] pools, byte[] versions, CopiedChunkData[] copies, int num)
            {
                this.positions = positions;
                this.meshes = meshes;
                this.pools = pools;
                this.versions = versions;

                this.copies = copies;
                this.num = num;
            }
        }

        private struct CollisionMeshInfo
        {
            public TypedIndex collidableShapeIndex; //TODO move elsewhere
            public StaticHandle collidableStaticHandle;
            public Mesh collidableMesh;

            public BufferPool bufferPool;

            public ChunkPosition position;

            public byte version;
            public byte meshVersion;
            public bool hasMesh;
            public bool hasSimReferences;
        }

        private Queue<ChunkPosition> dirtyChunkPositions = new Queue<ChunkPosition>();
        private HashSet<ChunkPosition> dirtyChunkKnown = new HashSet<ChunkPosition>();

        private CollisionMeshBatch currentBatch;
        private PriorityQueue<(CollisionMeshBatch batch, Task<BatchCollisionMeshTaskResult> task)> meshBatchTasksQueue = 
            new PriorityQueue<(CollisionMeshBatch batch, Task<BatchCollisionMeshTaskResult> task)>(true, (batch) =>
        {
            Vector3 avg = Vector3.Zero;

            for (int i = 0; i < MAX_CHUNKS_TO_MESH_PER_BATCH_TASK; i++)
                avg += batch.batch.positions[i].InWorldSpace();

            avg /= MAX_CHUNKS_TO_MESH_PER_BATCH_TASK;

            return (int)(Main.camera.Position - avg).Length();
        });
        //The 'active' batch mesh tasks.
        private Task<BatchCollisionMeshTaskResult>[] activeMeshBatchTasks = new Task<BatchCollisionMeshTaskResult>[MAX_ACTIVE_MESH_BATCH_TASKS];
        private int numActiveChunkMeshBatchTasks;

        private CollisionMeshInfo[] meshes;

        private readonly ChunkMesher mesher;
        private readonly int sizeInChunks;

        private readonly Physics.PhysicsInfo physicsInfo;

        private BufferPool bufferPool;

        public ChunkCollisionMesher(Physics.PhysicsInfo physicsInfo, ChunkMesher mesher, int sizeInChunks)
        {
            bufferPool = new BufferPool();

            this.physicsInfo = physicsInfo;
            meshes = new CollisionMeshInfo[sizeInChunks * sizeInChunks * sizeInChunks];
            this.mesher = mesher;
            this.sizeInChunks = sizeInChunks;
        }

        public void Update(World world)
        {
            if (!currentBatch.isUsed)
                currentBatch = new CollisionMeshBatch(new CopiedChunkData[MAX_CHUNKS_TO_MESH_PER_BATCH_TASK]);

            if (currentBatch.num >= MAX_CHUNKS_TO_MESH_PER_BATCH_TASK)
            {
                EnqueueBatch(ref currentBatch);
                currentBatch = new CollisionMeshBatch(new CopiedChunkData[MAX_CHUNKS_TO_MESH_PER_BATCH_TASK]);
            }

            //Note that we only attempt to enqueue one batch per frame regardless of what MAX_MESH_PER_FRAME is.
            while (dirtyChunkPositions.Count > 0 && currentBatch.num < MAX_CHUNKS_TO_MESH_PER_BATCH_TASK)
            {
                ChunkPosition position = dirtyChunkPositions.Dequeue();
                dirtyChunkKnown.Remove(position);

                ref CollisionMeshInfo meshInfo = ref GetChunkMeshInfo(position);

                if (meshInfo.version != meshInfo.meshVersion || !meshInfo.hasMesh)
                {
                    //place into the current batch to be meshed later.
                    currentBatch.positions[currentBatch.num] = meshInfo.position;
                    currentBatch.pools[currentBatch.num] = meshInfo.bufferPool;
                    currentBatch.versions[currentBatch.num] = (byte)(meshInfo.version + 1);
                    currentBatch.copies[currentBatch.num] = CopiedChunkPool.MakeCopy(world, position);
                    currentBatch.num++;
                }
            }

            //we want to make sure to flush this regardless of whether or not we've actually filled it fully
            //As there might be frames where we don't fully fill it, in which case it could wait a potentially arbitrary amount of time.
            if (currentBatch.num > 0)
            {
                EnqueueBatch(ref currentBatch);
                currentBatch = new CollisionMeshBatch(new CopiedChunkData[MAX_CHUNKS_TO_MESH_PER_BATCH_TASK]);
            }

            StartActiveTasks(world);
        }

        //Flushes all actively enqueued chunks, blocking until they have all been meshed.
        public void Flush()
        {
            Queue<Task<BatchCollisionMeshTaskResult>> tasks = new Queue<Task<BatchCollisionMeshTaskResult>>();

            EnqueueBatch(ref currentBatch);
            currentBatch = new CollisionMeshBatch(new CopiedChunkData[MAX_CHUNKS_TO_MESH_PER_BATCH_TASK]);

            int max = meshBatchTasksQueue.Count;
            GameStateTheIsland.ProgressMax = max;

            while (meshBatchTasksQueue.Count > 0)
            {
                GameStateTheIsland.ProgressMin = max - meshBatchTasksQueue.Count;

                var task = meshBatchTasksQueue.Dequeue().task;

                if (task.Status == TaskStatus.Created)
                {
                    if (Main.MULTITHREAD_MESHING)
                        task.Start();
                    else task.RunSynchronously();
                }
                tasks.Enqueue(task);
            }

            max = tasks.Count;
            GameStateTheIsland.ProgressMax = max;

            while (tasks.Count > 0)
            {
                GameStateTheIsland.ProgressMin = max - tasks.Count;

                var task = tasks.Dequeue();

                if (task.IsCompleted)
                {
                    if (!task.IsCompletedSuccessfully)
                        throw new Exception("???");

                    BatchCollisionMeshTaskResult batchResult = task.Result;

                    for (int j = 0; j < batchResult.num; j++)
                    {
                        batchResult.copies[j].Return();

                        //CollisionMeshInfo meshInfoResult = batchResult.meshInfos[j];

                        ref CollisionMeshInfo meshInfoOld = ref GetChunkMeshInfo(batchResult.positions[j]);

                        if (batchResult.versions[j] != meshInfoOld.version || !meshInfoOld.hasMesh)
                        {
                            //Unload the old mesh now
                            Unload(ref meshInfoOld);

                            //Then paste the result stuff over
                            meshInfoOld.meshVersion = batchResult.versions[j];
                            meshInfoOld.version = batchResult.versions[j];

                            meshInfoOld.collidableMesh = batchResult.meshes[j];

                            if (batchResult.meshes[j].Triangles.Allocated)
                            {
                                //We have to postpone adding the shapes and stuff since this requires access to the simulation
                                //and that can't be multithreaded.
                                meshInfoOld.collidableShapeIndex = physicsInfo.Simulation.Shapes.Add(meshInfoOld.collidableMesh);
                                meshInfoOld.collidableStaticHandle = physicsInfo.Simulation.Statics.Add(
                                    new StaticDescription(System.Numerics.Vector3.Zero, System.Numerics.Quaternion.Identity,
                                    meshInfoOld.collidableShapeIndex));

                                meshInfoOld.hasSimReferences = true;
                                meshInfoOld.hasMesh = true;
                            }
                        }
                        else
                        {
                            //version has changed while we're meshing - discard the old mesh, as a new one should already be queued.
                            batchResult.meshes[j].Dispose(batchResult.pools[j]);
                        }
                    }
                }
                else tasks.Enqueue(task);
            }
        }

        private void StartActiveTasks(World world)
        {
            //First, check for complete tasks.
            for (int i = 0; i < activeMeshBatchTasks.Length; i++)
            {
                if (activeMeshBatchTasks[i] != null && activeMeshBatchTasks[i].IsCompleted)
                {
                    numActiveChunkMeshBatchTasks--;

                    var task = activeMeshBatchTasks[i];

                    if (!task.IsCompletedSuccessfully)
                        throw new Exception("???");

                    var batchResult = task.Result;

                    for (int j = 0; j < batchResult.num; j++)
                    {
                        batchResult.copies[j].Return();

                        //CollisionMeshInfo meshInfoResult = batchResult.meshInfos[j];

                        ref CollisionMeshInfo meshInfoOld = ref GetChunkMeshInfo(batchResult.positions[j]);

                        if (batchResult.versions[j] != meshInfoOld.version || !meshInfoOld.hasMesh)
                        {
                            //Unload the old mesh now
                            Unload(ref meshInfoOld);

                            //Then paste the result stuff over
                            meshInfoOld.meshVersion = batchResult.versions[j];
                            meshInfoOld.version = batchResult.versions[j];

                            meshInfoOld.collidableMesh = batchResult.meshes[j];

                            if (batchResult.meshes[j].Triangles.Allocated)
                            {
                                //We have to postpone adding the shapes and stuff since this requires access to the simulation
                                //and that can't be multithreaded.
                                meshInfoOld.collidableShapeIndex = physicsInfo.Simulation.Shapes.Add(meshInfoOld.collidableMesh);
                                meshInfoOld.collidableStaticHandle = physicsInfo.Simulation.Statics.Add(
                                    new StaticDescription(System.Numerics.Vector3.Zero, System.Numerics.Quaternion.Identity, 
                                    meshInfoOld.collidableShapeIndex));
                                
                                meshInfoOld.hasSimReferences = true;
                                meshInfoOld.hasMesh = true;
                            }
                        }
                        else
                        {
                            //version has changed while we're meshing - discard the old mesh, as a new one should already be queued.
                            batchResult.meshes[j].Dispose(batchResult.pools[j]);
                        }
                    }

                    activeMeshBatchTasks[i] = null;
                }

                if (activeMeshBatchTasks[i] == null && meshBatchTasksQueue.Count > 0)
                {
                    meshBatchTasksQueue.Sort();
                    var task = meshBatchTasksQueue.Dequeue();
                    activeMeshBatchTasks[i] = task.task;
                    numActiveChunkMeshBatchTasks++;

                    if (task.task.Status == TaskStatus.Created)
                    {
                        if (Main.MULTITHREAD_MESHING)
                            task.task.Start();
                        else task.task.RunSynchronously();
                    }
                }
            }
        }

        //Adds a position in the current batch. 
        public void AddToNextBatch(World world, ChunkPosition position)
        {
            if (!currentBatch.isUsed)
                currentBatch = new CollisionMeshBatch(new CopiedChunkData[MAX_CHUNKS_TO_MESH_PER_BATCH_TASK]);

            if (currentBatch.num >= MAX_CHUNKS_TO_MESH_PER_BATCH_TASK)
            {
                EnqueueBatch(ref currentBatch);
                currentBatch = new CollisionMeshBatch(new CopiedChunkData[MAX_CHUNKS_TO_MESH_PER_BATCH_TASK]);
            }

            ref CollisionMeshInfo meshInfo = ref GetChunkMeshInfo(position);

            if (meshInfo.version != meshInfo.meshVersion || !meshInfo.hasMesh)
            {
                currentBatch.positions[currentBatch.num] = meshInfo.position;
                currentBatch.pools[currentBatch.num] = meshInfo.bufferPool;
                currentBatch.versions[currentBatch.num] = (byte)(meshInfo.version + 1);
                currentBatch.copies[currentBatch.num] = CopiedChunkPool.MakeCopy(world, position);
                currentBatch.num++;
            }
        }

        private void EnqueueBatch(ref CollisionMeshBatch batch)
        {
            Task<BatchCollisionMeshTaskResult> task = new Task<BatchCollisionMeshTaskResult>(MeshBatchFn, new BatchCollisionMeshTaskState(batch, mesher));

            meshBatchTasksQueue.EnqueueWithoutSorting((batch, task));
        }

        private static BatchCollisionMeshTaskResult MeshBatchFn(object obj)
        {
            BatchCollisionMeshTaskState state = (BatchCollisionMeshTaskState)obj;

            Span<CubePosition> positions = stackalloc CubePosition[Chunk.CHUNK_SIZE * Chunk.CHUNK_SIZE * Chunk.CHUNK_SIZE];
            Span<MeshHelper.CubeFace> faces = stackalloc MeshHelper.CubeFace[Chunk.CHUNK_SIZE * Chunk.CHUNK_SIZE * Chunk.CHUNK_SIZE];

            Mesh[] meshes = new Mesh[MAX_CHUNKS_TO_MESH_PER_BATCH_TASK];

            if (Main.DO_COLLISION_MESHING)
            {
                for (int i = 0; i < state.batch.num; i++)
                {
                    for (int z = 0; z < Chunk.CHUNK_SIZE; z++)
                    {
                        for (int y = 0; y < Chunk.CHUNK_SIZE; y++)
                        {
                            for (int x = 0; x < Chunk.CHUNK_SIZE; x++)
                            {
                                CubePosition pos = new CubePosition(x, y, z, CubePosition.CoordinateSpace.ChunkSpace);
                                Util.ThreeDToOneD(new ValuePoint3D(x, y, z), new ValuePoint3D(Chunk.CHUNK_SIZE), out int j);
                                //pos = pos.InCubeSpace(cmi.position);

                                positions[j] = pos;
                            }
                        }
                    }

                    state.batch.copies[i].GetFaces(positions, faces);

                    (List<VertexCube> verts, List<int> indices) opaques = state.mesher.GenerateChunk(state.batch.copies[i], faces, state.batch.positions[i], Cube.RenderPass.Opaque);

                    if (opaques.verts.Count > 0)
                        meshes[i] = GenerateMesh(state.batch.pools[i], opaques.verts, opaques.indices);
                    //else meshes[i] = default;
                }
            }

            return new BatchCollisionMeshTaskResult(state.batch.positions, meshes, state.batch.pools, state.batch.versions, state.batch.copies, state.batch.num);
        }

        //TODO this should eventually make its own mesh instead of using the opaque render pass mesh
        public static Mesh GenerateMesh(BufferPool bufferPool, List<VertexCube> vertices, List<int> indices)
        {
            Buffer<Triangle> triangleBuffer;

            lock (bufferPool)
                bufferPool.Take(indices.Count / 3, out triangleBuffer);

            for (int i = 0; i < indices.Count; i += 3)
            {
                int a = indices[i];
                int b = indices[i + 1];
                int c = indices[i + 2];

                triangleBuffer[i / 3] = new Triangle(vertices[a].Position.ToNumerics(), vertices[b].Position.ToNumerics(),
                    vertices[c].Position.ToNumerics());
            }

            lock (bufferPool)
            {
                var collidableMesh = new Mesh(triangleBuffer, System.Numerics.Vector3.One, bufferPool);

                return collidableMesh;
            }
        }

        public bool IsMeshed(ChunkPosition position)
        {
            return GetChunkMeshInfo(position).version == GetChunkMeshInfo(position).meshVersion;
        }

        private void Unload(ref CollisionMeshInfo meshInfo)
        {
            if (meshInfo.hasMesh)
            {
                if (meshInfo.hasSimReferences)
                {
                    physicsInfo.Simulation.Shapes.Remove(meshInfo.collidableShapeIndex);
                    physicsInfo.Simulation.Statics.Remove(meshInfo.collidableStaticHandle);
                    meshInfo.collidableShapeIndex = default;
                    meshInfo.collidableStaticHandle = default;
                    meshInfo.hasSimReferences = false;
                }

                lock (meshInfo.bufferPool)
                    meshInfo.collidableMesh.Dispose(meshInfo.bufferPool);
                meshInfo.collidableMesh = default;

                meshInfo.hasMesh = false;
            }
            else if (meshInfo.collidableMesh.Triangles.Allocated)
            {
                //hasMesh is false but triangles are allocated?
                Console.WriteLine("Leaked chunk collision mesh at {0}", meshInfo.position.ToString());
            }
        }

        public void Unload(ChunkPosition position)
        {
            if (dirtyChunkKnown.Contains(position))
                dirtyChunkKnown.Remove(position);

            ref CollisionMeshInfo meshInfo = ref GetChunkMeshInfo(position);

            Unload(ref meshInfo);
        }

        public void UnloadAll()
        {
            for (int j = 0; j < sizeInChunks * sizeInChunks * sizeInChunks; j++)
            {
                if (meshes[j].hasMesh)
                {
                    if (meshes[j].hasSimReferences)
                    {
                        physicsInfo.Simulation.Shapes.Remove(meshes[j].collidableShapeIndex);
                        physicsInfo.Simulation.Statics.Remove(meshes[j].collidableStaticHandle);
                        meshes[j].collidableShapeIndex = default;
                        meshes[j].collidableStaticHandle = default;
                        meshes[j].hasSimReferences = false;
                    }

                    lock (meshes[j].bufferPool)
                        meshes[j].collidableMesh.Dispose(meshes[j].bufferPool);

                    meshes[j].collidableMesh = default;

                    meshes[j].hasMesh = false;
                }
                else if (meshes[j].collidableMesh.Triangles.Allocated)
                {
                    Console.WriteLine("Leaked chunk collision mesh at {0}", meshes[j].position);
                }
            }

            bufferPool.AssertEmpty();
            bufferPool.Clear();
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
            ref CollisionMeshInfo meshInfo = ref meshes[i];
            meshInfo.position = pos;

            if (meshInfo.bufferPool == null)
                meshInfo.bufferPool = bufferPool;

            return ref meshInfo;
        }
    }
}
