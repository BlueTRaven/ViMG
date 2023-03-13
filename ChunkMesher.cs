using BepuUtilities.Memory;
using BrUtility;
using BrUtility.Ported;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ViMG.ChunkStuff;
using ViMG.Cubes;
using ViMG.Entities;

namespace ViMG
{
	public class ChunkMesher
	{
#if DEBUG
		private const int MAX_ACTIVE_MESH_BATCH_TASKS = 2;
		private const int MAX_CHUNKS_TO_MESH_PER_BATCH_TASK = 2;
#else
		private const int MAX_ACTIVE_MESH_BATCH_TASKS = 6;
		private const int MAX_CHUNKS_TO_MESH_PER_BATCH_TASK = 2;
#endif
		public const int NUM_CHUNK_MESH_PASSES = 5;

		//Represents a chunk mesh batch, including everything about a chunk that is necessary to mesh it, or to get the info required to do so.
		private struct ChunkMeshBatch
		{
			public ChunkMeshInfo[] cmis;
			public CopiedChunkData[] copies;
			public int num;

			public readonly bool isUsed;

			public ChunkMeshBatch(ChunkMeshInfo[] cmis, CopiedChunkData[] copies)
			{
				this.cmis = cmis;
				this.copies = copies;
				this.num = 0;

				isUsed = true;
			}
		}

		//The state of a chunk batch task state.
		private readonly struct ChunkBatchMeshTaskState
		{
			public readonly ChunkMeshBatch batch;
			public readonly World world;
			public readonly ChunkManager manager;
			public readonly ChunkMesher mesher;

			public ChunkBatchMeshTaskState(ChunkMeshBatch batch, World world, ChunkManager manager, ChunkMesher mesher)
			{
				this.batch = batch;
				this.world = world;
				this.manager = manager;
				this.mesher = mesher;
			}
		}

		//The result of a chunk batch task.
		private readonly struct ChunkBatchMeshTaskResult
		{
			public readonly ChunkMeshInfo[] cmis;
			//number of meshes included in the batch
			public readonly int num;

			public ChunkBatchMeshTaskResult(ChunkMeshInfo[] cmis, int num)
			{
				this.cmis = cmis;
				this.num = num;
			}
		}

		private struct ChunkMeshInfo
		{
			public ChunkPosition position;
			public (VertexBuffer VBO, IndexBuffer IBO)[] meshes;
			public byte meshVersion; //mesh version; if different from version, needs to be re-meshed
			public byte version;

			//There's a difference between having existing meshes and needing to be remeshed (being dirty) and not having meshes at all.
			//Therefore this bool exists to determine if the given chunk has a mesh. If it doesn't, it isn't necessarily marked dirty,
			//it just needs a mesh to be created in the first place.
			public bool hasMeshes;

			public ChunkMeshInfo(ChunkPosition position)
			{
				this.position = position;
				meshes = new (VertexBuffer VBO, IndexBuffer IBO)[NUM_CHUNK_MESH_PASSES];
				meshVersion = 0;
				version = 1;

				hasMeshes = false;
			}

			public int GetMeshVersionCode()
			{
				return meshes.GetHashCode() + version;
			}
		}

        private readonly GraphicsDevice device;
        private readonly int sizeInChunks;
        private ChunkMeshBatch currentBatch;
		private Task<ChunkBatchMeshTaskResult>[] activeChunkMeshBatchTasks = new Task<ChunkBatchMeshTaskResult>[MAX_ACTIVE_MESH_BATCH_TASKS];
		private int numActiveChunkMeshBatchTasks;

		private PriorityQueue<(ChunkMeshBatch batch, Task<ChunkBatchMeshTaskResult> task)> chunkMeshBatchTasks = new PriorityQueue<(ChunkMeshBatch batch, Task<ChunkBatchMeshTaskResult> task)>(true, (x) =>
		{
			Vector3 avg = Vector3.Zero;

			for (int i = 0; i < MAX_CHUNKS_TO_MESH_PER_BATCH_TASK; i++)
				avg += x.batch.cmis[i].position.InWorldSpace();

			avg /= MAX_CHUNKS_TO_MESH_PER_BATCH_TASK;

			return (int)(Main.camera.Position - avg).Length();
		});

		private ChunkMeshInfo[] chunkMeshInfos;

