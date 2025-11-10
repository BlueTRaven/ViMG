using BepuPhysics.Constraints;
using BepuUtilities.Memory;
using BrUtility.Ported;
using Engine.ChunkStuff;
using Microsoft.Xna.Framework;
using SharpDX.Direct3D11;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ViMG.ChunkStuff;
using ViMG.Entities;
using ViMG.GameStates;
using ViMG.IMGUIImpl;

namespace ViMG
{
    // TODO: are copies correctly returned if meshing is interrupted or unused?
    public class ChunkLoadManager : IDisposable
    {
		private enum LoadingState : byte
        {
			Unloaded,
			Enqueued,
			Loading,
			Loaded
        }

		private readonly ChunkMesher chunkMesher;
		private readonly ChunkManager chunkManager;
		private readonly EntityManager entityManager;
		private readonly ChunkManagerIO chunkIO;
        private readonly EntityManagerIO entIO;
		private LoadingState[] loadedChunksFastLookup;
		private Dictionary<ChunkPosition, LoadingState> loadedChunks = new Dictionary<ChunkPosition, LoadingState>();
		private List<ChunkPosition> unloadChunks = new List<ChunkPosition>();
		private IEnumerable<ChunkPosition> gettableLoadedChunks;

		private bool hasChanged = false;

		private const float DISTANCE_UNLOAD_CHECK_TIME = 4;
		private float distanceUnloadCheckTimer;

		private Vector3 loadTarget;

		private struct QueuedChunk
		{
			public ChunkPosition position;
			public Task<CopiedChunkData> copyTask;
		}

		private PriorityQueue<QueuedChunk> queue = new(true, (queuedChunk) =>
		{
			return (int)(Main.camera.Position - queuedChunk.position.InWorldSpace()).Length();
		});

		private List<QueuedChunk> copyingChunks = new();
		
		// Double buffers and the currently used buffer.
		private List<ChunkPosition> waitingToFinishMeshingChunks1 = new();
        private List<ChunkPosition> waitingToFinishMeshingChunks2 = new();
		private List<ChunkPosition> waitingToFinishMeshingChunks;

		public ChunkLoadManager(ChunkMesher chunkMesher, ChunkManager chunkManager, EntityManager entityManager, ChunkManagerIO chunkIO, EntityManagerIO entIO)
		{
			ThreadPool.SetMaxThreads(8, 8);

			loadedChunksFastLookup = new LoadingState[chunkManager.SizeInChunksXZ * chunkManager.SizeInChunksXZ * chunkManager.SizeInChunksXZ];
			this.chunkMesher = chunkMesher;
			this.chunkManager = chunkManager;
			this.entityManager = entityManager;

			this.chunkIO = chunkIO;
            this.entIO = entIO;

			waitingToFinishMeshingChunks = waitingToFinishMeshingChunks1;
        }

		public void Update(double deltaTime, World world)
		{
            using var zone = TracyImpl.Tracy.BeginZone();

            ProcessLoadQueue(world);

			distanceUnloadCheckTimer -= (float)deltaTime;

			if (distanceUnloadCheckTimer <= 0)
			{
				distanceUnloadCheckTimer = DISTANCE_UNLOAD_CHECK_TIME;
				LoadAroundTarget(world);
			}

			if (hasChanged)
				gettableLoadedChunks = loadedChunks.Keys;

			hasChanged = false;
		}

		public IEnumerable<ChunkPosition> GetLoaded()
        {
			if (gettableLoadedChunks == null)
				gettableLoadedChunks = loadedChunks.Keys;
			return gettableLoadedChunks;
        }

		public bool IsLoaded(ChunkPosition position)
        {
			if (!chunkManager.IsInWorldBounds(position))
				return false;

			Util.ThreeDToOneD(new ValuePoint3D(position.X, position.Y, position.Z), new ValuePoint3D(chunkManager.SizeInChunksXZ), out int i);
			return loadedChunksFastLookup[i] == LoadingState.Loaded;
			//return loadedChunks.ContainsKey(position) && loadedChunks[position] == LoadingState.Loaded;
        }

