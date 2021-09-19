using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace ViMG
{
	public class ChunkGenerationThread
	{
		private Thread thread;

		private readonly ChunkGenerator generator;
		private readonly ChunkGenerationThreadDataBus dataBus;

		public static bool ForceStop;

		public ChunkGenerationThread(ChunkGenerator generator, ChunkGenerationThreadDataBus dataBus)
		{
			this.generator = generator;
			this.dataBus = dataBus;

			thread = new Thread(() => { Generate(); });
		}

		public void Start()
		{
			thread.Start();
		}

		private void Generate()
		{
			while (!ForceStop)
			{
				dataBus.SendToGenerateToThread();

				int num = 16;
				while (dataBus.HasChunksToGenerate() && num > 0)
				{
					Chunk chunk = dataBus.GetNextChunk();

					GenerateChunk(chunk.Position);

					num--;
				}

				if (!Main.MainThread.IsAlive)
					ForceStop = true;
				else if (!dataBus.HasChunksToGenerate())
					Thread.Sleep(1000);
			}
		}

		private HashSet<Chunk> cascadedChunks = new HashSet<Chunk>();

		private void GenerateChunk(ChunkPosition position)
		{
			cascadedChunks.Clear();

			Chunk chunk = dataBus.GetChunk(position, generator);
			if (chunk.GetData().GenStep == ChunkData.GenerationStep.Broad)
			{
				GenerateChunkBroad(position);
				GenerateChunkDetail(position, cascadedChunks);

				dataBus.FinishChunk(position);

				List<ChunkPosition> pos = new List<ChunkPosition>();

				foreach (Chunk cc in cascadedChunks)
				{
					if (cc.Position != position)
					{
						pos.Add(cc.Position);
						
						if (!dataBus.ChunkExists(cc))
							throw new Exception("???");
					}
						//dataBus.FinishChunk(cc.Position);
				}

				dataBus.FinishChunks(pos);
			}
			else if (chunk.GetData().GenStep == ChunkData.GenerationStep.Detail)
			{
				GenerateChunkDetail(position, cascadedChunks);

				dataBus.FinishChunk(position);

				foreach (Chunk cc in cascadedChunks)
					if (cc.Position != position)
						dataBus.FinishChunk(cc.Position);
			}
		}

		private void GenerateChunkBroad(ChunkPosition position)
		{
			//Chunk chunk = generator.MakeChunk(dataBus.GetManager().ChunkDatas, position);
			Chunk chunk = dataBus.GetChunk(position, generator);
			generator.GenerateChunkBroad(chunk, position);
		}

		private void GenerateChunkDetail(ChunkPosition position, HashSet<Chunk> cascadedChunks)
		{
			Chunk chunk = dataBus.GetChunk(position, generator);

			generator.GenerateChunkDetail(dataBus, chunk, position, cascadedChunks);

			//chunks[position.X, position.Y, position.Z].chunk.Initialize(world);
		}
	}
}
