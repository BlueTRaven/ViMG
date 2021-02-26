using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using ViMG.Cubes;

namespace ViMG
{
	public class ChunkerThread
	{
		private static int chunkGenerationQueue;
		public static int ChunkGenerationQueue => chunkGenerationQueue;
		private static int chunkMeshingQueue;
		public static int ChunkMeshingQueue => chunkMeshingQueue;

		private readonly ChunkGenerator generator;
		private readonly ChunkMesher mesher;
		private Thread thread;

		public bool HasFinishedChunks => finishedChunks.Count > 0;
		private List<(Chunk, ChunkMesh)> finishedChunks = new List<(Chunk, ChunkMesh)>();

		public bool IsAlive => thread != null && thread.IsAlive;

		public ChunkerThread(ChunkGenerator generator, ChunkMesher mesher)
		{
			this.generator = generator;
			this.mesher = mesher;
		}

		public void Start(CubeRegistry registry, Queue<ChunkPosition> chunksToGenerate, Queue<ChunkPosition> chunksToMesh, Dictionary<ChunkPosition, Chunk> generatedChunks)
		{
			thread = new Thread(() => Process(registry, chunksToGenerate, chunksToMesh, generatedChunks));
			thread.Start();
		}

		private void Process(CubeRegistry registry, Queue<ChunkPosition> chunksToGenerate, Queue<ChunkPosition> chunksToMesh, Dictionary<ChunkPosition, Chunk> generatedChunks)
		{
			chunkGenerationQueue = chunksToGenerate.Count;
			chunkMeshingQueue = chunksToMesh.Count;

			ProcessGenerationQueue(chunksToGenerate, generatedChunks);

			var meshed = ProcessMeshQueue(registry, chunksToMesh, generatedChunks);

			for (int i = 0; i < meshed.Count; i++)
			{
				generatedChunks[meshed[i].Item1].GetData().IsThreadedLoad = false;
				finishedChunks.Add((generatedChunks[meshed[i].Item1], meshed[i].Item2));
			}
		}

		private void ProcessGenerationQueue(Queue<ChunkPosition> chunksToGenerate, Dictionary<ChunkPosition, Chunk> generatedChunks)
		{
			while (chunksToGenerate.Count > 0)
			{
				var pos = chunksToGenerate.Dequeue();

				if (!generatedChunks.ContainsKey(pos))
				{
					Stopwatch watch = Stopwatch.StartNew();

					Chunk chunk = generator.GenerateChunk(pos)[0];
					chunk.GetData().IsThreadedLoad = true;
					//generated.Add((pos, generator.GenerateChunk(world, pos)));
					generatedChunks.Add(pos, chunk);
					watch.Stop();

					Console.WriteLine("Generated chunk at " + pos.X + ", " + pos.Y + ", " + pos.Z + " with - took " + watch.Elapsed.TotalSeconds);
				}

				chunkGenerationQueue--;
			}
		}

		private List<(ChunkPosition, ChunkMesh)> ProcessMeshQueue(CubeRegistry registry, Queue<ChunkPosition> chunksToMesh, Dictionary<ChunkPosition, Chunk> generatedChunks)
		{
			List<(ChunkPosition, ChunkMesh)> meshed = new List<(ChunkPosition, ChunkMesh)>();

			while (chunksToMesh.Count > 0)
			{
				var pos = chunksToMesh.Dequeue();

				if (generatedChunks.ContainsKey(pos))
				{
					Stopwatch watch = Stopwatch.StartNew();

					meshed.Add((pos, mesher.GenerateChunk(registry, generatedChunks[pos])));

					watch.Stop();

					Console.WriteLine("Meshed chunk at " + pos.X + ", " + pos.Y + ", " + pos.Z + " with " + ChunkData.ChunkUpdate + " updates - took " + watch.Elapsed.TotalSeconds);
				}

				chunkMeshingQueue--;
			}

			return meshed;
		}

		public List<(Chunk, ChunkMesh)> GetFinishedChunks()
		{
			if (thread.IsAlive)
				return null;

			List<(Chunk, ChunkMesh)> output = new List<(Chunk, ChunkMesh)>(finishedChunks);

			finishedChunks.Clear();

			return output;
		}
	}
}