		private Queue<ChunkPosition> dirtyChunkPositions = new Queue<ChunkPosition>();
		private HashSet<ChunkPosition> dirtyChunkKnown = new HashSet<ChunkPosition>();

		private BufferPool buffer;

		public ChunkMesher(GraphicsDevice device, int sizeInChunks)
        {
            this.device = device;
            this.sizeInChunks = sizeInChunks;

			this.buffer = new BufferPool();

			chunkMeshInfos = new ChunkMeshInfo[sizeInChunks * sizeInChunks * sizeInChunks];
			for (int i = 0; i < sizeInChunks * sizeInChunks * sizeInChunks; i++)
			{
				Util.OneDToThreeD(i, new ValuePoint3D(sizeInChunks), out ValuePoint3D point);
				chunkMeshInfos[i] = new ChunkMeshInfo(new ChunkPosition(point.x, point.y, point.z));
			}
		}

		public void Update(World world, ChunkManager manager)
        {
			if (!currentBatch.isUsed)
				currentBatch = new ChunkMeshBatch(new ChunkMeshInfo[MAX_CHUNKS_TO_MESH_PER_BATCH_TASK], new CopiedChunkData[MAX_CHUNKS_TO_MESH_PER_BATCH_TASK]);

			if (currentBatch.num >= MAX_CHUNKS_TO_MESH_PER_BATCH_TASK)
			{
				EnqueueBatchLocking(world, ref currentBatch);
				currentBatch = new ChunkMeshBatch(new ChunkMeshInfo[MAX_CHUNKS_TO_MESH_PER_BATCH_TASK], new CopiedChunkData[MAX_CHUNKS_TO_MESH_PER_BATCH_TASK]);
			}

			//Note that we only attempt to enqueue one batch per frame regardless of what MAX_MESH_PER_FRAME is.
			while (dirtyChunkPositions.Count > 0 && currentBatch.num < MAX_CHUNKS_TO_MESH_PER_BATCH_TASK)
			{
				ChunkPosition position = dirtyChunkPositions.Dequeue();
				dirtyChunkKnown.Remove(position);

				ref ChunkMeshInfo c = ref GetChunkMeshInfo(position);

				if (c.version != c.meshVersion || !c.hasMeshes)
				{
					//place into the current batch to be meshed later.
					currentBatch.cmis[currentBatch.num] = c;
					currentBatch.copies[currentBatch.num] = MakeCopy(world, position);
					currentBatch.num++;
				}
			}

			//we want to make sure to flush this regardless of whether or not we've actually filled it fully
			//As there might be frames where we don't fully fill it, in which case it could wait a potentially arbitrary amount of time.
			if (currentBatch.num > 0)
			{
				EnqueueBatchLocking(world, ref currentBatch);
				currentBatch = new ChunkMeshBatch(new ChunkMeshInfo[MAX_CHUNKS_TO_MESH_PER_BATCH_TASK], new CopiedChunkData[MAX_CHUNKS_TO_MESH_PER_BATCH_TASK]);
			}

			StartActiveTasks(world);
		}

		//Flushes all actively enqueued chunks, blocking until they have all been meshed.
		public void Flush(World world)
        {
			Queue<Task<ChunkBatchMeshTaskResult>> tasks = new Queue<Task<ChunkBatchMeshTaskResult>>();

            EnqueueBatch(world, ref currentBatch);
			currentBatch = new ChunkMeshBatch(new ChunkMeshInfo[MAX_CHUNKS_TO_MESH_PER_BATCH_TASK], new CopiedChunkData[MAX_CHUNKS_TO_MESH_PER_BATCH_TASK]);

			int max = chunkMeshBatchTasks.Count;
			world.GameStateManager.TheIsland.ProgressMax = max;

			while (chunkMeshBatchTasks.Count > 0)
            {
				world.GameStateManager.TheIsland.ProgressMin = max - chunkMeshBatchTasks.Count;

				var task = chunkMeshBatchTasks.Dequeue().task;

				if (task.Status == TaskStatus.Created)
				{
					if (Main.MULTITHREAD_MESHING)
						task.Start();
					else task.RunSynchronously();
				}
				tasks.Enqueue(task);
			}

			max = tasks.Count;
			world.GameStateManager.TheIsland.ProgressMax = max;

			while (tasks.Count > 0)
            {
				world.GameStateManager.TheIsland.ProgressMin = max - tasks.Count;

				var task = tasks.Dequeue();

				if (task.IsCompleted)
				{
					if (!task.IsCompletedSuccessfully)
						throw new Exception("???");

					var batchResult = task.Result;

					for (int j = 0; j < batchResult.num; j++)
					{
						ChunkMeshInfo meshResult = batchResult.cmis[j];

						ref ChunkMeshInfo c = ref GetChunkMeshInfo(meshResult.position);

						if (meshResult.version >= c.version)
						{
							//Unload the old mesh now
							UnloadMesh(ref c);

							c.meshVersion = c.version;

							c.meshes = meshResult.meshes;
						}
						else
						{
							//version has changed while we're meshing - discard the old mesh, as a new one should already be queued.
							UnloadMesh(ref meshResult);
						}
                    }
				}
				else tasks.Enqueue(task);
			}
        }