		//Loads the entirety of the loading queue at once.
		//It's best practice to use this before saving, so as not to miss loading chunks!
		public void FlushLoadQueue(World world)
		{
            using var zone = TracyImpl.Tracy.BeginZone();

			foreach (var queuedChunk in queue.GetEnumerable())
			{
				queuedChunk.copyTask.Start();
			}
            
			foreach (var queuedChunk in queue.GetEnumerable()) 
			{ 
                queuedChunk.copyTask.Wait();
                
				chunkMesher.RenderMesher.AddToNextBatch(world, queuedChunk.position, queuedChunk.copyTask.Result);
                chunkMesher.CollisionMesher.AddToNextBatch(world, queuedChunk.position, queuedChunk.copyTask.Result);
            }

            chunkMesher.RenderMesher.BeginFlush();
			chunkMesher.CollisionMesher.BeginFlush();
			chunkMesher.RenderMesher.FinishFlush();
            chunkMesher.CollisionMesher.FinishFlush();

			int max = queue.Count;
            GameStateTheIsland.ProgressMax = max;

			while (queue.Count > 0)
			{
                //GameStateTheIsland.ProgressMin = max - queue.Count;

				// TODO: sometimes there's stuff in the queue that apparently never gets meshed properly. Why is this?

				QueuedChunk queuedChunk = queue.Dequeue();

                entIO.Deserialize(queuedChunk.position);

                Util.ThreeDToOneD(new ValuePoint3D(queuedChunk.position.X, queuedChunk.position.Y, queuedChunk.position.Z), new ValuePoint3D(chunkManager.SizeInChunksXZ), out int i);
				loadedChunks[queuedChunk.position] = LoadingState.Loaded;
				loadedChunksFastLookup[i] = LoadingState.Loaded;

				hasChanged = true;
				//Chunk has been told to unload before we got to it.
				//if (loadedChunks.ContainsKey(queuedPosition) && loadedChunks[queuedPosition] == LoadingState.Unloaded)
				//Util.ThreeDToOneD(new ValuePoint3D(queuedPosition.X, queuedPosition.Y, queuedPosition.Z), new ValuePoint3D(chunkManager.SizeInChunksXZ), out int i);
				//if (loadedChunksFastLookup[i] == LoadingState.Unloaded)
				//	continue;
				//else if (loadedChunksFastLookup[i] == LoadingState.Loading)
				//{
				//	//entIO.Deserialize(queuedPosition);
				//	//chunkManager.Mesher.BatchMeshChunk(world, queuedPosition);
				//	if (chunkManager.RenderMesher.IsMeshed(queuedPosition) && chunkManager.CollisionMesher.IsMeshed(queuedPosition))
				//	{
				//		loadedChunks[queuedPosition] = LoadingState.Loaded;
				//		loadedChunksFastLookup[i] = LoadingState.Loaded;

				//		hasChanged = true;
				//	}
				//	else
				//	{
				//		//not finished loading; re-queue
				//		queue.EnqueueWithoutSorting(queuedPosition);
				//	}
				//}
			}

			waitingToFinishMeshingChunks.Clear();

            //GameStateTheIsland.LoadMessage = "Flushing mesh queue...";
            //chunkManager.RenderMesher.FinishFlush();

            if (hasChanged)
				gettableLoadedChunks = loadedChunks.Keys;

			hasChanged = false;
		}

		public void ProcessLoadQueue(World world)
        {
            using var zone = TracyImpl.Tracy.BeginZone();

			using (var zoneSort = TracyImpl.Tracy.BeginZone(name: "Sort"))
			{
				if (queue.Count > 30)
				{
					queue.Sort();
				}
			}

			int currentNum = 0;

			var zoneQueue = TracyImpl.Tracy.BeginZone(name: "Queue");
			while (queue.Count > 0 && currentNum < IMGUISettings.CopiesPerFrame)
            {
				QueuedChunk queuedChunk = queue.Dequeue();

				//Chunk has been told to unload before we got to it.
				Util.ThreeDToOneD(new ValuePoint3D(queuedChunk.position.X, queuedChunk.position.Y, queuedChunk.position.Z), new ValuePoint3D(chunkManager.SizeInChunksXZ), out int i);
				//if (loadedChunks.ContainsKey(queuedPosition) && loadedChunks[queuedPosition] == LoadingState.Unloaded)
				if (loadedChunksFastLookup[i] == LoadingState.Unloaded)
				{
					// TODO: do we need to stop things?
					continue;
				}
				else if (loadedChunksFastLookup[i] == LoadingState.Enqueued)
				{
                    queuedChunk.copyTask.Start();
                    copyingChunks.Add(queuedChunk);

                    currentNum++;
                }
			}
			zoneQueue.End();

			var zoneWait = TracyImpl.Tracy.BeginZone(name: "WaitForCopy");
			foreach (QueuedChunk copyingChunk in copyingChunks)
			{
				copyingChunk.copyTask.Wait();
                CopiedChunkData copy = copyingChunk.copyTask.Result;

                Util.ThreeDToOneD(new ValuePoint3D(copyingChunk.position.X, copyingChunk.position.Y, copyingChunk.position.Z), new ValuePoint3D(chunkManager.SizeInChunksXZ), out int i);
                loadedChunks[copyingChunk.position] = LoadingState.Loading;
                loadedChunksFastLookup[i] = LoadingState.Loading;

                chunkMesher.RenderMesher.AddToNextBatch(world, copyingChunk.position, copy);
                chunkMesher.CollisionMesher.AddToNextBatch(world, copyingChunk.position, copy);

				waitingToFinishMeshingChunks1.Add(copyingChunk.position);
            }

			copyingChunks.Clear();
			zoneWait.End();


            zoneWait = TracyImpl.Tracy.BeginZone(name: "WaitForMeshingFinished");
            // Double buffered. If a chunk is not finished, it is moved to the other buffer, and the buffers are swapped each ProcessLoadQueue call.
            var otherBuffer = waitingToFinishMeshingChunks == waitingToFinishMeshingChunks1 ? waitingToFinishMeshingChunks2 : waitingToFinishMeshingChunks1;
			foreach (ChunkPosition position in waitingToFinishMeshingChunks)
			{
                if (chunkMesher.RenderMesher.IsMeshed(position) && chunkMesher.CollisionMesher.IsMeshed(position))
                {
                    Util.ThreeDToOneD(new ValuePoint3D(position.X, position.Y, position.Z), new ValuePoint3D(chunkManager.SizeInChunksXZ), out int i);

                    loadedChunks[position] = LoadingState.Loaded;
                    loadedChunksFastLookup[i] = LoadingState.Loaded;

                    entIO.Deserialize(position);
                    hasChanged = true;
                }
                else
                {
                    //not finished loading; re-queue
                    otherBuffer.Add(position);
                }

            }

            waitingToFinishMeshingChunks.Clear();
			waitingToFinishMeshingChunks = otherBuffer;
			zoneWait.End();
		}

