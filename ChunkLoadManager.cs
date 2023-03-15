using BrUtility.Ported;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Entities;

namespace ViMG
{
	public class ChunkLoadManager : IDisposable
    {
		private enum LoadingState : byte
        {
			Unloaded,
			Loading,
			Loaded
        }

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

		private PriorityQueue<ChunkPosition> queue = new PriorityQueue<ChunkPosition>(true, (x) =>
		{
			return (int)(Main.camera.Position - x.InWorldSpace()).Length();
		}); 
		
		public ChunkLoadManager(ChunkManager chunkManager, EntityManager entityManager, ChunkManagerIO chunkIO, EntityManagerIO entIO)
		{
			loadedChunksFastLookup = new LoadingState[chunkManager.SizeInChunksXZ * chunkManager.SizeInChunksXZ * chunkManager.SizeInChunksXZ];
			this.chunkManager = chunkManager;
			this.entityManager = entityManager;

			this.chunkIO = chunkIO;
            this.entIO = entIO;
        }

		public void Update(double deltaTime, World world)
		{
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
			Util.ThreeDToOneD(new ValuePoint3D(position.X, position.Y, position.Z), new ValuePoint3D(chunkManager.SizeInChunksXZ), out int i);
			return loadedChunksFastLookup[i] == LoadingState.Loaded;
			//return loadedChunks.ContainsKey(position) && loadedChunks[position] == LoadingState.Loaded;
        }

		//Loads the entirety of the loading queue at once.
		//It's best practice to use this before saving, so as not to miss loading chunks!
		public void FlushLoadQueue(World world)
		{
			chunkManager.RenderMesher.Flush(world);
			chunkManager.CollisionMesher.Flush(world);

			int max = queue.Count;
			world.GameStateManager.TheIsland.ProgressMax = max;

			while (queue.Count > 0)
			{
				world.GameStateManager.TheIsland.ProgressMin = max - queue.Count;

				ChunkPosition queuedPosition = queue.Dequeue();

				//Chunk has been told to unload before we got to it.
				//if (loadedChunks.ContainsKey(queuedPosition) && loadedChunks[queuedPosition] == LoadingState.Unloaded)
				Util.ThreeDToOneD(new ValuePoint3D(queuedPosition.X, queuedPosition.Y, queuedPosition.Z), new ValuePoint3D(chunkManager.SizeInChunksXZ), out int i);
				if (loadedChunksFastLookup[i] == LoadingState.Unloaded)
					continue;
				else if (loadedChunksFastLookup[i] == LoadingState.Loading)
				{
					//entIO.Deserialize(queuedPosition);
					//chunkManager.Mesher.BatchMeshChunk(world, queuedPosition);
					if (chunkManager.RenderMesher.IsMeshed(queuedPosition) && chunkManager.CollisionMesher.IsMeshed(queuedPosition))
					{
						loadedChunks[queuedPosition] = LoadingState.Loaded;
						loadedChunksFastLookup[i] = LoadingState.Loaded;

						hasChanged = true;
					}
					else
					{
						//not finished loading; re-queue
						queue.EnqueueWithoutSorting(queuedPosition);
					}
				}
			}

			world.GameStateManager.TheIsland.LoadMessage = "Flushing mesh queue...";
			chunkManager.RenderMesher.Flush(world);

			if (hasChanged)
				gettableLoadedChunks = loadedChunks.Keys;

			hasChanged = false;
		}

		public void ProcessLoadQueue(World world)
        {
			if (queue.Count > 30)
            {
				queue.Sort();
            }

			const int NUM_PER_FRAME = 200;

			int currentNum = 0;

			while (queue.Count > 0 && currentNum < NUM_PER_FRAME)
            {
				ChunkPosition queuedPosition = queue.Dequeue();

				//Chunk has been told to unload before we got to it.
				Util.ThreeDToOneD(new ValuePoint3D(queuedPosition.X, queuedPosition.Y, queuedPosition.Z), new ValuePoint3D(chunkManager.SizeInChunksXZ), out int i);
				//if (loadedChunks.ContainsKey(queuedPosition) && loadedChunks[queuedPosition] == LoadingState.Unloaded)
				if (loadedChunksFastLookup[i] == LoadingState.Unloaded)
					continue;
				else if (loadedChunksFastLookup[i] == LoadingState.Loading)
				{
					if (chunkManager.RenderMesher.IsMeshed(queuedPosition) && chunkManager.CollisionMesher.IsMeshed(queuedPosition))
					{
						loadedChunks[queuedPosition] = LoadingState.Loaded;
						loadedChunksFastLookup[i] = LoadingState.Loaded;

						hasChanged = true;
					}
					else
					{
						//not finished loading; re-queue
						queue.EnqueueWithoutSorting(queuedPosition);
					}

					currentNum++;
				}
			}
		}

