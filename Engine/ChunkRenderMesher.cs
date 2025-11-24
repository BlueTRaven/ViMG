using BepuUtilities.Memory;
using BrUtility;
using BrUtility.Ported;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using SharpDX.Direct2D1.Effects;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ViMG.ChunkStuff;
using ViMG.Cubes;
using ViMG.Entities;
using ViMG.GameStates;
using ViMG.Rendering;
using ViMG.VertexDeclarations;

namespace ViMG
{
	public class ChunkRenderMesher
	{
#if DEBUG
		private const int MAX_ACTIVE_MESH_BATCH_TASKS = 20;
		private const int MAX_CHUNKS_TO_MESH_PER_BATCH_TASK = 20;
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
			public readonly ChunkRenderMesher mesher;

			public BatchRenderMeshTaskState(RenderMeshBatch batch, ChunkRenderMesher mesher)
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
			public VerySimpleMesh[] meshes;
			public byte meshVersion; //mesh version; if different from version, needs to be re-meshed
			public byte version;

			//There's a difference between having existing meshes and needing to be remeshed (being dirty) and not having meshes at all.
			//Therefore this bool exists to determine if the given chunk has a mesh. If it doesn't, it isn't necessarily marked dirty,
			//it just needs a mesh to be created in the first place.
			public bool hasMeshes;

