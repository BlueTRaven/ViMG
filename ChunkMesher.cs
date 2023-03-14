using BepuUtilities.Memory;
using BrUtility;
using BrUtility.Ported;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using SharpDX.Direct2D1.Effects;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ViMG.ChunkStuff;
using ViMG.Cubes;
using ViMG.Entities;

namespace ViMG
{
	public class ChunkMesher
	{
#if DEBUG
		private const int MAX_ACTIVE_MESH_BATCH_TASKS = 2;
		private const int MAX_CHUNKS_TO_MESH_PER_BATCH_TASK = 10;
#else
		private const int MAX_ACTIVE_MESH_BATCH_TASKS = 20;
		private const int MAX_CHUNKS_TO_MESH_PER_BATCH_TASK = 4;
#endif
		public const int NUM_CHUNK_MESH_PASSES = 5;

		//Represents a chunk mesh batch, including everything about a chunk that is necessary to mesh it, or to get the info required to do so.
		private struct RenderMeshBatch
		{
			public RenderMeshInfo[] meshInfos;
			public CopiedChunkData[] copies;
			public int num;

			public readonly bool isUsed;

			public RenderMeshBatch(RenderMeshInfo[] meshInfos, CopiedChunkData[] copies)
			{
				this.meshInfos = meshInfos;
				this.copies = copies;
				this.num = 0;

				isUsed = true;
			}
		}

		//The state of a chunk batch task state.
		private readonly struct BatchRenderMeshTaskState
		{
			public readonly RenderMeshBatch batch;
			public readonly ChunkMesher mesher;

			public BatchRenderMeshTaskState(RenderMeshBatch batch, ChunkMesher mesher)
			{
				this.batch = batch;
				this.mesher = mesher;
			}
		}

		//The result of a chunk batch task.
		private readonly struct BatchRenderMeshTaskResult
		{
			public readonly RenderMeshInfo[] meshInfos;
			public readonly CopiedChunkData[] copies;
			//number of meshes included in the batch
			public readonly int num;

			public BatchRenderMeshTaskResult(RenderMeshInfo[] meshInfos, CopiedChunkData[] copies, int num)
			{
				this.meshInfos = meshInfos;
				this.copies = copies;
				this.num = num;
			}
		}

		private struct RenderMeshInfo
		{
			public ChunkPosition position;
			public (VertexBuffer VBO, IndexBuffer IBO)[] meshes;
			public byte meshVersion; //mesh version; if different from version, needs to be re-meshed
			public byte version;

			//There's a difference between having existing meshes and needing to be remeshed (being dirty) and not having meshes at all.
			//Therefore this bool exists to determine if the given chunk has a mesh. If it doesn't, it isn't necessarily marked dirty,
			//it just needs a mesh to be created in the first place.
			public bool hasMeshes;

			public RenderMeshInfo(ChunkPosition position)
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

		//In terms of granularity, this system works like so:
		//Marking something dirty adds it to the next frame's batch. (Only one batch will be started a frame via this method).
		//Adding something to a batch allows it to be enqueued to the task queue. N amounts of chunks are allowed to in a batch at once.
		//This queue is unlimited in size, but does not start its tasks immediately.
		//Queued batch tasks are pulled off the queue (sorted by distance from the camera), are started, and then are added to the active tasks.
		//There may only be a certain amount of active tasks at once.
		//Active tasks are checked once per frame to see if their thread has finished. If it has, then all of its data is copied over to the main thread
		//Meshes are uploaded to the gpu, etc.
		//If a mesh is updated during the time it is in the queue or is active, then the thread is not stopped. Instead, it is run to completion
		//and the resulting mesh is immediately unloaded. The chunk may be re-added to the queue at any point in this process.
        private Queue<ChunkPosition> dirtyChunkPositions = new Queue<ChunkPosition>();
        private HashSet<ChunkPosition> dirtyChunkKnown = new HashSet<ChunkPosition>();

        private RenderMeshBatch currentBatch;
        private PriorityQueue<(RenderMeshBatch batch, Task<BatchRenderMeshTaskResult> task)> meshBatchTasksQueue = new PriorityQueue<(RenderMeshBatch batch, Task<BatchRenderMeshTaskResult> task)>(true, (x) =>
        {
            Vector3 avg = Vector3.Zero;

            for (int i = 0; i < MAX_CHUNKS_TO_MESH_PER_BATCH_TASK; i++)
                avg += x.batch.meshInfos[i].position.InWorldSpace();

            avg /= MAX_CHUNKS_TO_MESH_PER_BATCH_TASK;

            return (int)(Main.camera.Position - avg).Length();
        });
        //The 'active' batch mesh tasks.
        private Task<BatchRenderMeshTaskResult>[] activeMeshBatchTasks = new Task<BatchRenderMeshTaskResult>[MAX_ACTIVE_MESH_BATCH_TASKS];
		private int numActiveChunkMeshBatchTasks;

