using BrUtility.Ported;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace ViMG
{
	public class ChunkManager
	{
		public struct ManagedChunk
		{
			public Chunk chunk;
			public ChunkMesh mesh;
			public Matrix transform;

			public bool genDirty;
			// If true, the mesh is dirty and must be regenerated (or generated.)
			public bool meshDirty;

			// In the queue to be generated
			public bool genQueued;
			public bool meshQueued;

			public ManagedChunk(World world, int x, int y, int z)
			{
				chunk = new Chunk(x, y, z);
				mesh = null;

				transform = Matrix.Identity;

				meshDirty = true;
				genDirty = true;
				genQueued = false;
				meshQueued = false;
			}
		}

		private ChunkGenerator generator;
		private ChunkMesher mesher;

		private ManagedChunk[,,] chunks;

		private PriorityQueue<ChunkPosition> chunksToMeshQueue = new PriorityQueue<ChunkPosition>(true, (x) => 
		{
			return (int)(-Main.camera.Position - x.InWorldSpace()).Length(); 
		});
		private HashSet<ChunkPosition> chunksToMeshAlreadyAdded = new HashSet<ChunkPosition>();
		private PriorityQueue<ChunkPosition> chunksToGenerateQueue = new PriorityQueue<ChunkPosition>(true, (x) =>
		{
			return (int)(-Main.camera.Position - x.InWorldSpace()).Length();
		});
		private HashSet<ChunkPosition> chunksToGenerateAlreadyAdded = new HashSet<ChunkPosition>();

		//private List<ChunkPosition> chunksToGenerate = new List<ChunkPosition>();
		//private List<ChunkPosition> chunksToMesh = new List<ChunkPosition>();

		// Always meshed on the main thread, and before any others.
		private List<ChunkPosition> priorityMeshChunks = new List<ChunkPosition>();

		public static int QueueGenerate = 0;
		public static int QueueMesh = 0;
		public static int TotalQueueGenerate = 0;
		public static int TotalQueueMesh = 0;

		private ChunkerThread thread;

		//private Queue<ChunkMesh> unuploadedMeshes = new Queue<ChunkMesh>();

		public ChunkManager(GraphicsDevice device, int chunkSize, World world)
		{
			generator = new ChunkGenerator();
			mesher = new ChunkMesher(device);

			chunks = new ManagedChunk[chunkSize, chunkSize, chunkSize];

			for (int x = 0; x < chunkSize; x++)
			{
				for (int y = 0; y < chunkSize; y++)
				{
					for (int z = 0; z < chunkSize; z++)
					{
						chunks[x, y, z] = new ManagedChunk(world, x, y, z);
					}
				}
			}
		}

		public void Initialize(World world)
		{
			generator.Initialize(world);
			thread = new ChunkerThread(generator, mesher);
		}

		public void ProcessChunkQueue(World world, int forceMode)
		{
			ProcessPriorityMeshChunks(world);

			if (forceMode == 0)
			{
				ProcessChunkQueueSync(world, 1, 5);
				// Decide for ourselves

				/*if (chunksToGenerate.Count < 5 && chunksToMesh.Count < 15)
					//ProcessChunkQueueSync(world);
				else ProcessChunkQueueThreaded(world);*/
			}
			else if (forceMode == 1)
			{
				//ProcessChunkQueueThreaded(world);
			}
			else if (forceMode == 2)
			{
				//ProcessChunkQueueSync(world);
			}
		}

		// Uses the ChunkerThread to process chunks.
		/*public void ProcessChunkQueueThreaded(World world)
		{
			// Process newly finished chunks
			if (!thread.IsAlive && thread.HasFinishedChunks)
			{
				var chunks = thread.GetFinishedChunks();

				foreach (var pair in chunks)
				{
					ref ManagedChunk chunk = ref this.chunks[pair.Item1.Position.X, pair.Item1.Position.Y, pair.Item1.Position.Z];

					chunk.genDirty = false;
					chunk.genQueued = false;
					chunk.meshDirty = false;
					chunk.meshQueued = false;

					chunk.chunk = pair.Item1;
					chunk.mesh = pair.Item2;
					//unuploadedMeshes.Enqueue(chunk.mesh);
					chunk.chunk.Initialize(world);
				}
			}

			//upload meshes ourselves
			/*const int maxUploadPerFrame = 1;
			int num = 0;
			while (num < maxUploadPerFrame && unuploadedMeshes.Count > 0)
			{
				var unuploadedMesh = unuploadedMeshes.Dequeue();
				if (!unuploadedMesh.Uploaded)
					unuploadedMesh.Upload();

				num++;
			}

			if (!thread.IsAlive && (chunksToGenerate.Count > 0 || chunksToMesh.Count > 0))
			{
				Console.WriteLine("To Generate: " + chunksToGenerate.Count + " chunks.\n" +
					"To Mesh: " + chunksToMesh.Count + " chunks.");

				Dictionary<ChunkPosition, Chunk> alreadyGeneratedChunks = new Dictionary<ChunkPosition, Chunk>();

				foreach (ChunkPosition pos in chunksToMesh)
				{
					if (!chunks[pos.X, pos.Y, pos.Z].genDirty && chunks[pos.X, pos.Y, pos.Z].chunk != null && !alreadyGeneratedChunks.ContainsKey(pos))
						alreadyGeneratedChunks.Add(pos, chunks[pos.X, pos.Y, pos.Z].chunk);
				}

				Stopwatch sortTimer = Stopwatch.StartNew();

				ChunkPosition cameraPosition = ChunkPosition.WorldSpaceChunk(-Main.camera.Position);

				List<ChunkPosition> chunksToGenerateSorted = chunksToGenerate.Distinct().OrderBy(x => 
				{
					return (new Vector3(x.X, x.Y, x.Z) - new Vector3(cameraPosition.X, cameraPosition.Y, cameraPosition.Z)).Length(); 
				}).ToList();
				List<ChunkPosition> chunksToMeshSorted = chunksToMesh.Distinct().OrderBy(x => 
				{
					return (new Vector3(x.X, x.Y, x.Z) - new Vector3(cameraPosition.X, cameraPosition.Y, cameraPosition.Z)).Length();
				}).ToList();

				sortTimer.Stop();
				
				Console.WriteLine("Sorting chunks to generate/mesh: " + sortTimer.Elapsed.TotalSeconds);

				const int maxChunksPerThread = 80;

				if (chunksToGenerate.Count > maxChunksPerThread)
				{
					Console.WriteLine("Chunks to generate over " + maxChunksPerThread + ", splitting for later...");
					
					List<ChunkPosition> clampedChunksToGenerate = new List<ChunkPosition>();
					List<ChunkPosition> leftover = new List<ChunkPosition>();

					for (int i = 0; i < maxChunksPerThread; i++)
					{
						clampedChunksToGenerate.Add(chunksToGenerateSorted[i]);
					}

					for (int i = maxChunksPerThread; i < chunksToGenerateSorted.Count; i++)
					{
						leftover.Add(chunksToGenerateSorted[i]);
					}

					chunksToGenerate = leftover;
					chunksToGenerateSorted = clampedChunksToGenerate;
				}
				else chunksToGenerate.Clear();

				if (chunksToMesh.Count > maxChunksPerThread)
				{
					Console.WriteLine("Chunks to mesh over " + maxChunksPerThread + ", splitting for later...");

					List<ChunkPosition> clampedChunksToMesh = new List<ChunkPosition>();
					List<ChunkPosition> leftover = new List<ChunkPosition>();

					for (int i = 0; i < maxChunksPerThread; i++)
					{
						clampedChunksToMesh.Add(chunksToMeshSorted[i]);
					}

					for (int i = maxChunksPerThread; i < chunksToMeshSorted.Count; i++)
					{
						leftover.Add(chunksToMeshSorted[i]);
					}

					chunksToMesh = leftover;
					chunksToMeshSorted = clampedChunksToMesh;
				}
				else chunksToMesh.Clear();

				TotalQueueGenerate = chunksToGenerate.Count;
				TotalQueueMesh = chunksToMesh.Count;

				thread.Start(world.CubeRegistry, new Queue<ChunkPosition>(chunksToGenerateSorted), new Queue<ChunkPosition>(chunksToMeshSorted), alreadyGeneratedChunks);
			}
		}*/

		// Synchronously processess chunks in the queue.
		public void ProcessChunkQueueSync(World world, int maxGen = -1, int maxMesh = -1)
		{
			QueueGenerate = chunksToGenerateQueue.Count;
			QueueMesh = chunksToMeshAlreadyAdded.Count;
			if (chunksToGenerateQueue.Count > 0)
			{
				int num = 0;
				while (chunksToGenerateQueue.Count > 0 && (maxGen == -1 || num < maxGen))
				{
					var pos = chunksToGenerateQueue.Dequeue();

					Stopwatch watch = Stopwatch.StartNew();

					chunks[pos.X, pos.Y, pos.Z].chunk = generator.GenerateChunk(pos)[0];
					chunks[pos.X, pos.Y, pos.Z].chunk.Initialize(world);
					chunks[pos.X, pos.Y, pos.Z].genDirty = false;

					MeshChunk(world, pos);

					watch.Stop();

					Console.WriteLine("Generated chunk at " + pos.X + ", " + pos.Y + ", " + pos.Z + " with - took " + watch.Elapsed.TotalSeconds);

					chunksToGenerateAlreadyAdded.Remove(pos);

					num++;
				}

				//chunksToGenerate = queue.ToList();
				//chunksToMesh = queue.ToList();
				//chunksToGenerate.Clear();
			}
			else if (chunksToMeshQueue.Count > 0)
			{
				// we only manually process the meshing chunks if we don't have any to generate.
				//Queue<ChunkPosition> queue = new Queue<ChunkPosition>(chunksToMesh.Distinct());
				int num = 0;

				while (chunksToMeshQueue.Count > 0 && (maxMesh == -1 || num < maxMesh))
				{
					var pos = chunksToMeshQueue.Dequeue();

					if (!chunks[pos.X, pos.Y, pos.Z].meshDirty)
					{
						// Already meshed, remove from list
						continue;
					}

					if (chunks[pos.X, pos.Y, pos.Z].genDirty)
					{
						// Requeue - try again later.
						chunksToMeshQueue.Enqueue(pos);
						num++;
						continue;
						//throw new Exception("Cannot mesh chunk before it has been generated. Did you try to mark a chunk dirty before it has been generated?");
					}

					MeshChunk(world, pos);
					chunksToMeshAlreadyAdded.Remove(pos);

					num++;
				}

				//chunksToMesh = queue.ToList();
				//chunksToMesh.Clear();
			}
		}

		private void MeshChunk(World world, ChunkPosition pos)
		{
			Stopwatch watch = Stopwatch.StartNew();

			chunks[pos.X, pos.Y, pos.Z].mesh = mesher.GenerateChunk(world.CubeRegistry, chunks[pos.X, pos.Y, pos.Z].chunk);
			chunks[pos.X, pos.Y, pos.Z].meshDirty = false;

			watch.Stop();

			Console.WriteLine("Meshed chunk at " + pos.X + ", " + pos.Y + ", " + pos.Z + " with " + ChunkData.ChunkUpdate + " updates - took " + watch.Elapsed.TotalSeconds);
		}

		public void ProcessPriorityMeshChunks(World world)
		{
			if (priorityMeshChunks.Count > 0)
			{
				Queue<ChunkPosition> queue = new Queue<ChunkPosition>(priorityMeshChunks.Distinct());

				while (queue.Count > 0)
				{
					var pos = queue.Dequeue();

					if (chunks[pos.X, pos.Y, pos.Z].genDirty)
					{
						throw new Exception("Cannot mesh chunk before it has been generated. Did you try to mark a chunk dirty before it has been generated?");
					}

					Stopwatch watch = Stopwatch.StartNew();

					chunks[pos.X, pos.Y, pos.Z].mesh = mesher.GenerateChunk(world.CubeRegistry, chunks[pos.X, pos.Y, pos.Z].chunk);
					chunks[pos.X, pos.Y, pos.Z].meshDirty = false;

					watch.Stop();

					Console.WriteLine("Meshed chunk at " + pos.X + ", " + pos.Y + ", " + pos.Z + " with " + ChunkData.ChunkUpdate + " updates - took " + watch.Elapsed.TotalSeconds);
				}

				priorityMeshChunks.Clear();
			}
		}

		public bool IsChunkGenerated(CubePosition position)
		{
			ChunkPosition chunkPos = ChunkPosition.CubeChunk(position);

			return !chunks[chunkPos.X, chunkPos.Y, chunkPos.Z].genDirty || chunks[position.X, position.Y, position.Z].genQueued;
		}

		public bool IsChunkGenerated(ChunkPosition position)
		{
			return !chunks[position.X, position.Y, position.Z].genDirty || chunks[position.X, position.Y, position.Z].genQueued;
		}

		public Chunk GetChunk(CubePosition position)
		{
			ChunkPosition chunkPos = ChunkPosition.CubeChunk(position);

			return chunks[chunkPos.X, chunkPos.Y, chunkPos.Z].chunk;
		}

		public Chunk GetChunk(int x, int y, int z)
		{
			return chunks[x, y, z].chunk;
		}

		public Chunk GetChunk(ChunkPosition position)
		{
			return chunks[position.X, position.Y, position.Z].chunk;
		}

		public ChunkMesh GetMesh(ChunkPosition position)
		{
			return chunks[position.X, position.Y, position.Z].mesh;
		}

		public ChunkMesh GetMesh(int x, int y, int z)
		{
			return chunks[x, y, z].mesh;
		}

		public Matrix GetTransform(ChunkPosition position)
		{
			return chunks[position.X, position.Y, position.Z].transform;
		}

		public void MarkGenerateDirty(ChunkPosition position)
		{
			chunks[position.X, position.Y, position.Z].genDirty = true;
			chunks[position.X, position.Y, position.Z].meshDirty = true;
			chunks[position.X, position.Y, position.Z].genQueued = true;
			chunks[position.X, position.Y, position.Z].meshQueued = true;
			chunksToGenerateQueue.Enqueue(position);
			//note that we don't actually add this to the list of chunks to mesh. The generation method will also mesh it.
		}

		// Marks a chunk as dirty, meaning it needs to be remeshed.
		public void MarkDirty(ChunkPosition position)
		{
			chunks[position.X, position.Y, position.Z].meshDirty = true;
			if (chunksToMeshQueue.Count > 15 || chunksToGenerateQueue.Count > 2)
				priorityMeshChunks.Add(position);
			else
			{
				if (!chunksToMeshAlreadyAdded.Contains(position))
				{
					chunksToMeshQueue.Enqueue(position);
					chunksToMeshAlreadyAdded.Add(position);
				}
			}
		}

		public void MarkDirty(int x, int y, int z)
		{
			MarkDirty(new ChunkPosition(x, y, z));
		}
	}
}
