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

		private void Generate()
		{
			while (!ForceStop)
			{
				dataBus.SendToGenerateToThread();

				while (dataBus.HasChunksToGenerate())
				{
					ChunkData chunk = dataBus.GetNextChunk();


				}

				Thread.Sleep(1000);
			}
		}

		/*private void GenerateChunk(World world, ChunkPosition position)
		{
			if (chunks[position.X, position.Y, position.Z].genStep == ChunkManager.GenerationStep.Broad)
			{
				GenerateChunkBroad(position);
				GenerateChunkDetail(world, position);
			}
			else if (chunks[position.X, position.Y, position.Z].genStep == ChunkManager.GenerationStep.Detail)
			{
				GenerateChunkDetail(world, position);
			}
		}

		public void GenerateChunkBroad(ChunkPosition position)
		{
			Chunk chunk = generator.MakeChunk(dataBus.GetManager().ChunkDatas, position);
			generator.GenerateChunkBroad(chunk, position);

			chunks[position.X, position.Y, position.Z].chunk = chunk;
			chunks[position.X, position.Y, position.Z].genStep = GenerationStep.Detail;
		}

		public void GenerateChunkDetail(World world, ChunkPosition position)
		{
			Chunk chunk = chunks[position.X, position.Y, position.Z].chunk;
			generator.GenerateChunkDetail(this, chunk, position);

			chunks[position.X, position.Y, position.Z].genStep = GenerationStep.Done;
			chunks[position.X, position.Y, position.Z].chunk.Initialize(world);

			chunks[position.X, position.Y, position.Z].meshDirty = true;
			chunks[position.X, position.Y, position.Z].meshQueued = true;
			chunksToMeshQueue.Enqueue(position);
		}*/
	}
}