			public RenderMeshInfo(ChunkPosition position)
			{
				this.position = position;
				meshes = new VerySimpleMesh[NUM_CHUNK_MESH_PASSES];
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

		private Queue<Task<BatchRenderMeshTaskResult>> flushTaskQueue = new Queue<Task<BatchRenderMeshTaskResult>>();

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

		private BufferPool bufferPool;

		private RenderMeshInfo[] chunkMeshInfos;

		public ChunkRenderMesher(GraphicsDevice device, int sizeInChunks, BufferPool bufferPool)
		{
			this.device = device;
			this.sizeInChunks = sizeInChunks;

			chunkMeshInfos = new RenderMeshInfo[sizeInChunks * sizeInChunks * sizeInChunks];
			for (int i = 0; i < sizeInChunks * sizeInChunks * sizeInChunks; i++)
			{
				Util.OneDToThreeD(i, new ValuePoint3D(sizeInChunks), out ValuePoint3D point);
				chunkMeshInfos[i] = new RenderMeshInfo(new ChunkPosition(point.x, point.y, point.z));
			}

			this.bufferPool = bufferPool;
		}

		public void Update(World world)
		{
            using var zone = TracyImpl.Tracy.BeginZone();

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
					currentBatch.copies[currentBatch.num] = CopiedChunkPool.MakeCopy(world, bufferPool, position);
					currentBatch.copies[currentBatch.num].refcount += 1;
                    currentBatch.copies[currentBatch.num].render = true;
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

		public bool WorkFinished()
		{
            return activeMeshBatchTasks.Length == 0 && flushTaskQueue.Count == 0;
        }

		public void BeginFlush(World world)
		{
            using var zone = TracyImpl.Tracy.BeginZone();

            EnqueueBatch(ref currentBatch);
			currentBatch = new RenderMeshBatch(new RenderMeshInfo[MAX_CHUNKS_TO_MESH_PER_BATCH_TASK], new CopiedChunkData[MAX_CHUNKS_TO_MESH_PER_BATCH_TASK]);

			//while (meshBatchTasksQueue.Count > 0 || numActiveChunkMeshBatchTasks > 0)
			//{
			//	StartActiveTasks(world);
			//}

			while (meshBatchTasksQueue.Count > 0)
			{
				var task = meshBatchTasksQueue.Dequeue().task;

				if (task.Status == TaskStatus.Created)
				{
					if (Main.MULTITHREAD_MESHING)
						task.Start();
					else task.RunSynchronously();
				}
				flushTaskQueue.Enqueue(task);
			}
		}

		//Flushes all actively enqueued chunks, blocking until they have all been meshed.
		public void FinishFlush()
		{
			//return;
            using var zone = TracyImpl.Tracy.BeginZone();
			
            int max = flushTaskQueue.Count;
			GameStateTheIsland.ProgressMax = max;

			while (flushTaskQueue.Count > 0)
			{
				GameStateTheIsland.ProgressMin = max - flushTaskQueue.Count;

				var task = flushTaskQueue.Dequeue();

				if (task.IsCompleted)
				{
					if (!task.IsCompletedSuccessfully)
						throw new Exception("???");

					BatchRenderMeshTaskResult batchResult = task.Result;

					for (int j = 0; j < batchResult.num; j++)
					{
						lock (bufferPool)
						{
							batchResult.copies[j].Return(bufferPool);
							batchResult.copies[j].render = false;
                        }

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
				//if a task is not finished, re-enqueue it at the back of the queue.
				else flushTaskQueue.Enqueue(task);
			}
		}

		private void StartActiveTasks(World world)
		{
			using var zone = TracyImpl.Tracy.BeginZone();

            //First, check for complete tasks.
            for (int i = 0; i < activeMeshBatchTasks.Length; i++)
			{
				if (activeMeshBatchTasks[i] != null && activeMeshBatchTasks[i].IsCompleted)
				{
                    using var zoneActive = TracyImpl.Tracy.BeginZone(name: "EndBatch");

					var task = activeMeshBatchTasks[i];

					if (!task.IsCompletedSuccessfully)
						throw new Exception("???");

					var batchResult = task.Result;

					for (int j = 0; j < batchResult.num; j++)
					{
						lock (bufferPool)
						{
                            //using var zoneLock = TracyImpl.Tracy.BeginZone(name: "Lock");
                            batchResult.copies[j].Return(bufferPool);
							batchResult.copies[j].render = false;
						}

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
                    
					numActiveChunkMeshBatchTasks--;
                    activeMeshBatchTasks[i] = null;
				}

				if (activeMeshBatchTasks[i] == null && meshBatchTasksQueue.Count > 0)
				{
					using var zoneStart = TracyImpl.Tracy.BeginZone(name: "StartBatch");

					meshBatchTasksQueue.Sort();
					var task = meshBatchTasksQueue.Dequeue();
					activeMeshBatchTasks[i] = task.task;
					numActiveChunkMeshBatchTasks++;

					Debug.Assert(task.task.Status == TaskStatus.Created);

					if (Main.MULTITHREAD_MESHING)
						task.task.Start();
					else task.task.RunSynchronously();
				}
			}
		}

		//Adds a position in the current batch. 
		public bool AddToNextBatch(World world, ChunkPosition position, CopiedChunkData copy)
		{
            using var zone = TracyImpl.Tracy.BeginZone();

			copy.refcount += 1;
			copy.render = true;

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
				currentBatch.copies[currentBatch.num] = copy;//CopiedChunkPool.MakeCopy(world, bufferPool, position);
                currentBatch.num++;
				return true;
			} 
			else
			{
				lock (bufferPool)
				{
					copy.Return(bufferPool);
				}
				return false;
			}
		}

		private void EnqueueBatch(ref RenderMeshBatch batch)
		{
			Task<BatchRenderMeshTaskResult> task = new Task<BatchRenderMeshTaskResult>(MeshBatchFn, new BatchRenderMeshTaskState(batch, this));

			meshBatchTasksQueue.EnqueueWithoutSorting((batch, task));
		}

		public void ImmediatelyMesh(World world, ChunkPosition position)
		{
			var batch = new RenderMeshBatch(new RenderMeshInfo[1], new CopiedChunkData[1]);
            ref RenderMeshInfo meshInfo = ref GetChunkMeshInfo(position);
			batch.meshInfos[0] = meshInfo;
			batch.copies[0] = CopiedChunkPool.MakeCopy(world, bufferPool, position);
			batch.copies[0].refcount += 1;
			batch.copies[0].render = true;
			batch.num = 1;

            var batchState = new BatchRenderMeshTaskState(batch, this);

            BatchRenderMeshTaskResult result = MeshBatchFn(batchState);

            meshInfo.meshVersion = result.meshInfos[0].meshVersion;

            meshInfo.meshes = result.meshInfos[0].meshes;

			batch.copies[0].Return(bufferPool);
        }

		private static unsafe BatchRenderMeshTaskResult MeshBatchFn(object obj)
		{
            using var zone = TracyImpl.Tracy.BeginZone();

            BatchRenderMeshTaskState state = (BatchRenderMeshTaskState)obj;

			Span<CubePosition> positions = stackalloc CubePosition[Chunk.CHUNK_SIZE * Chunk.CHUNK_SIZE * Chunk.CHUNK_SIZE];
			Span<MeshHelper.CubeFace> faces = stackalloc MeshHelper.CubeFace[Chunk.CHUNK_SIZE * Chunk.CHUNK_SIZE * Chunk.CHUNK_SIZE];

			for (int i = 0; i < state.batch.num; i++)
			{
				RenderMeshInfo cmi = state.batch.meshInfos[i];

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

				cmi.meshes = new VerySimpleMesh[NUM_CHUNK_MESH_PASSES];

                VertexAttributes opaques = state.mesher.GenerateChunk(in state.batch.copies[i], faces, cmi.position, Cube.RenderPass.Opaque, 0);
                VertexAttributes transparents = state.mesher.GenerateChunk(
					in state.batch.copies[i], faces, cmi.position, Cube.RenderPass.Transparent, 0);
                VertexAttributes shadows = state.mesher.GenerateChunk(
					in state.batch.copies[i], faces, cmi.position, Cube.RenderPass.DepthOnly, 0);
                VertexAttributes empties = state.mesher.GenerateChunk(
					in state.batch.copies[i], faces, cmi.position, Cube.RenderPass.Air, 0);

				cmi.meshes[(int)Cube.RenderPass.Opaque] = VerySimpleMesh.Opaque(state.mesher.device, opaques, false);
				//MeshHelper.MakeSimplerMesh(state.mesher.device,
				//opaques.verts.ToVertexOpaquePass(), opaques.indices, false);    //opaque meshes bake their own tangents
				cmi.meshes[(int)Cube.RenderPass.Transparent] = VerySimpleMesh.Transparent(state.mesher.device, transparents);
				//MeshHelper.MakeSimplerMesh(state.mesher.device,
				//transparents.verts.ToVertexTransparentPass(), transparents.indices);
				cmi.meshes[(int)Cube.RenderPass.DepthOnly] = VerySimpleMesh.Shadow(state.mesher.device, shadows);
				//MeshHelper.MakeSimplerMesh(state.mesher.device,
				//shadows.verts.ToVertexShadowPass(), shadows.indices);
				cmi.meshes[(int)Cube.RenderPass.Fluid] = new VerySimpleMesh();
				// (null, null);   //TODO fluids?
				cmi.meshes[(int)Cube.RenderPass.Air] = VerySimpleMesh.SolidColor(state.mesher.device, empties);
					//MeshHelper.MakeSimplerMesh(state.mesher.device,
					//empties.verts.ToVertexEmptyPass(), empties.indices);

				state.batch.meshInfos[i] = cmi;
				state.batch.meshInfos[i].hasMeshes = true;
			}

			return new BatchRenderMeshTaskResult(state.batch.meshInfos, state.batch.copies, state.batch.num);
		}

		private void Unload(ref RenderMeshInfo c)
		{
			for (int i = 0; i < NUM_CHUNK_MESH_PASSES; i++)
			{
				c.meshes[i].Dispose();
				c.meshes[i] = new VerySimpleMesh();
				//if (c.meshes[i].VBO != null)
				//{
				//	c.meshes[i].VBO.Dispose();
				//	c.meshes[i].IBO.Dispose();

				//	c.meshes[i] = (null, null);
				//}

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
					VerySimpleMesh mesh = chunkMeshInfos[j].meshes[k];
                    chunkMeshInfos[j].meshes[k].Dispose();
					chunkMeshInfos[j].meshes[k] = new VerySimpleMesh();

					//if (mesh.VBO != null)
					//{
					//	mesh.VBO.Dispose();
					//	mesh.IBO.Dispose();

					//	chunkMeshInfos[j].meshes[k] = (null, null);
					//}
				}

				chunkMeshInfos[j].hasMeshes = false;
			}

			bufferPool.AssertEmpty();
			bufferPool.Clear();
		}

		public bool MarkDirty(ChunkPosition position)
		{
			GetChunkMeshInfo(position).version++;

			if (!dirtyChunkKnown.Contains(position))
			{
				dirtyChunkPositions.Enqueue(position);
				dirtyChunkKnown.Add(position);

				return true;
			}

			return false;
		}

		public bool IsMeshed(ChunkPosition position)
		{
			//we know we're not meshing this chunk currently if meshVersion is equal to version.
			return GetChunkMeshInfo(position).meshVersion == GetChunkMeshInfo(position).version;
		}

		public VerySimpleMesh GetMesh(ChunkPosition position, Cube.RenderPass pass)
		{
            VerySimpleMesh mesh = GetChunkMeshInfo(position).meshes[(int)pass];

			//if (mesh.VBO != null && mesh.VBO.IsDisposed)
				//throw new Exception("??");

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

			public Vector3 GetTangent()
			{
				return Vector3.Normalize(b - a);
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

		public ref struct VertexAttributes
		{
			public Optional<FastList<Vector3>> position;
			public Optional<FastList<Color>> color;
			public Optional<FastList<Vector2>> texCoord;
			public Optional<FastList<VertexNormal>> normal;
			public Optional<FastList<float>> ao;

			public Optional<FastList<VertexAnimated>> animation;

			public List<int> indices;

			public static VertexAttributes Transparent(FastList<VertexCube> vertices, List<int> indices)
			{
                FastList<Vector3> positions = new FastList<Vector3>(vertices.Length);
                FastList<Color> colors = new FastList<Color>(vertices.Length);
                FastList<Vector2> texCoords = new FastList<Vector2>(vertices.Length);

				for (int i = 0; i < vertices.Length; i++)
				{
					var vertex = vertices.Buffer[i];

					positions.Add(vertex.Position);
					colors.Add(vertex.Color);
					texCoords.Add(vertex.TextureCoordinate);
				}

				return new VertexAttributes
				{
					position = new(positions),
					color = new(colors),
					texCoord = new(texCoords),
					indices = indices,
				};
            }

			public VertexAttributes(FastList<VertexCube> vertices, List<int> indices)
			{
				FastList<Vector3> positions = new FastList<Vector3>(vertices.Length);
				FastList<Color> colors = new FastList<Color>(vertices.Length);
				FastList<Vector2> texCoords = new FastList<Vector2>(vertices.Length);
                FastList<VertexNormal> normals = new FastList<VertexNormal>(vertices.Length);
                FastList<float> aos = new FastList<float>(vertices.Length);
                FastList<VertexAnimated> animations = new FastList<VertexAnimated>(vertices.Length);

				for (int i = 0; i < vertices.Length; i++)
				{
					var vertex = vertices.Buffer[i];

					positions.Add(vertex.Position);
					colors.Add(vertex.Color);
					texCoords.Add(vertex.TextureCoordinate);
					normals.Add(new VertexNormal()
					{
						Normal = vertex.Normal,
						Tangent = vertex.Tangent,
						Bitangent = vertex.Bitangent,
					});
					aos.Add(vertex.AO);
					animations.Add(new VertexAnimated
					{
						AnimFrameSize = vertex.AnimFrameSize,
						AnimFrameTime = vertex.AnimFrameTime,
						NumAnimFrames = vertex.NumAnimFrames,
					});
				}

				position = new(positions);
				color = new(colors);
				texCoord = new(texCoords);
				normal = new(normals);
				ao = new(aos);
				animation = new(animations);
				this.indices = indices;
			}
		}

		public VertexAttributes GenerateChunk(in CopiedChunkData data, Span<MeshHelper.CubeFace> faces, ChunkPosition position, Cube.RenderPass pass, int dummy)
		{
            using var zone = TracyImpl.Tracy.BeginZone();

            Vector3 n = new Vector3(0);
            Vector3 f = new Vector3(Cube.CUBE_SCALE);

            FastList<VertexCube> vertices = new FastList<VertexCube>(4);
            List<int> indices = new List<int>();
			int iter = 0;

			VertexAttributes attributes = new VertexAttributes();

			switch (pass)
			{
				case Cube.RenderPass.Opaque:
				case Cube.RenderPass.Transparent:
				case Cube.RenderPass.Fluid:
					attributes.position = new(new FastList<Vector3>());
					attributes.color = new(new FastList<Color>());
					attributes.texCoord = new(new FastList<Vector2>());
					attributes.normal = new(new FastList<VertexNormal>());
					attributes.ao = new(new FastList<float>());

					attributes.animation = new(new FastList<VertexAnimated>());
					break;
				case Cube.RenderPass.DepthOnly:
					attributes.position = new(new FastList<Vector3>());
					attributes.texCoord = new(new FastList<Vector2>());
					break;

				default:
					break;
			}

			int vertexCount = 0;

            for (int z = 0; z < Chunk.CHUNK_SIZE; z++)
            {
                for (int y = 0; y < Chunk.CHUNK_SIZE; y++)
                {
                    for (int x = 0; x < Chunk.CHUNK_SIZE; x++)
                    {
                        CubePosition cubePosition = new CubePosition(x, y, z, CubePosition.CoordinateSpace.ChunkSpace);
                        Util.ThreeDToOneD(new ValuePoint3D(x, y, z), new ValuePoint3D(Chunk.CHUNK_SIZE), out int i);

                        ushort id = data.GetId(cubePosition);
                        MeshHelper.CubeFace renderingFaces = faces[i];

                        int oldIndices = indices.Count;

                        if (pass == Cube.RenderPass.Transparent || pass == Cube.RenderPass.Opaque || pass == Cube.RenderPass.Fluid || pass == Cube.RenderPass.DepthOnly)
                        {
                            //if we're air or have no faces, ignore this cube.
                            if (id == 0 || renderingFaces == MeshHelper.CubeFace.NONE)
                                continue;

                            Cube cube = Main.Registry.CubeRegistry.Get(id);

                            if (cube.ShouldMeshPass(pass))
                            {
								CubePosition positionCS = data.BasePosition + new CubePosition(cubePosition, CubePosition.CoordinateSpace.CubeSpace);
                                CubeMeshingParameters parameters = new CubeMeshingParameters()
                                {
                                    cube = cube,
                                    id = id,
                                    positionWS = positionCS.InWorldSpace(),
                                    positionCS = positionCS,
                                    position = cubePosition,
                                    faces = renderingFaces
                                };


                                cube.MakeCubeVerts(pass, data, parameters, vertices, indices, vertexCount);

                                if (vertices.Length % 4 != 0 || indices.Count % 6 != 0)
                                    throw new Exception("Invalid mesh! Must be defined as quads and not some other structure!");

                                MeshHelper.BakeTangents(0, vertices.Length, vertices);
                                BakeAO(data, cubePosition, 0, vertices.Length, vertices);
                            }
                        }
                        else if (pass == Cube.RenderPass.Air)
                        {
                            //Note that for air, we we do still make verts if id is 0 (though still not if no faces).
                            if (id != 0 || renderingFaces == MeshHelper.CubeFace.NONE)
                                continue;

                            CubePosition positionCS = data.BasePosition + new CubePosition(cubePosition, CubePosition.CoordinateSpace.CubeSpace);
                            CubeMeshingParameters parameters = new CubeMeshingParameters()
                            {
                                cube = Main.Registry.CubeRegistry.Air,
                                id = id,
                                positionWS = positionCS.InWorldSpace(),
                                positionCS = positionCS,
                                position = cubePosition,
                                faces = renderingFaces
                            };

                            Main.Registry.CubeRegistry.Air.MakeCubeVerts(pass, data, parameters, vertices, indices, vertexCount);
                        }

						using (var zoneCopy = TracyImpl.Tracy.BeginZone(name: "Copy")) 
						{
							for (int j = 0; j < vertices.Length; j++)
							{
								if (attributes.position.GetOut(out var positions))
									positions.Add(vertices[j].Position);
								if (attributes.color.GetOut(out var colors))
									colors.Add(vertices[j].Color);
								if (attributes.texCoord.GetOut(out var texCoords))
									texCoords.Add(vertices[j].TextureCoordinate);
								if (attributes.normal.GetOut(out var normals))
									normals.Add(new VertexNormal
									{
										Normal = vertices[j].Normal,
										Tangent = vertices[j].Tangent,
										Bitangent = vertices[j].Bitangent,
									});
								if (attributes.ao.GetOut(out var aos))
									aos.Add(vertices[j].AO);
								if (attributes.animation.GetOut(out var animations))
									animations.Add(new VertexAnimated()
									{
										AnimFrameSize = vertices[j].AnimFrameSize,
										AnimFrameTime = vertices[j].AnimFrameTime,
										NumAnimFrames = vertices[j].NumAnimFrames,
									});
							}
						}

                        vertexCount += vertices.Length;
                        iter++;

                        vertices.Clear();
                    }
                }
            }

			attributes.indices = indices;

			return attributes;
        }

		public (FastList<VertexCube> vertices, List<int> indices) GenerateChunk(in CopiedChunkData data, Span<MeshHelper.CubeFace> faces, ChunkPosition position, Cube.RenderPass pass)
		{
			Vector3 n = new Vector3(0);
			Vector3 f = new Vector3(Cube.CUBE_SCALE);

            FastList<VertexCube> vertices = new FastList<VertexCube>();
			List<int> indices = new List<int>();

			for (int z = 0; z < Chunk.CHUNK_SIZE; z++)
			{
				for (int y = 0; y < Chunk.CHUNK_SIZE; y++)
				{
					for (int x = 0; x < Chunk.CHUNK_SIZE; x++)
					{
						CubePosition cubePosition = new CubePosition(x, y, z, CubePosition.CoordinateSpace.ChunkSpace);
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
                                CubePosition positionCS = data.BasePosition + new CubePosition(cubePosition, CubePosition.CoordinateSpace.CubeSpace);

                                CubeMeshingParameters parameters = new CubeMeshingParameters()
								{
									cube = cube,
									id = id,
									positionWS = positionCS.InWorldSpace(),
									positionCS = positionCS,
									position = cubePosition,
									faces = renderingFaces
								};

								int oldCount = vertices.Length;

								cube.MakeCubeVerts(pass, data, parameters, vertices, indices);

								if (vertices.Length % 4 != 0)
									throw new Exception("Invalid mesh! Must be defined as quads and not some other structure!");

								int count = vertices.Length - oldCount;

								MeshHelper.BakeTangents(oldCount, oldCount + count, vertices);
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

		private static unsafe void BakeAO(CopiedChunkData data, CubePosition cubePosition, int start, int end, FastList<VertexCube> vertices)
		{
            using var zone = TracyImpl.Tracy.BeginZone();

            Span<CubePosition> checkPositions = stackalloc CubePosition[4];
			Span<ushort> checkIds = stackalloc ushort[4];

			fixed (VertexCube* verticesPtr = vertices.Buffer)
			{
				for (int i = start; i < end; i++)
				{
					ref VertexCube vertex = ref verticesPtr[i];

					//pc =
					CubePosition vertCubePos = CubePosition.FromWorldSpace(vertex.Position).InChunkSpace();

					CubePosition nrm = new CubePosition(cubePosition.X + (int)vertex.Normal.X,
						cubePosition.Y + (int)vertex.Normal.Y,
						cubePosition.Z + (int)vertex.Normal.Z, CubePosition.CoordinateSpace.ChunkSpace);

					CubePosition t = new CubePosition();
					CubePosition bt = new CubePosition();

                    int sX = vertCubePos.X == cubePosition.X ? -1 : 1;
					int sY = vertCubePos.Y == cubePosition.Y ? -1 : 1;
					int sZ = vertCubePos.Z == cubePosition.Z ? -1 : 1;

					if (vertex.Normal.X != 0)
					{
						t = new CubePosition(0, sY, 0, CubePosition.CoordinateSpace.ChunkSpace);
						bt = new CubePosition(0, 0, sZ, CubePosition.CoordinateSpace.ChunkSpace);
					}
					else if (vertex.Normal.Y != 0)
					{
						t = new CubePosition(sX, 0, 0, CubePosition.CoordinateSpace.ChunkSpace);
						bt = new CubePosition(0, 0, sZ, CubePosition.CoordinateSpace.ChunkSpace);
					}
					else if (vertex.Normal.Z != 0)
					{
						t = new CubePosition(sX, 0, 0, CubePosition.CoordinateSpace.ChunkSpace);
						bt = new CubePosition(0, sY, 0, CubePosition.CoordinateSpace.ChunkSpace);
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
							if (corner > 0)
								ao++;
							if (sideA > 0)
								ao++;
							if (sideB > 0)
								ao++;

							ao /= 3;
						}

						vertex.AO = 1 - ao;
						//vertices.Buffer[i] = vertex;
					}
				}

				//We always want a triangle touching both two side blocks contributing to AO.
				//Sometimes we may have:
				//c2222	  2222c
				//1a--b	  a--b1
				//1|\ |	  |\ |1
				//1| \|	  | \|1
				//1c--d	  c--d1
				//Where 1 and 2 are the two contributing sides. In certain orientations these sides do not have the same triangle touching both side's faces
				//which can produce odd AO results.
				//Instead we always want:
				//c2222	  2222c
				//1b--c	  a--b1
				//1| /|	  |\ |1
				//1|/ |	  | \|1
				//1d--a	  c--d1
				//We can accomplish this by checking the AO of our quad. Vertices c and b or a and d should have equivalent AO. If they don't, rotate the quad by 90 degrees.
				for (int i = start; i < end; i += 4)
				{
					var vertex00 = vertices[i + 0];
					var vertex10 = vertices[i + 1];
					var vertex11 = vertices[i + 2];
					var vertex01 = vertices[i + 3];

					if (vertex00.AO + vertex11.AO > vertex01.AO + vertex10.AO)
					{
						vertices.Buffer[i + 0] = vertex10;
						vertices.Buffer[i + 1] = vertex11;
						vertices.Buffer[i + 2] = vertex01;
						vertices.Buffer[i + 3] = vertex00;
					}
				}
			}
		}
	}
}