		private void StartActiveTasks(World world)
		{
			for (int i = 0; i < activeChunkMeshBatchTasks.Length; i++)
			{
				if (activeChunkMeshBatchTasks[i] != null && activeChunkMeshBatchTasks[i].IsCompleted)
				{
					numActiveChunkMeshBatchTasks--;

					var task = activeChunkMeshBatchTasks[i];

					if (!task.IsCompletedSuccessfully)
						throw new Exception("???");

					var batchResult = task.Result;

					for (int j = 0; j < batchResult.num; j++)
					{
						ChunkMeshInfo meshResult = batchResult.cmis[j];

						ref ChunkMeshInfo c = ref GetChunkMeshInfo(meshResult.position);

						if (meshResult.version >= c.version)
						{
							//Unload the old mesh now
							UnloadMesh(ref c);

							c.meshVersion = c.version;

							c.meshes = meshResult.meshes;
						}
						else
						{
							//version has changed while we're meshing - discard the old mesh, as a new one should already be queued.
							UnloadMesh(ref meshResult);
						}

                        /*if (meshResult.collidableMesh.Triangles.Allocated)
                        {
                            meshResult.collidableShapeIndex = world.PhysicsSimulation.Shapes.Add(meshResult.collidableMesh);
                            meshResult.collidableStaticHandle = world.PhysicsSimulation.Statics.Add(
                                new BepuPhysics.StaticDescription(System.Numerics.Vector3.Zero, System.Numerics.Quaternion.Identity, meshResult.collidableShapeIndex));
                        }*/
                    }

					activeChunkMeshBatchTasks[i] = null;
				}
				
				if (activeChunkMeshBatchTasks[i] == null && chunkMeshBatchTasks.Count > 0)
                {
					chunkMeshBatchTasks.Sort();
					var task = chunkMeshBatchTasks.Dequeue();
					activeChunkMeshBatchTasks[i] = task.task;
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

		public void BatchMeshChunk(World world, ChunkPosition position)
		{
			if (!currentBatch.isUsed)
				currentBatch = new ChunkMeshBatch(new ChunkMeshInfo[MAX_CHUNKS_TO_MESH_PER_BATCH_TASK], new CopiedChunkData[MAX_CHUNKS_TO_MESH_PER_BATCH_TASK]);

			if (currentBatch.num >= MAX_CHUNKS_TO_MESH_PER_BATCH_TASK)
			{
				EnqueueBatch(world, ref currentBatch);
				currentBatch = new ChunkMeshBatch(new ChunkMeshInfo[MAX_CHUNKS_TO_MESH_PER_BATCH_TASK], new CopiedChunkData[MAX_CHUNKS_TO_MESH_PER_BATCH_TASK]);
			}

			ref ChunkMeshInfo c = ref GetChunkMeshInfo(position);

			if (c.version != c.meshVersion || !c.hasMeshes)
			{
				currentBatch.cmis[currentBatch.num] = c;
				currentBatch.copies[currentBatch.num] = MakeCopy(world, position);
				currentBatch.num++;
			}
		}

		private CopiedChunkData MakeCopy(World world, ChunkPosition position)
		{
			ProfilingHelper.Start("Making copy of chunk...");

            CubePosition basePosition = position.InCubeSpace();
			CopiedChunkData copied = new CopiedChunkData()
			{
				ids = new ushort[CopiedChunkData.SIZE],
				entityMeshingDatas = new object[CopiedChunkData.SIZE],
				basePosition = basePosition,
            };

            for (int x = -1; x <= Chunk.CHUNK_SIZE; x++)
            {
                for (int y = -1; y <= Chunk.CHUNK_SIZE; y++)
                {
                    for (int z = -1; z <= Chunk.CHUNK_SIZE; z++)
                    {
                        Util.ThreeDToOneD(new ValuePoint3D(x + 1, y + 1, z + 1), new ValuePoint3D(CopiedChunkData.WHD), out int i);
                        CubePosition pos = basePosition + new CubePosition(x, y, z);

						if (world.ChunkManager.IsInWorldBounds(pos))
						{
							copied.ids[i] = world.ChunkManager.InitializerView.GetId(pos);
                            Entities.Entity tracking = world.EntityManager.GetEntityTrackingPosition(pos).Get();

							if (tracking is ICubeTracker tracker)
								copied.entityMeshingDatas[i] = tracker.GetMeshingData(); 
						}
                    }
                }
            }

			ProfilingHelper.End("Done.");

			return copied;
        }

        private void EnqueueBatch(World world, ref ChunkMeshBatch batch)
        {
            Task<ChunkBatchMeshTaskResult> task = new Task<ChunkBatchMeshTaskResult>(MeshBatchFn, new ChunkBatchMeshTaskState(batch, world, world.ChunkManager, this));

            chunkMeshBatchTasks.EnqueueWithoutSorting((batch, task));
        }

        private void EnqueueBatchLocking(World world, ref ChunkMeshBatch batch)
		{
			Task<ChunkBatchMeshTaskResult> task = new Task<ChunkBatchMeshTaskResult>(LockingMeshBatchFn, new ChunkBatchMeshTaskState(batch, world, world.ChunkManager, this));

			chunkMeshBatchTasks.EnqueueWithoutSorting((batch, task));
		}

        private static ChunkBatchMeshTaskResult MeshBatchFn(object obj)
        {
            ChunkBatchMeshTaskState state = (ChunkBatchMeshTaskState)obj;

            Span<CubePosition> positions = stackalloc CubePosition[Chunk.CHUNK_SIZE * Chunk.CHUNK_SIZE * Chunk.CHUNK_SIZE];
            Span<ushort> ids = stackalloc ushort[Chunk.CHUNK_SIZE * Chunk.CHUNK_SIZE * Chunk.CHUNK_SIZE];
            Span<MeshHelper.CubeFace> faces = stackalloc MeshHelper.CubeFace[Chunk.CHUNK_SIZE * Chunk.CHUNK_SIZE * Chunk.CHUNK_SIZE];
            ChunkMeshData data = new ChunkMeshData(positions, ids, faces);

            for (int i = 0; i < state.batch.num; i++)
            {
                ChunkMeshInfo cmi = state.batch.cmis[i];
                int cpi = 0;
                for (int x = 0; x < Chunk.CHUNK_SIZE; x++)
                {
                    for (int y = 0; y < Chunk.CHUNK_SIZE; y++)
                    {
                        for (int z = 0; z < Chunk.CHUNK_SIZE; z++)
                        {
                            CubePosition pos = new CubePosition(x, y, z, CubePosition.CoordinateSpace.ChunkSpace);
                            pos = pos.InCubeSpace(cmi.position);

                            positions[cpi] = pos;
                            cpi++;
                        }
                    }
                }

                const int NUM_SPLITS = 1;
                int c = cpi / NUM_SPLITS;

                for (int k = 0; k < NUM_SPLITS; k++)
                {
                    int offset = c * k;
                    state.manager.InitializerView.GetIds(positions, ids, offset, c);
                    state.manager.InitializerView.GetFaces(positions, faces, offset, c);
                }

                cmi.meshes = new (VertexBuffer VBO, IndexBuffer IBO)[NUM_CHUNK_MESH_PASSES];

                (List<VertexCube> verts, List<int> indices) opaques = state.mesher.GenerateChunk(in data, state.world, state.manager, cmi.position, Cube.RenderPass.Opaque);

                cmi.meshes[(int)Cube.RenderPass.Opaque] = MeshHelper.MakeSimplerMesh(state.mesher.device, opaques);
                cmi.meshes[(int)Cube.RenderPass.Transparent] = MeshHelper.MakeSimplerMesh(state.mesher.device, state.mesher.GenerateChunk(in data, state.world, state.manager, cmi.position, Cube.RenderPass.Transparent));
                cmi.meshes[(int)Cube.RenderPass.DepthOnly] = MeshHelper.MakeSimplerMesh(state.mesher.device, state.mesher.GenerateChunk(in data, state.world, state.manager, cmi.position, Cube.RenderPass.DepthOnly));
                cmi.meshes[(int)Cube.RenderPass.Fluid] = (null, null);   //TODO fluids?
                cmi.meshes[(int)Cube.RenderPass.Air] = MeshHelper.MakeSimplerMesh(state.mesher.device, state.mesher.GenerateChunk(in data, state.world, state.manager, cmi.position, Cube.RenderPass.Air));

                state.batch.cmis[i] = cmi;
                state.batch.cmis[i].hasMeshes = true;
            }

            return new ChunkBatchMeshTaskResult(state.batch.cmis, state.batch.num);
        }

        private static ChunkBatchMeshTaskResult LockingMeshBatchFn(object obj)
		{
			ChunkBatchMeshTaskState state = (ChunkBatchMeshTaskState)obj;

			Span<CubePosition> positions = stackalloc CubePosition[Chunk.CHUNK_SIZE * Chunk.CHUNK_SIZE * Chunk.CHUNK_SIZE];
			Span<ushort> ids = stackalloc ushort[Chunk.CHUNK_SIZE * Chunk.CHUNK_SIZE * Chunk.CHUNK_SIZE];
			Span<MeshHelper.CubeFace> faces = stackalloc MeshHelper.CubeFace[Chunk.CHUNK_SIZE * Chunk.CHUNK_SIZE * Chunk.CHUNK_SIZE];
			ChunkMeshData data = new ChunkMeshData(positions, ids, faces);

			for (int i = 0; i < state.batch.num; i++)
			{
				ChunkMeshInfo cmi = state.batch.cmis[i];
				int cpi = 0;
				for (int x = 0; x < Chunk.CHUNK_SIZE; x++)
				{
					for (int y = 0; y < Chunk.CHUNK_SIZE; y++)
					{
						for (int z = 0; z < Chunk.CHUNK_SIZE; z++)
						{
							CubePosition pos = new CubePosition(x, y, z, CubePosition.CoordinateSpace.ChunkSpace);
							pos = pos.InCubeSpace(cmi.position);

							positions[cpi] = pos;
							cpi++;
						}
					}
				}

				const int NUM_SPLITS = 1;
				int c = cpi / NUM_SPLITS;

				for (int k = 0; k < NUM_SPLITS; k++)
				{
					int offset = c * k;
					state.manager.ThreadedView.GetIds(positions, ids, offset, c);
					state.manager.ThreadedView.GetFaces(positions, faces, offset, c);
				}

				cmi.meshes = new (VertexBuffer VBO, IndexBuffer IBO)[NUM_CHUNK_MESH_PASSES];

				(List<VertexCube> verts, List<int> indices) opaques = state.mesher.GenerateChunk(in data, state.world, state.manager, cmi.position, Cube.RenderPass.Opaque, true);

				cmi.meshes[(int)Cube.RenderPass.Opaque] = MeshHelper.MakeSimplerMesh(state.mesher.device, opaques);
                cmi.meshes[(int)Cube.RenderPass.Transparent] = MeshHelper.MakeSimplerMesh(state.mesher.device, state.mesher.GenerateChunk(in data, 
					state.world, state.manager, cmi.position, Cube.RenderPass.Transparent, true));
                cmi.meshes[(int)Cube.RenderPass.DepthOnly] = MeshHelper.MakeSimplerMesh(state.mesher.device, state.mesher.GenerateChunk(in data, 
					state.world, state.manager, cmi.position, Cube.RenderPass.DepthOnly, true));
                cmi.meshes[(int)Cube.RenderPass.Fluid] = (null, null);   //TODO fluids?
                cmi.meshes[(int)Cube.RenderPass.Air] = MeshHelper.MakeSimplerMesh(state.mesher.device, state.mesher.GenerateChunk(in data, 
					state.world, state.manager, cmi.position, Cube.RenderPass.Air, true));

                state.batch.cmis[i] = cmi;
				state.batch.cmis[i].hasMeshes = true;
			}

			return new ChunkBatchMeshTaskResult(state.batch.cmis, state.batch.num);
		}

		private void UnloadMesh(ref ChunkMeshInfo c)
		{
			for (int i = 0; i < NUM_CHUNK_MESH_PASSES; i++)
			{
				if (c.meshes[i].VBO != null)
				{
					c.meshes[i].VBO.Dispose();
					c.meshes[i].IBO.Dispose();

					c.meshes[i] = (null, null);
				}

			}

			c.hasMeshes = false;
		}

		public void UnloadMesh(ChunkPosition position)
        {
			UnloadMesh(ref GetChunkMeshInfo(position));
        }

		public void UnloadAllMeshes()
		{
			lock (buffer)
			{
				for (int j = 0; j < sizeInChunks * sizeInChunks * sizeInChunks; j++)
				{
					for (int k = 0; k < NUM_CHUNK_MESH_PASSES; k++)
					{
						(VertexBuffer VBO, IndexBuffer IBO) mesh = chunkMeshInfos[j].meshes[k];
						if (mesh.VBO != null)
						{
							mesh.VBO.Dispose();
							mesh.IBO.Dispose();

							chunkMeshInfos[j].meshes[k] = (null, null);
						}
					}

					chunkMeshInfos[j].hasMeshes = false;
				}
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

		public bool IsMeshed(ChunkPosition position)
        {
			//we know we're not meshing this chunk currently if meshVersion is equal to version.
			return GetChunkMeshInfo(position).meshVersion == GetChunkMeshInfo(position).version;
        }

		public (VertexBuffer VBO, IndexBuffer IBO) GetMesh(ChunkPosition position, Cube.RenderPass pass)
		{
			(VertexBuffer VBO, IndexBuffer IBO) mesh = GetChunkMeshInfo(position).meshes[(int)pass];

			if (mesh.VBO != null && mesh.VBO.IsDisposed)
				throw new Exception("??");

			return mesh;
		}

		private ref ChunkMeshInfo GetChunkMeshInfo(ChunkPosition pos)
		{
			Util.ThreeDToOneD(new ValuePoint3D(pos.X, pos.Y, pos.Z), new ValuePoint3D(sizeInChunks), out int i);
			return ref chunkMeshInfos[i];
		}

		public int GetMeshVersionCode(ChunkPosition position)
		{
			return GetChunkMeshInfo(position).GetMeshVersionCode();
		}

		public struct CubeMeshingQuad
		{
			public Vector3 a;
			public Vector3 b;
			public Vector3 c;
			public Vector3 d;
			public Vector3 n;

			public CubeMeshingQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 n)
			{
				this.a = a;
				this.b = b;
				this.c = c;
				this.d = d;
				this.n = n;
			}
		}

		public struct CubeMeshingParameters
		{
			public CubePosition position;
			public Vector3 positionWS;
			public ushort id;
			public Cube cube;
			public MeshHelper.CubeFace faces;
		}
		
		public (List<VertexCube> vertices, List<int> indices) GenerateChunk(in ChunkMeshData data, World world, ChunkManager manager, ChunkPosition position, Cube.RenderPass pass, bool threaded = false)
		{
			Vector3 n = new Vector3(0);
			Vector3 f = new Vector3(Cube.CUBE_SCALE);

			List<VertexCube> vertices = new List<VertexCube>();
			List<int> indices = new List<int>();

			for (int i = 0; i < Chunk.CHUNK_SIZE * Chunk.CHUNK_SIZE * Chunk.CHUNK_SIZE; i++) 
			{
				CubePosition pos = data.positions[i];
				ushort id = data.ids[i];
				MeshHelper.CubeFace faces = data.faces[i];

				if (pass == Cube.RenderPass.Transparent || pass == Cube.RenderPass.Opaque || pass == Cube.RenderPass.Fluid || pass == Cube.RenderPass.DepthOnly)
				{
					//if we're air or have no faces, ignore this cube.
					if (id == 0 || faces == MeshHelper.CubeFace.NONE)
						continue;

					Cube cube = Main.Registry.CubeRegistry.Get(id);

					if (cube.ShouldMeshPass(pass))
					{
						CubeMeshingParameters parameters = new CubeMeshingParameters()
						{
							cube = cube,
							id = id,
							positionWS = pos.InWorldSpace(),
							position = pos,
							faces = faces
						};

						int oldCount = vertices.Count;

						cube.MakeCubeVerts(pass, world, parameters, vertices, indices);

						int count = vertices.Count - oldCount;

						BakeAO(manager, pos, oldCount, oldCount + count, vertices, threaded);
					}
				}
				else if (pass == Cube.RenderPass.Air)
				{
					//Note that for air, we we do still make verts if id is 0 (though still not if no faces).
					if (id != 0 || faces == MeshHelper.CubeFace.NONE)
						continue;

                    CubeMeshingParameters parameters = new CubeMeshingParameters()
                    {
                        cube = Main.Registry.CubeRegistry.Air,
                        id = id,
                        positionWS = pos.InWorldSpace(),
                        position = pos,
                        faces = faces
                    };

					Main.Registry.CubeRegistry.Air.MakeCubeVerts(pass, world, parameters, vertices, indices);
				}
			}

			return (vertices, indices);
		}

		private static void BakeAO(ChunkManager manager, CubePosition pos, int start, int end, List<VertexCube> vertices, bool threaded = false)
        {
			Span<CubePosition> checkPositions = stackalloc CubePosition[4];
			Span<ushort> checkIds = stackalloc ushort[4];

			for (int i = start; i < end; i++)
			{
				VertexCube vertex = vertices[i];

				//pos =
				CubePosition cubePos = pos;
				//pc =
				CubePosition vertCubePos = CubePosition.FromWorldSpace(vertex.Position);

				CubePosition nrm = new CubePosition(cubePos.X + (int)vertex.Normal.X,
					cubePos.Y + (int)vertex.Normal.Y,
					cubePos.Z + (int)vertex.Normal.Z, CubePosition.CoordinateSpace.CubeSpace);

				CubePosition t = new CubePosition();
				CubePosition bt = new CubePosition();

				int sX = vertCubePos.X == cubePos.X ? -1 : 1;
				int sY = vertCubePos.Y == cubePos.Y ? -1 : 1;
				int sZ = vertCubePos.Z == cubePos.Z ? -1 : 1;

				if (vertex.Normal.X != 0)
				{
					t = new CubePosition(0, sY, 0, CubePosition.CoordinateSpace.CubeSpace);
					bt = new CubePosition(0, 0, sZ, CubePosition.CoordinateSpace.CubeSpace);
				}
				else if (vertex.Normal.Y != 0)
				{
					t = new CubePosition(sX, 0, 0, CubePosition.CoordinateSpace.CubeSpace);
					bt = new CubePosition(0, 0, sZ, CubePosition.CoordinateSpace.CubeSpace);
				}
				else if (vertex.Normal.Z != 0)
				{
					t = new CubePosition(sX, 0, 0, CubePosition.CoordinateSpace.CubeSpace);
					bt = new CubePosition(0, sY, 0, CubePosition.CoordinateSpace.CubeSpace);
				}

				checkPositions[0] = nrm;
				checkPositions[1] = nrm + t + bt;
				checkPositions[2] = nrm + t;
				checkPositions[3] = nrm + bt;
				if (threaded)
					manager.ThreadedView.GetIds(checkPositions, checkIds);
				else manager.InitializerView.GetIds(checkPositions, checkIds);

				int top = checkIds[0];
				int corner = checkIds[1];
				int sideA = checkIds[2];
				int sideB = checkIds[3];

				if (corner > 0 && Main.Registry.CubeRegistry.noAo[corner])
					corner = 0;
				if (sideA > 0 && Main.Registry.CubeRegistry.noAo[sideA])
					sideA = 0;
				if (sideB > 0 && Main.Registry.CubeRegistry.noAo[sideB])
					sideB = 0;

				if (top > 0)
				{
					//nothing
				}
				else
				{
					float ao = 0;
					if (sideA > 0 && sideB > 0)
					{
						ao = 1;
					}
					else
					{
						// Only up to two of these will get hit
						if (corner > 0) ao++;
						if (sideA > 0) ao++;
						if (sideB > 0) ao++;

						ao /= 3;
					}

					vertex.AO = 1 - ao;
					vertices[i] = vertex;
				}
			}
		}
	}
}