		// Loads a single chunk, blocking until it is fully loaded.
		public void LoadChunk(World world, ChunkPosition position)
        {
            using var zone = TracyImpl.Tracy.BeginZone();

            if (chunkManager.IsInWorldBounds(position) && (!loadedChunks.ContainsKey(position) || loadedChunks[position] == LoadingState.Unloaded))
			{
				bool shouldLoad = false;
				if (!loadedChunks.ContainsKey(position))
				{
					loadedChunks.Add(position, LoadingState.Loaded);
					shouldLoad = true;
				}
				else if (loadedChunks[position] == LoadingState.Unloaded)
				{
					loadedChunks[position] = LoadingState.Loaded;
					shouldLoad = true;
				}

				if (shouldLoad)
				{
					Util.ThreeDToOneD(new ValuePoint3D(position.X, position.Y, position.Z), new ValuePoint3D(chunkManager.SizeInChunksXZ), out int i);
					loadedChunksFastLookup[i] = LoadingState.Loaded;
                    //queue.EnqueueWithoutSorting(position);

                    chunkMesher.RenderMesher.ImmediatelyMesh(world, position);
                    chunkMesher.CollisionMesher.ImmediatelyMesh(world, position);

					entIO.Deserialize(position);
					//CopiedChunkData copy = CopiedChunkPool.MakeCopy(world, bufferPool, position);
					//chunkManager.RenderMesher.AddToNextBatch(world, position, copy);
					//chunkManager.CollisionMesher.AddToNextBatch(world, position, copy);

					hasChanged = true;
				}
			}
		}