		public void LoadChunk(World world, ChunkPosition position)
        {
			if (chunkManager.IsInWorldBounds(position) && (!loadedChunks.ContainsKey(position) || loadedChunks[position] == LoadingState.Unloaded))
			{
				bool shouldLoad = false;
				if (!loadedChunks.ContainsKey(position))
				{
					loadedChunks.Add(position, LoadingState.Loading);
					shouldLoad = true;
				}
				else if (loadedChunks[position] == LoadingState.Unloaded)
				{
					loadedChunks[position] = LoadingState.Loading;
					shouldLoad = true;
				}

				if (shouldLoad)
				{
					Util.ThreeDToOneD(new ValuePoint3D(position.X, position.Y, position.Z), new ValuePoint3D(chunkManager.SizeInChunksXZ), out int i);
					loadedChunksFastLookup[i] = LoadingState.Loading;
					queue.EnqueueWithoutSorting(position);

					entIO.Deserialize(position);
					chunkManager.RenderMesher.AddToNextBatch(world, position);
					chunkManager.CollisionMesher.AddToNextBatch(world, position);

					hasChanged = true;
				}
			}
		}

		public void LoadAroundTarget(World world)
		{
			ChunkPosition baseChunkPos = ChunkPosition.WorldSpaceChunk(loadTarget);

			//ProfilingHelper.StartBatch("Beginning load around target...");

			for (int x = -Options.RenderDistance; x <= Options.RenderDistance; x++)
			{
				for (int y = -Options.RenderDistance; y <= Options.RenderDistance; y++)
				{
					for (int z = -Options.RenderDistance; z < Options.RenderDistance; z++)
					{
						var pos = baseChunkPos + new ChunkPosition(x, y, z);

						Vector2 distH = new Vector2(pos.X, pos.Z) - new Vector2(baseChunkPos.X, baseChunkPos.Z);
						
						if (distH.Length() < Options.RenderDistance && chunkManager.IsInWorldBounds(pos))
						{
							bool shouldLoad = false;
							if (!loadedChunks.ContainsKey(pos))
							{
								loadedChunks.Add(pos, LoadingState.Loading);
								shouldLoad = true;
							}
							else if (loadedChunks[pos] == LoadingState.Unloaded)
							{
								loadedChunks[pos] = LoadingState.Loading;
								shouldLoad = true;
							}

							if (shouldLoad)
							{
								Util.ThreeDToOneD(new ValuePoint3D(pos.X, pos.Y, pos.Z), new ValuePoint3D(chunkManager.SizeInChunksXZ), out int i);
								loadedChunksFastLookup[i] = LoadingState.Loading;
								queue.EnqueueWithoutSorting(pos);

								entIO.Deserialize(pos);
								//Note that we add to the next batch directly instead of simply marking dirty
								//This is because marking dirty isn't guaranteed to be finished any time soon,
								//and will only ever enqueue one batch per frame.
								chunkManager.RenderMesher.AddToNextBatch(world, pos);
								chunkManager.CollisionMesher.AddToNextBatch(world, pos);
								//ProfilingHelper.AddBatch();

								hasChanged = true;
							}
						}
					}
				}
			}

			//ProfilingHelper.EndBatch("Done.");

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
					chunkManager.Unload(pos);
				}

				loadedChunksFastLookup[i] = LoadingState.Unloaded;
				loadedChunks.Remove(pos);

				hasChanged = true;
			}

			unloadChunks.Clear();
		}

		public void UpdateLoadTarget(Vector3 position)
		{
			this.loadTarget = position;
		}

		public void UnloadAll()
		{
			loadedChunks.Clear();
			//TODO: there may still be meshes in the queue.
			//The reason why I'm not calling FlushMeshQueue here is because it needs World
			chunkManager.RenderMesher.UnloadAll();
			chunkManager.CollisionMesher.UnloadAll();
			entityManager.UnloadAll();
		}

        public void Dispose()
        {
			UnloadAll();

			loadedChunks = null;
        }
    }
}
