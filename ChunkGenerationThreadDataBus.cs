using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using System.Threading;

namespace ViMG
{
	public class ChunkGenerationThreadDataBus
	{
		public class ThreadedChunk
		{
			public ChunkPosition position;
			public ChunkData data;

			public bool syncable;
		}

		private readonly Dictionary<ChunkPosition, ThreadedChunk> chunks = new Dictionary<ChunkPosition, ThreadedChunk>();
		private readonly Stack<ThreadedChunk> nextChunks = new Stack<ThreadedChunk>();
		private readonly List<ThreadedChunk> syncableChunks = new List<ThreadedChunk>();
		private readonly ChunkManager manager;

		private List<ThreadedChunk> chunksToGenerateQueue = new List<ThreadedChunk>();

		private Mutex accessMut;

		public ChunkGenerationThreadDataBus(ChunkManager manager)
		{
			this.manager = manager;
		}

		public void AddChunkToGenerate(ChunkPosition position, ChunkData data)
		{
			accessMut.WaitOne();

			chunksToGenerateQueue.Add(new ThreadedChunk()
			{
				position = position,
				data = data,
				syncable = false
			});

			accessMut.ReleaseMutex();
		}

		// Call from ChunkGenerationThread
		public void SendToGenerateToThread()
		{
			accessMut.WaitOne();

			foreach (var chunk in chunksToGenerateQueue)
			{
				chunks.Add(chunk.position, chunk);
				nextChunks.Push(chunk);
			}

			chunksToGenerateQueue.Clear();

			accessMut.ReleaseMutex();
		}

		public ChunkManager GetManager()
		{
			return manager;
		}

		public bool HasChunksToGenerate()
		{
			return nextChunks.Count > 0;
		}

		public ChunkData GetNextChunk()
		{
			return nextChunks.Pop().data;
		}

		public void FinishChunk(ChunkPosition position)
		{
			if (Thread.CurrentThread == Main.MainThread)
				throw new Exception("Cannot mark a chunk as syncable on the main thread.");

			chunks[position].syncable = true;

			syncableChunks.Add(chunks[position]);
		}

		public ChunkData GetChunk(ChunkPosition position)
		{
			if (Thread.CurrentThread == Main.MainThread)
				throw new Exception("Cannot get a chunk directly from the data bus on the main thread. Use GetSyncableChunks.");

			if (chunks.ContainsKey(position))
			{
				var tc = chunks[position];
				tc.syncable = false;
				return tc.data;
			}
			else
			{
				ChunkData data = manager.ChunkDatas.Get();

				if (manager.GetChunk(position).Initialized)
					data.CloneFrom(manager.GetChunk(position).GetData());

				chunks.Add(position, new ThreadedChunk()
				{
					position = position,
					data = data,
					syncable = false
				});

				return data;
			}
		}

		public List<ThreadedChunk> GetSyncableChunks()
		{
			accessMut.WaitOne();

			foreach (ThreadedChunk chunk in syncableChunks)
			{
				chunks.Remove(chunk.position);
			}

			List<ThreadedChunk> syncingChunks = new List<ThreadedChunk>(syncableChunks);

			accessMut.ReleaseMutex();

			return syncingChunks;
		}
	}
}
