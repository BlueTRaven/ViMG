using BrUtility.Ported;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Entities;

namespace ViMG
{
	public class ChunkLoadManager
    {
		private enum LoadingState
        {
			Unloaded,
			Loading,
			Loaded
        }

        private WorldSaver saver;
		private readonly ChunkManager chunkManager;
		private readonly EntityManager entityManager;
		private readonly ChunkManagerIO chunkIO;
        private readonly EntityManagerIO entIO;
        private Dictionary<ChunkPosition, LoadingState> loadedChunks = new Dictionary<ChunkPosition, LoadingState>();
		private List<ChunkPosition> unloadChunks = new List<ChunkPosition>();

		private const float DISTANCE_UNLOAD_CHECK_TIME = 4;
		private float distanceUnloadCheckTimer;

		private readonly int radiusH;
		private readonly int radiusV;
		private readonly int unloadRadius;

		private Vector3 loadTarget;

		private PriorityQueue<ChunkPosition> queue = new PriorityQueue<ChunkPosition>(true, (x) =>
		{
			return (int)(Main.camera.Position - x.InWorldSpace()).Length();
		}); 
		
		public ChunkLoadManager(WorldSaver saver, ChunkManager chunkManager, EntityManager entityManager, int radiusH, int radiusV, int unloadRadius, ChunkManagerIO chunkIO, EntityManagerIO entIO)
		{
			this.saver = saver;
			this.chunkManager = chunkManager;
			this.entityManager = entityManager;
			this.radiusH = radiusH;
			this.radiusV = radiusV;
			this.unloadRadius = unloadRadius;

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
		}

		public IEnumerable<ChunkPosition> GetLoadedChunks()
        {
			return loadedChunks.Keys;
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

				chunkIO.DeserializeChunk(world, queuedPosition);
				entIO.Deserialize(queuedPosition);
				loadedChunks[queuedPosition] = LoadingState.Loaded;
			}
		}

		public void ProcessLoadQueue(World world)
        {
			if (queue.Count > 30)
            {
				queue.Sort();
            }

			const int NUM_PER_FRAME = 1;

			int currentNum = 0;

			while (queue.Count > 0 && currentNum < NUM_PER_FRAME)
            {
				ChunkPosition queuedPosition = queue.Dequeue();

				//Chunk has been told to unload before we got to it.
				if (loadedChunks.ContainsKey(queuedPosition) && loadedChunks[queuedPosition] == LoadingState.Unloaded)
					continue;

				chunkIO.DeserializeChunk(world, queuedPosition);
				entIO.Deserialize(queuedPosition);
				loadedChunks[queuedPosition] = LoadingState.Loaded;

				currentNum++;
			}
		}

		//Loads a 1x32x1 column. Usually used for when the player first spawns into the world, so as to guarantee a valid spawn position,
		//if their spawn position does not yet exist.
		//NOTE this IMMEDIATELY loads chunks without going through the queue. Can be slow.
		public void LoadColumn(World world)
        {
			ChunkPosition baseChunkPos = ChunkPosition.WorldSpaceChunk(loadTarget);

			for (int y = 0; y < world.sizeInChunks; y++)
            {
				ChunkPosition pos = new ChunkPosition(baseChunkPos.X, y, baseChunkPos.Z);

				if (chunkManager.IsInWorldBounds(pos) && (!loadedChunks.ContainsKey(pos) || loadedChunks[pos] == LoadingState.Unloaded))
                {
					chunkIO.DeserializeChunk(world, pos);
					entIO.Deserialize(pos);
					loadedChunks.Add(pos, LoadingState.Loaded);
				}
            }
        }

		public void LoadChunk(World world, ChunkPosition position)
        {
			if (chunkManager.IsInWorldBounds(position) && (!loadedChunks.ContainsKey(position) || loadedChunks[position] == LoadingState.Unloaded))
			{
				chunkIO.DeserializeChunk(world, position);
				entIO.Deserialize(position);
				loadedChunks.Add(position, LoadingState.Loaded);
			}
		}

		public void LoadAroundTarget(World world)
		{
			ChunkPosition baseChunkPos = ChunkPosition.WorldSpaceChunk(loadTarget);
			
			for (int x = -radiusH; x <= radiusH; x++)
			{
				for (int y = -radiusV; y <= radiusV; y++)
				{
					for (int z = -radiusH; z < radiusH; z++)
					{
						var pos = baseChunkPos + new ChunkPosition(x, y, z);

						Vector2 distH = new Vector2(pos.X, pos.Z) - new Vector2(baseChunkPos.X, baseChunkPos.Z);
						
						if (distH.Length() < radiusH && chunkManager.IsInWorldBounds(pos))
						{
							if (!loadedChunks.ContainsKey(pos))
							{ 
								loadedChunks.Add(pos, LoadingState.Loading);
								queue.EnqueueWithoutSorting(pos);
							}
							else if (loadedChunks[pos] == LoadingState.Unloaded)
                            {
								loadedChunks[pos] = LoadingState.Loading;
								queue.EnqueueWithoutSorting(pos);
							}
						}
					}
				}
			}

			foreach (ChunkPosition pos in loadedChunks.Keys)
			{
				Vector2 dist = new Vector2(pos.X, pos.Z) - new Vector2(baseChunkPos.X, baseChunkPos.Z);

				float len = dist.Length();

				if (len > unloadRadius)
					unloadChunks.Add(pos);
			}

			Chunk[] chunks = chunkManager.GetChunks();

			foreach (ChunkPosition pos in unloadChunks)
			{
				if (loadedChunks[pos] == LoadingState.Loaded)
				{
					chunkIO.SerializeChunk(chunks, pos);
					entIO.Serialize(pos);

					entityManager.Unload(pos);
					chunkManager.Unload(pos);
				}

				loadedChunks.Remove(pos);
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
			chunkManager.UnloadAll();
			entityManager.UnloadAll();
		}
	}
}
