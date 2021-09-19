using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using System.Threading;
using BrUtility;

namespace ViMG
{
	public class ChunkGenerationThreadDataBus
	{
		public class ThreadedChunk
		{
			public Chunk chunk;
			public List<ThreadedChunk> cascadedChunks = new List<ThreadedChunk>();

			public bool syncable;
		}

		private readonly Dictionary<ChunkPosition, ThreadedChunk> chunks = new Dictionary<ChunkPosition, ThreadedChunk>();
		private readonly Stack<ThreadedChunk> nextChunks = new Stack<ThreadedChunk>();
		private readonly Queue<ThreadedChunk> syncableChunks = new Queue<ThreadedChunk>();
		private readonly ChunkManager manager;

		private List<ThreadedChunk> chunksToGenerateQueue = new List<ThreadedChunk>();

		private SemaphoreSlim accessSem;
		private Mutex accessMut;

		public ChunkGenerationThreadDataBus(ChunkManager manager)
		{
			this.manager = manager;
			accessMut = new Mutex();
			accessSem = new SemaphoreSlim(1);
		}

		public bool AddChunkToGenerate(ChunkPosition position, ChunkGenerator generator, GenericPool<ChunkData> chunkDatas)
		{
			if (accessSem.Wait(0))
			{
				if (!chunks.ContainsKey(position) || chunks[position].chunk.GetData().GenStep != ChunkData.GenerationStep.Done)
				{
					Chunk chunk = generator.MakeChunk(chunkDatas, position);
					//accessMut.WaitOne();
					chunksToGenerateQueue.Add(new ThreadedChunk()
					{
						chunk = chunk,
						syncable = false
					});

					accessSem.Release();

					return true;
				}

				accessSem.Release();
			}
			//accessMut.ReleaseMutex();

			return false;
		}

		// Call from ChunkGenerationThread
		public void SendToGenerateToThread()
		{
			if (Thread.CurrentThread == Main.MainThread)
				throw new Exception("Cannot start generating chunks on main thread.");

			//accessMut;
			accessSem.Wait();

			foreach (var chunk in chunksToGenerateQueue)
			{
				if (!chunks.ContainsKey(chunk.chunk.Position))
				{
					chunks.Add(chunk.chunk.Position, chunk);
					nextChunks.Push(chunk);
				}
			}

			chunksToGenerateQueue.Clear();

			accessSem.Release();
			//accessMut.ReleaseMutex();
		}

		public ChunkManager GetManager()
		{
			return manager;
		}

		public bool HasChunksToGenerate()
		{
			return nextChunks.Count > 0;
		}

		public Chunk GetNextChunk()
		{
			return nextChunks.Pop().chunk;
		}

		public void FinishChunk(ChunkPosition position)
		{
			if (Thread.CurrentThread == Main.MainThread)
				throw new Exception("Cannot mark a chunk as syncable on the main thread.");

			chunks[position].syncable = true;

			accessSem.Wait();
			//accessMut.WaitOne();

			syncableChunks.Enqueue(chunks[position]);

			chunks.Remove(position);

			accessSem.Release();
			//accessMut.ReleaseMutex();
		}

		public void FinishChunks(List<ChunkPosition> positions)
		{
			accessSem.Wait();

			foreach (ChunkPosition pos in positions)
			{
				syncableChunks.Enqueue(chunks[pos]);
				chunks.Remove(pos);
			}

			accessSem.Release();
		}

		public Chunk GetChunk(ChunkPosition position, ChunkGenerator generator)
		{
			if (Thread.CurrentThread == Main.MainThread)
				throw new Exception("Cannot get a chunk directly from the data bus on the main thread. Use GetSyncableChunks.");

			accessSem.Wait();
			//accessMut.WaitOne();

			if (chunks.ContainsKey(position))
			{
				var tc = chunks[position];
				tc.syncable = false;

				accessSem.Release();
				//accessMut.ReleaseMutex();

				return tc.chunk;
			}
			else
			{
				ThreadedChunk chunk = new ThreadedChunk()
				{
					chunk = generator.MakeChunk(manager.ChunkDatas, position),
					syncable = false
				};

				chunks.Add(position, chunk);

				if (manager.GetChunk(position).Initialized)
				{
					chunk.chunk.GetData().CloneFrom(manager.GetChunk(position).GetData());
				}

				accessSem.Release();
				//accessMut.ReleaseMutex();

				return chunk.chunk;
			}
		}

		public bool ChunkExists(Chunk chunk)
		{
			return chunks.ContainsKey(chunk.Position);
		}

		const int MAX_SYNC_PER_FRAME = 64;
		private ThreadedChunk[] syncingChunks = new ThreadedChunk[MAX_SYNC_PER_FRAME];

		public ThreadedChunk[] GetSyncableChunks()
		{
			if (accessSem.Wait(0))
			{
				for (int i = 0; i < MAX_SYNC_PER_FRAME; i++)
					syncingChunks[i] = null;

				if (syncableChunks.Count > 0)
				{
					for (int i = 0; i < Math.Min(MAX_SYNC_PER_FRAME, syncableChunks.Count); i++)
					{
						ThreadedChunk chunk = syncableChunks.Dequeue();

						syncingChunks[i] = chunk;

						//chunks.Remove(chunk.chunk.Position);
					}

					//syncableChunks.Clear();
				}

				accessSem.Release();
				//accessMut.ReleaseMutex();

				return syncingChunks;
			}

			return null;
		}
	}
}
