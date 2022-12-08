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
		private enum LoadingState
        {
			Unloaded,
			Loading,
			Loaded
        }

		private readonly ChunkManager2 chunkManager;
		private readonly EntityManager entityManager;
		private readonly ChunkManagerIO chunkIO;
        private readonly EntityManagerIO entIO;
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
		
		public ChunkLoadManager(ChunkManager2 chunkManager, EntityManager entityManager, ChunkManagerIO chunkIO, EntityManagerIO entIO)
		{
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
			return gettableLoadedChunks;
        }

		public bool IsLoaded(ChunkPosition position)
        {
			return loadedChunks.ContainsKey(position) && loadedChunks[position] == LoadingState.Loaded;
        }

		//Loads the entirety of the loading queue at once.
		//It's best practice to use this before saving, so as not to miss loading chunks!
		public void FlushLoadQueue(World world)
		{
			while (queue.Count > 0)
			{
				ChunkPosition queuedPosition = queue.Dequeue();

				//Chunk has been told to unload before we got to it.
				if (loadedChunks.ContainsKey(queuedPosition) && loadedChunks[queuedPosition] == LoadingState.Unloaded)
					continue;

				//chunkIO.DeserializeChunk(world, queuedPosition);
				entIO.Deserialize(queuedPosition);
				chunkManager.Mesher.BatchMeshChunk(world, queuedPosition);
				loadedChunks[queuedPosition] = LoadingState.Loaded;

				hasChanged = true;
			}

			world.GameStateManager.TheIsland.LoadMessage = "Flushing mesh queue...";
			chunkManager.Mesher.FlushMeshQueue();

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
				if (loadedChunks.ContainsKey(queuedPosition) && loadedChunks[queuedPosition] == LoadingState.Unloaded)
					continue;

				//chunkIO.DeserializeChunk(world, queuedPosition);
				entIO.Deserialize(queuedPosition);
				chunkManager.Mesher.BatchMeshChunk(world, queuedPosition);
				loadedChunks[queuedPosition] = LoadingState.Loaded;

				hasChanged = true;
				currentNum++;
			}
		}

		public void LoadChunk(World world, ChunkPosition position)
        {
			if (chunkManager.IsInWorldBounds(position) && (!loadedChunks.ContainsKey(position) || loadedChunks[position] == LoadingState.Unloaded))
			{
				//chunkIO.DeserializeChunk(world, position);
				entIO.Deserialize(position);
				chunkManager.Mesher.BatchMeshChunk(world, position);
				loadedChunks.Add(position, LoadingState.Loaded);

				hasChanged = true;
			}
		}

		//TODO this is primarily for debug purposes, I don't expect this to ever actually be used
		public void ReloadChunk(World world, ChunkPosition position)
        {
			if (chunkManager.IsInWorldBounds(position) && loadedChunks.ContainsKey(position))
            {
				//chunkIO.DeserializeChunk(world, position);
				loadedChunks[position] = LoadingState.Loading;
            }
		}

		public void LoadAroundTarget(World world)
		{
			ChunkPosition baseChunkPos = ChunkPosition.WorldSpaceChunk(loadTarget);
			
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
							if (!loadedChunks.ContainsKey(pos))
							{ 
								loadedChunks.Add(pos, LoadingState.Loading);
								queue.EnqueueWithoutSorting(pos);

								hasChanged = true;
							}
							else if (loadedChunks[pos] == LoadingState.Unloaded)
                            {
								loadedChunks[pos] = LoadingState.Loading;
								queue.EnqueueWithoutSorting(pos);

								hasChanged = true;
							}
						}
					}
				}
			}

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
			chunkManager.Mesher.UnloadAllMeshes();
			entityManager.UnloadAll();
		}

        public void Dispose()
        {
			UnloadAll();

			loadedChunks = null;
        }
    }
}
