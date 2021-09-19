using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace ViMG
{
	public class ChunkLoadManager
	{
		private WorldSaver saver;
		private ChunkManager manager;
		private HashSet<ChunkPosition> loadedChunks = new HashSet<ChunkPosition>();
		private List<ChunkPosition> unloadChunks = new List<ChunkPosition>();

		private const float DISTANCE_UNLOAD_CHECK_TIME = 4;
		private float distanceUnloadCheckTimer;

		private readonly int radiusH;
		private readonly int radiusV;
		private readonly int unloadRadius;

		private Vector3 loadTarget;

		public ChunkLoadManager(WorldSaver saver, ChunkManager manager, int radiusH, int radiusV, int unloadRadius)
		{
			this.saver = saver;
			this.manager = manager;
			this.radiusH = radiusH;
			this.radiusV = radiusV;
			this.unloadRadius = unloadRadius;
		}

		public void Initialize(bool isFirstLoad)
		{
			if (isFirstLoad)
			{
				UnloadAll();
			}
		}

		public void Update(double deltaTime)
		{
			distanceUnloadCheckTimer -= (float)deltaTime;

			if (distanceUnloadCheckTimer <= 0)
			{
				distanceUnloadCheckTimer = DISTANCE_UNLOAD_CHECK_TIME;
				CheckAndLoadAroundTarget();
			}
		}

		public void CheckAndLoadAroundTarget()
		{
			ChunkPosition baseChunkPos = ChunkPosition.WorldSpaceChunk(loadTarget);
			
			foreach (ChunkPosition pos in unloadChunks)
			{
				if (!loadedChunks.Contains(pos))
					throw new Exception("???");

				//saver.SaveOne(pos);
				manager.Unload(pos);
				loadedChunks.Remove(pos);
			}

			for (int x = -radiusH; x <= radiusH; x++)
			{
				for (int y = -radiusV; y <= radiusV; y++)
				{
					for (int z = -radiusH; z < radiusH; z++)
					{
						var pos = baseChunkPos + new ChunkPosition(x, y, z);
						if (manager.IsInWorldBounds(pos) && !loadedChunks.Contains(pos))
						{
							//saver.LoadOne(pos);
							loadedChunks.Add(pos);
						}
					}
				}
			}

			foreach (ChunkPosition pos in loadedChunks)
			{
				Vector3 dist = new Vector3(pos.X, pos.Y, pos.Z) - new Vector3(baseChunkPos.X, baseChunkPos.Y, baseChunkPos.Z);

				float len = dist.Length();

				if (len > unloadRadius)
					unloadChunks.Add(pos);
			}

			unloadChunks.Clear();
		}

		public void UpdateLoadTarget(Vector3 position)
		{
			this.loadTarget = position;
		}

		public void ManualLoad(ChunkPosition position)
		{
			if (!loadedChunks.Contains(position))
			{
				loadedChunks.Add(position);
				//saver.LoadOne(position);
			}
		}

		private void UnloadAll()
		{
			loadedChunks.Clear();
			manager.UnloadAll();
		}
	}
}