		private RenderMeshInfo[] chunkMeshInfos;

		public ChunkMesher(GraphicsDevice device, int sizeInChunks)
		{
			this.device = device;
			this.sizeInChunks = sizeInChunks;

			chunkMeshInfos = new RenderMeshInfo[sizeInChunks * sizeInChunks * sizeInChunks];
			for (int i = 0; i < sizeInChunks * sizeInChunks * sizeInChunks; i++)
			{
				Util.OneDToThreeD(i, new ValuePoint3D(sizeInChunks), out ValuePoint3D point);
				chunkMeshInfos[i] = new RenderMeshInfo(new ChunkPosition(point.x, point.y, point.z));
			}
		}

		public void Update(World world)
		{
			if (!currentBatch.isUsed)
				currentBatch = new RenderMeshBatch(new RenderMeshInfo[MAX_CHUNKS_TO_MESH_PER_BATCH_TASK], new CopiedChunkData[MAX_CHUNKS_TO_MESH_PER_BATCH_TASK]);

			if (currentBatch.num >= MAX_CHUNKS_TO_MESH_PER_BATCH_TASK)
			{
				EnqueueBatch(ref currentBatch);
				currentBatch = new RenderMeshBatch(new RenderMeshInfo[MAX_CHUNKS_TO_MESH_PER_BATCH_TASK], new CopiedChunkData[MAX_CHUNKS_TO_MESH_PER_BATCH_TASK]);
			}

			//Note that we only attempt to enqueue one batch per frame regardless of what MAX_MESH_PER_FRAME is.
			while (dirtyChunkPositions.Count > 0 && currentBatch.num < MAX_CHUNKS_TO_MESH_PER_BATCH_TASK)
			{
				ChunkPosition position = dirtyChunkPositions.Dequeue();
				dirtyChunkKnown.Remove(position);

				ref RenderMeshInfo c = ref GetChunkMeshInfo(position);

				if (c.version != c.meshVersion || !c.hasMeshes)
				{
					//place into the current batch to be meshed later.
					currentBatch.meshInfos[currentBatch.num] = c;
					currentBatch.copies[currentBatch.num] = CopiedChunkPool.MakeCopy(world, position);
					currentBatch.num++;
				}
			}

			//we want to make sure to flush this regardless of whether or not we've actually filled it fully
			//As there might be frames where we don't fully fill it, in which case it could wait a potentially arbitrary amount of time.
			if (currentBatch.num > 0)
			{
				EnqueueBatch(ref currentBatch);
				currentBatch = new RenderMeshBatch(new RenderMeshInfo[MAX_CHUNKS_TO_MESH_PER_BATCH_TASK], new CopiedChunkData[MAX_CHUNKS_TO_MESH_PER_BATCH_TASK]);
			}

			StartActiveTasks(world);
		}