		public unsafe void LoadAroundTarget(World world, int iteration = 4, int? tempRenderDistance = null)
		{
            using var zone = TracyImpl.Tracy.BeginZone();

            ChunkPosition baseChunkPos = ChunkPosition.WorldSpaceChunk(loadTarget);

			//ProfilingHelper.StartBatch("Beginning load around target...");

			int useRenderDistance = tempRenderDistance.GetValueOrDefault(Options.RenderDistance);

			List<(ChunkPosition, Task<CopiedChunkData>)> things = new List<(ChunkPosition, Task<CopiedChunkData>)>();

			int minx = 0;
			int maxx = 0;
			int minz = 0;
			int maxz = 0;

			if (iteration == 0)
			{
				minx = -useRenderDistance;
				maxx = 0;
				minz = -useRenderDistance;
				maxz = 0;
			}
			else if (iteration == 1)
			{
				minx = 0;
				maxx = useRenderDistance;
				minz = -useRenderDistance;
				maxz = 0;
			}
			else if (iteration == 2)
			{
				minx = 0;
				maxx = useRenderDistance;
				minz = 0;
				maxz = useRenderDistance;
			}
			else if (iteration == 3)
			{
                minx = -useRenderDistance;
                maxx = 0; 
				minz = 0;
                maxz = useRenderDistance;
            }
			else if (iteration == 4)
			{
				minx = -useRenderDistance;
				maxx = useRenderDistance;
				minz = -useRenderDistance;
				maxz = useRenderDistance;
			}

            for (int z = minz; z < maxz; z++)
			{
				for (int y = -useRenderDistance; y <= useRenderDistance; y++)
				{
					for (int x = minx; x <= maxx; x++)
					{
						var pos = baseChunkPos + new ChunkPosition(x, y, z);

						Vector2 distH = new Vector2(pos.X, pos.Z) - new Vector2(baseChunkPos.X, baseChunkPos.Z);
						
						if (distH.Length() < Options.RenderDistance && chunkManager.IsInWorldBounds(pos))
						{
                            Util.ThreeDToOneD(new ValuePoint3D(pos.X, pos.Y, pos.Z), new ValuePoint3D(chunkManager.SizeInChunksXZ), out int i);
                            bool shouldLoad = false;
							if (loadedChunksFastLookup[i] == LoadingState.Unloaded)
							{
								shouldLoad = true;
							}

							//if (!loadedChunks.ContainsKey(pos))
							//{
							//	loadedChunks.Add(pos, LoadingState.Loading);
							//	shouldLoad = true;
							//}
							//else if (loadedChunks[pos] == LoadingState.Unloaded)
							//{
							//	shouldLoad = true;
							//}

							if (shouldLoad)
							{
                                if (!loadedChunks.ContainsKey(pos))
                                {
                                    loadedChunks.Add(pos, LoadingState.Enqueued);
                                } else loadedChunks[pos] = LoadingState.Enqueued;

								loadedChunksFastLookup[i] = LoadingState.Enqueued;

								var context = new CopyChunkTaskContext {
									world = world,
									pool = chunkMesher.bufferPool,
									position = pos,
								};
								var task = new Task<CopiedChunkData>(CopyChunkTaskFn, context);
								// NOTE: tasks are not immediately started.
								queue.EnqueueWithoutSorting(new QueuedChunk
								{
									copyTask = task,
									position = pos,
								});

								hasChanged = true;

                            }
                        }
					}
				}
			}

			//foreach ((ChunkPosition pos, Task<CopiedChunkData> task) thing in things)
			//{
			//	thing.task.Wait();
			//	CopiedChunkData copy = thing.task.Result;
   //             chunkManager.RenderMesher.AddToNextBatch(world, thing.pos, copy);
   //             chunkManager.CollisionMesher.AddToNextBatch(world, thing.pos, copy);
			//}

            //ProfilingHelper.EndBatch("Done.");

            var zoneUnload = TracyImpl.Tracy.BeginZone(name: "Unload");
            foreach (ChunkPosition pos in loadedChunks.Keys)
			{
				Vector2 dist = new Vector2(pos.X, pos.Z) - new Vector2(baseChunkPos.X, baseChunkPos.Z);

				float len = dist.Length();

				if (len > Options.RenderDistance + 2)
					unloadChunks.Add(pos);
			}

			foreach (ChunkPosition pos in unloadChunks)
			{
				Util.ThreeDToOneD(new ValuePoint3D(pos.X, pos.Y, pos.Z), new ValuePoint3D(chunkManager.SizeInChunksXZ), out int i);

				if (loadedChunks[pos] == LoadingState.Loaded)
				{
					//chunkIO.SerializeChunk(chunks, pos);
					entIO.Serialize(pos);

					entityManager.Unload(pos);
                    chunkMesher.Unload(pos);
				}

				loadedChunksFastLookup[i] = LoadingState.Unloaded;
				loadedChunks.Remove(pos);

				hasChanged = true;
			}

			unloadChunks.Clear();
			zoneUnload.End();
		}

        private struct CopyChunkTaskContext
        {
			public World world;
			public BufferPool pool;
			public ChunkPosition position;
        }

        private CopiedChunkData CopyChunkTaskFn(object context)
		{
			var copyContext = (CopyChunkTaskContext)context;

			return CopiedChunkPool.MakeCopy(copyContext.world, copyContext.pool, copyContext.position);
		}

		public void UpdateLoadTarget(Vector3 position)
		{
			this.loadTarget = position;
		}

		public void UnloadAll()
		{
            using var zone = TracyImpl.Tracy.BeginZone();

            //TODO: there may still be meshes in the queue.
            chunkMesher.RenderMesher.FinishFlush();
            chunkMesher.CollisionMesher.FinishFlush();

            chunkMesher.RenderMesher.UnloadAll();
            chunkMesher.CollisionMesher.UnloadAll();
			
			entityManager.UnloadAll();
			
			loadedChunks.Clear();
		}

        public void Dispose()
        {
            chunkMesher.Dispose();
			entityManager.Dispose();

			loadedChunks = null;
        }
    }
}