		//Flushes all actively enqueued chunks, blocking until they have all been meshed.
		public void Flush(World world)
		{
			Queue<Task<BatchRenderMeshTaskResult>> tasks = new Queue<Task<BatchRenderMeshTaskResult>>();

			EnqueueBatch(ref currentBatch);
			currentBatch = new RenderMeshBatch(new RenderMeshInfo[MAX_CHUNKS_TO_MESH_PER_BATCH_TASK], new CopiedChunkData[MAX_CHUNKS_TO_MESH_PER_BATCH_TASK]);

			int max = meshBatchTasksQueue.Count;
			world.GameStateManager.TheIsland.ProgressMax = max;

			while (meshBatchTasksQueue.Count > 0)
			{
				world.GameStateManager.TheIsland.ProgressMin = max - meshBatchTasksQueue.Count;

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
			world.GameStateManager.TheIsland.ProgressMax = max;

			while (tasks.Count > 0)
			{
				world.GameStateManager.TheIsland.ProgressMin = max - tasks.Count;

				var task = tasks.Dequeue();

				if (task.IsCompleted)
				{
					if (!task.IsCompletedSuccessfully)
						throw new Exception("???");

                    BatchRenderMeshTaskResult batchResult = task.Result;

					for (int j = 0; j < batchResult.num; j++)
					{
						batchResult.copies[j].Return();
						RenderMeshInfo meshResult = batchResult.meshInfos[j];

						ref RenderMeshInfo c = ref GetChunkMeshInfo(meshResult.position);

						if (meshResult.version >= c.version)
						{
							//Unload the old mesh now
							Unload(ref c);

							c.meshVersion = c.version;

							c.meshes = meshResult.meshes;
						}
						else
						{
							//version has changed while we're meshing - discard the old mesh, as a new one should already be queued.
							Unload(ref meshResult);
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

						RenderMeshInfo meshResult = batchResult.meshInfos[j];

						ref RenderMeshInfo c = ref GetChunkMeshInfo(meshResult.position);

						if (meshResult.version >= c.version)
						{
							//Unload the old mesh now
							Unload(ref c);

							c.meshVersion = c.version;

							c.meshes = meshResult.meshes;
						}
						else
						{
							//version has changed while we're meshing - discard the old mesh, as a new one should already be queued.
							Unload(ref meshResult);
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
				currentBatch = new RenderMeshBatch(new RenderMeshInfo[MAX_CHUNKS_TO_MESH_PER_BATCH_TASK], new CopiedChunkData[MAX_CHUNKS_TO_MESH_PER_BATCH_TASK]);

			if (currentBatch.num >= MAX_CHUNKS_TO_MESH_PER_BATCH_TASK)
			{
				EnqueueBatch(ref currentBatch);
				currentBatch = new RenderMeshBatch(new RenderMeshInfo[MAX_CHUNKS_TO_MESH_PER_BATCH_TASK], new CopiedChunkData[MAX_CHUNKS_TO_MESH_PER_BATCH_TASK]);
			}

			ref RenderMeshInfo c = ref GetChunkMeshInfo(position);

			if (c.version != c.meshVersion || !c.hasMeshes)
			{
				currentBatch.meshInfos[currentBatch.num] = c;
				currentBatch.copies[currentBatch.num] = CopiedChunkPool.MakeCopy(world, position);
				currentBatch.num++;
			}
		}

		private void EnqueueBatch(ref RenderMeshBatch batch)
		{
			Task<BatchRenderMeshTaskResult> task = new Task<BatchRenderMeshTaskResult>(MeshBatchFn, new BatchRenderMeshTaskState(batch, this));

			meshBatchTasksQueue.EnqueueWithoutSorting((batch, task));
		}

		private static BatchRenderMeshTaskResult MeshBatchFn(object obj)
		{
			BatchRenderMeshTaskState state = (BatchRenderMeshTaskState)obj;

			Span<CubePosition> positions = stackalloc CubePosition[Chunk.CHUNK_SIZE * Chunk.CHUNK_SIZE * Chunk.CHUNK_SIZE];
			Span<MeshHelper.CubeFace> faces = stackalloc MeshHelper.CubeFace[Chunk.CHUNK_SIZE * Chunk.CHUNK_SIZE * Chunk.CHUNK_SIZE];

			for (int i = 0; i < state.batch.num; i++)
			{
				RenderMeshInfo cmi = state.batch.meshInfos[i];
				for (int x = 0; x < Chunk.CHUNK_SIZE; x++)
				{
					for (int y = 0; y < Chunk.CHUNK_SIZE; y++)
					{
						for (int z = 0; z < Chunk.CHUNK_SIZE; z++)
						{
							CubePosition pos = new CubePosition(x, y, z, CubePosition.CoordinateSpace.ChunkSpace);
							Util.ThreeDToOneD(new ValuePoint3D(x, y, z), new ValuePoint3D(Chunk.CHUNK_SIZE), out int j);
							//pos = pos.InCubeSpace(cmi.position);

							positions[j] = pos;
						}
					}
				}

				state.batch.copies[i].GetFaces(positions, faces);

				cmi.meshes = new (VertexBuffer VBO, IndexBuffer IBO)[NUM_CHUNK_MESH_PASSES];

				(List<VertexCube> verts, List<int> indices) opaques = state.mesher.GenerateChunk(in state.batch.copies[i], faces, cmi.position, Cube.RenderPass.Opaque);

				cmi.meshes[(int)Cube.RenderPass.Opaque] = MeshHelper.MakeSimplerMesh(state.mesher.device, opaques);
				cmi.meshes[(int)Cube.RenderPass.Transparent] = MeshHelper.MakeSimplerMesh(state.mesher.device, state.mesher.GenerateChunk(
					in state.batch.copies[i], faces, cmi.position, Cube.RenderPass.Transparent));
				cmi.meshes[(int)Cube.RenderPass.DepthOnly] = MeshHelper.MakeSimplerMesh(state.mesher.device, state.mesher.GenerateChunk(
					in state.batch.copies[i], faces, cmi.position, Cube.RenderPass.DepthOnly));
				cmi.meshes[(int)Cube.RenderPass.Fluid] = (null, null);   //TODO fluids?
				cmi.meshes[(int)Cube.RenderPass.Air] = MeshHelper.MakeSimplerMesh(state.mesher.device, state.mesher.GenerateChunk(
					in state.batch.copies[i], faces, cmi.position, Cube.RenderPass.Air));

				state.batch.meshInfos[i] = cmi;
				state.batch.meshInfos[i].hasMeshes = true;
			}

			return new BatchRenderMeshTaskResult(state.batch.meshInfos, state.batch.copies, state.batch.num);
		}

		private void Unload(ref RenderMeshInfo c)
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

		public void Unload(ChunkPosition position)
		{
			Unload(ref GetChunkMeshInfo(position));
		}

		public void UnloadAll()
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

		private ref RenderMeshInfo GetChunkMeshInfo(ChunkPosition pos)
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
			//position in copied chunk space
			public CubePosition position;
			//position in cube space
			public CubePosition positionCS;
			//position in world space
			public Vector3 positionWS;
			public ushort id;
			public Cube cube;
			public MeshHelper.CubeFace faces;
		}

		public (List<VertexCube> vertices, List<int> indices) GenerateChunk(in CopiedChunkData data, Span<MeshHelper.CubeFace> faces, ChunkPosition position, Cube.RenderPass pass)
		{
			Vector3 n = new Vector3(0);
			Vector3 f = new Vector3(Cube.CUBE_SCALE);

			List<VertexCube> vertices = new List<VertexCube>();
			List<int> indices = new List<int>();

			for (int x = 0; x < Chunk.CHUNK_SIZE; x++)
			{
				for (int y = 0; y < Chunk.CHUNK_SIZE; y++)
				{
					for (int z = 0; z < Chunk.CHUNK_SIZE; z++)
					{
						CubePosition cubePosition = new CubePosition(x, y, z);
						Util.ThreeDToOneD(new ValuePoint3D(x, y, z), new ValuePoint3D(Chunk.CHUNK_SIZE), out int i);

						ushort id = data.GetId(cubePosition);
						MeshHelper.CubeFace renderingFaces = faces[i];

						if (pass == Cube.RenderPass.Transparent || pass == Cube.RenderPass.Opaque || pass == Cube.RenderPass.Fluid || pass == Cube.RenderPass.DepthOnly)
						{
							//if we're air or have no faces, ignore this cube.
							if (id == 0 || renderingFaces == MeshHelper.CubeFace.NONE)
								continue;

							Cube cube = Main.Registry.CubeRegistry.Get(id);

							if (cube.ShouldMeshPass(pass))
							{
								CubeMeshingParameters parameters = new CubeMeshingParameters()
								{
									cube = cube,
									id = id,
									positionWS = (data.BasePosition + cubePosition).InWorldSpace(),
									positionCS = data.BasePosition + cubePosition,
									position = cubePosition,
									faces = renderingFaces
								};

								int oldCount = vertices.Count;

								cube.MakeCubeVerts(pass, data, parameters, vertices, indices);

								int count = vertices.Count - oldCount;

								BakeAO(data, cubePosition, oldCount, oldCount + count, vertices);
							}
						}
						else if (pass == Cube.RenderPass.Air)
						{
							//Note that for air, we we do still make verts if id is 0 (though still not if no faces).
							if (id != 0 || renderingFaces == MeshHelper.CubeFace.NONE)
								continue;

							CubeMeshingParameters parameters = new CubeMeshingParameters()
							{
								cube = Main.Registry.CubeRegistry.Air,
								id = id,
								positionWS = (data.BasePosition + cubePosition).InWorldSpace(),
								positionCS = data.BasePosition + cubePosition,
								position = cubePosition,
								faces = renderingFaces
							};

							Main.Registry.CubeRegistry.Air.MakeCubeVerts(pass, data, parameters, vertices, indices);
						}
					}
				}
			}

			return (vertices, indices);
		}

		private static void BakeAO(CopiedChunkData data, CubePosition pos, int start, int end, List<VertexCube> vertices, bool threaded = false)
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
					cubePos.Z + (int)vertex.Normal.Z);

				CubePosition t = new CubePosition();
				CubePosition bt = new CubePosition();

				int sX = vertCubePos.X == cubePos.X ? -1 : 1;
				int sY = vertCubePos.Y == cubePos.Y ? -1 : 1;
				int sZ = vertCubePos.Z == cubePos.Z ? -1 : 1;

				if (vertex.Normal.X != 0)
				{
					t = new CubePosition(0, sY, 0);
					bt = new CubePosition(0, 0, sZ);
				}
				else if (vertex.Normal.Y != 0)
				{
					t = new CubePosition(sX, 0, 0);
					bt = new CubePosition(0, 0, sZ);
				}
				else if (vertex.Normal.Z != 0)
				{
					t = new CubePosition(sX, 0, 0);
					bt = new CubePosition(0, sY, 0);
				}

				checkPositions[0] = nrm;
				checkPositions[1] = nrm + t + bt;
				checkPositions[2] = nrm + t;
				checkPositions[3] = nrm + bt;

				data.GetIds(checkPositions, checkIds);

				int top = checkIds[0];
				int corner = checkIds[1];
				int sideA = checkIds[2];
				int sideB = checkIds[3];

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
