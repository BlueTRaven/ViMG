using BrUtility;
using BrUtility.Ported;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using ViMG.Cubes;

namespace ViMG
{
	public class ChunkManager
	{
		private struct ManagedChunk
		{
			public Chunk chunk;
			public ChunkMesh mesh;
			public Matrix transform;

			//public GenerationStep genStep;
			// If true, the mesh is dirty and must be regenerated (or generated.)
			public bool meshDirty;

			// In the queue to be generated
			public bool genQueued;
			public bool meshQueued;

			public ManagedChunk(GenericPool<ChunkData> chunkDatas, int x, int y, int z)
			{
				chunk = new Chunk(chunkDatas, x, y, z);
				mesh = null;

				transform = Matrix.Identity;

				meshDirty = true;
				//genStep = GenerationStep.Broad;
				genQueued = false;
				meshQueued = false;
			}
		}

		public readonly int sizeInChunks;
		public readonly int sizeInCubes;

		private ChunkGenerator generator;
		private ChunkMesher mesher;

		private ManagedChunk[,,] chunks;

		public GenericPool<ChunkData> ChunkDatas = new GenericPool<ChunkData>(() => new ChunkData());

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

		// Always meshed on the main thread, and before any others.
		private List<ChunkPosition> priorityMeshChunks = new List<ChunkPosition>();

		public static int QueueGenerate = 0;
		public static int QueueMesh = 0;
		public static int TotalQueueGenerate = 0;
		public static int TotalQueueMesh = 0;

		private ChunkerThread thread;

		//private Queue<ChunkMesh> unuploadedMeshes = new Queue<ChunkMesh>();

		public ChunkManager(GraphicsDevice device, int sizeInChunks, int sizeInCubes, World world)
		{
			generator = new ChunkGenerator();
			mesher = new ChunkMesher(device);

			this.sizeInChunks = sizeInChunks;
			this.sizeInCubes = sizeInCubes;

			chunks = new ManagedChunk[sizeInChunks, sizeInChunks, sizeInChunks];

			for (int x = 0; x < sizeInChunks; x++)
			{
				for (int y = 0; y < sizeInChunks; y++)
				{
					for (int z = 0; z < sizeInChunks; z++)
					{
						chunks[x, y, z] = new ManagedChunk(ChunkDatas, x, y, z);
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

			ProcessChunkQueueSync(world, 1, 5);
		}

		// Synchronously processess chunks in the queue.
		public void ProcessChunkQueueSync(World world, int maxGen = -1, int maxMesh = -1)
		{
			QueueGenerate = chunksToGenerateQueue.Count;
			QueueMesh = chunksToMeshQueue.Count;

			if (chunksToMeshQueue.Count > 0)
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

					if (chunks[pos.X, pos.Y, pos.Z].chunk.GetData().GenStep != ChunkData.GenerationStep.Done)
					{
						// Requeue - try again later.
						chunksToMeshQueue.Enqueue(pos);
						num++;
						continue;
						//throw new Exception("Cannot mesh chunk before it has been generated. Did you try to mark a chunk dirty before it has been generated?");
					}

					if (pos.X - 1 >= 0)
					{
						if (chunks[pos.X - 1, pos.Y, pos.Z].chunk.GetData().GenStep != ChunkData.GenerationStep.Done)
						{
							chunksToMeshQueue.Enqueue(pos);
							num++;
							continue;
						}
					}

					if (pos.Y - 1 >= 0)
					{
						if (chunks[pos.X, pos.Y - 1, pos.Z].chunk.GetData().GenStep != ChunkData.GenerationStep.Done)
						{
							chunksToMeshQueue.Enqueue(pos);
							num++;
							continue;
						}
					}

					if (pos.Z - 1 >= 0)
					{
						if (chunks[pos.X, pos.Y, pos.Z - 1].chunk.GetData().GenStep != ChunkData.GenerationStep.Done)
						{
							chunksToMeshQueue.Enqueue(pos);
							num++;
							continue;
						}
					}

					if (pos.X + 1 < Chunk.CHUNK_SIZE)
					{
						if (chunks[pos.X + 1, pos.Y, pos.Z].chunk.GetData().GenStep != ChunkData.GenerationStep.Done)
						{
							chunksToMeshQueue.Enqueue(pos);
							num++;
							continue;
						}
					}

					if (pos.Y + 1 < Chunk.CHUNK_SIZE)
					{
						if (chunks[pos.X, pos.Y + 1, pos.Z].chunk.GetData().GenStep != ChunkData.GenerationStep.Done)
						{
							chunksToMeshQueue.Enqueue(pos);
							num++;
							continue;
						}
					}

					if (pos.Z + 1 < Chunk.CHUNK_SIZE)
					{
						if (chunks[pos.X, pos.Y, pos.Z + 1].chunk.GetData().GenStep != ChunkData.GenerationStep.Done)
						{
							chunksToMeshQueue.Enqueue(pos);
							num++;
							continue;
						}
					}

					MeshChunk(world, pos);
					chunksToMeshAlreadyAdded.Remove(pos);

					num++;
				}

				//chunksToMesh = queue.ToList();
				//chunksToMesh.Clear();
			}

			if (chunksToGenerateQueue.Count > 0)
			{
				int num = 0;
				while (chunksToGenerateQueue.Count > 0 && (maxGen == -1 || num < maxGen))
				{
					var pos = chunksToGenerateQueue.Dequeue();

					// This check is necessary for 'cascading' generation to work.
					if (chunks[pos.X, pos.Y, pos.Z].chunk.GetData().GenStep == ChunkData.GenerationStep.Done)
						continue;	//we've already generated this chunk

					Stopwatch watch = Stopwatch.StartNew();

					GenerateChunk(world, pos);
					watch.Stop();

					Console.WriteLine("Generated chunk at " + pos.X + ", " + pos.Y + ", " + pos.Z + ". Took " + watch.Elapsed.TotalSeconds);


					num++;
				}
			}
		}

		private void GenerateChunk(World world, ChunkPosition position)
		{
			if (chunks[position.X, position.Y, position.Z].chunk.GetData().GenStep == ChunkData.GenerationStep.Broad)
			{
				GenerateChunkBroad(position);
				GenerateChunkDetail(world, position);
			}
			else if (chunks[position.X, position.Y, position.Z].chunk.GetData().GenStep == ChunkData.GenerationStep.Detail)
			{
				GenerateChunkDetail(world, position);
			}

			chunks[position.X, position.Y, position.Z].genQueued = false;
		}

		public void GenerateChunkBroad(ChunkPosition position)
		{
			Chunk chunk = generator.MakeChunk(ChunkDatas, position);
			generator.GenerateChunkBroad(chunk, position);

			chunks[position.X, position.Y, position.Z].chunk = chunk;
			//chunks[position.X, position.Y, position.Z].genStep = GenerationStep.Detail;
		}

		public void GenerateChunkDetail(World world, ChunkPosition position)
		{
			Chunk chunk = chunks[position.X, position.Y, position.Z].chunk;
			generator.GenerateChunkDetail(this, chunk, position);

			//chunks[position.X, position.Y, position.Z].genStep = GenerationStep.Done;
			chunks[position.X, position.Y, position.Z].chunk.Initialize(world);

			chunks[position.X, position.Y, position.Z].meshDirty = true;
			chunks[position.X, position.Y, position.Z].meshQueued = true;
			chunksToMeshQueue.Enqueue(position);
		}

		private void MeshChunk(World world, ChunkPosition pos)
		{
			Stopwatch watch = Stopwatch.StartNew();

			chunks[pos.X, pos.Y, pos.Z].mesh = mesher.GenerateChunk(chunks[pos.X, pos.Y, pos.Z].chunk, world, true);
			chunks[pos.X, pos.Y, pos.Z].meshDirty = false;
			chunks[pos.X, pos.Y, pos.Z].meshQueued = false;

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

					if (chunks[pos.X, pos.Y, pos.Z].chunk.GetData().GenStep != ChunkData.GenerationStep.Done)
					{
						throw new Exception("Cannot mesh chunk before it has been generated. Did you try to mark a chunk dirty before it has been generated?");
					}

					MeshChunk(world, pos);
				}

				priorityMeshChunks.Clear();
			}
		}

		public bool IsChunkGenerated(CubePosition position)
		{
			ChunkPosition chunkPos = ChunkPosition.CubeChunk(position);

			return chunks[chunkPos.X, chunkPos.Y, chunkPos.Z].chunk.GetData().GenStep == ChunkData.GenerationStep.Done;
		}

		public bool IsChunkGenerated(ChunkPosition position)
		{
			return chunks[position.X, position.Y, position.Z].chunk.GetData().GenStep == ChunkData.GenerationStep.Done;
		}

		public ChunkData.GenerationStep GetChunkGenerationStep(CubePosition position)
		{
			ChunkPosition chunkPos = ChunkPosition.CubeChunk(position);

			return chunks[chunkPos.X, chunkPos.Y, chunkPos.Z].chunk.GetData().GenStep;
		}

		public ChunkData.GenerationStep GetChunkGenerationStep(ChunkPosition position)
		{
			return chunks[position.X, position.Y, position.Z].chunk.GetData().GenStep;
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

		public bool IsInWorldBounds(Vector3 position)
		{
			return IsInWorldBounds(CubePosition.FromWorldSpace(position));
		}

		public bool IsInWorldBounds(CubePosition position)
		{
			if (position.Coord == CubePosition.CoordinateSpace.ChunkSpace)
				return false;
			else
			{
				return position.X >= 0 && position.X < sizeInCubes &&
					position.Y >= 0 && position.Y < sizeInCubes &&
					position.Z >= 0 && position.Z < sizeInCubes;
			}
		}

		public bool IsInWorldBounds(ChunkPosition position)
		{
			return position.X >= 0 && position.X < sizeInChunks &&
					position.Y >= 0 && position.Y < sizeInChunks &&
					position.Z >= 0 && position.Z < sizeInChunks;
		}

		// Takes a world space position.
		public int GetRaw(Vector3 position)
		{
			return GetRaw(CubePosition.FromWorldSpace(position));
		}

		public int GetRaw(CubePosition position)
		{
			if (position.Coord == CubePosition.CoordinateSpace.ChunkSpace)
			{
				Console.WriteLine("Warning: cannot use World.GetRaw with Chunk Space CubePosition.");
				return -1;
			}

			if (IsInWorldBounds(position))
				return GetChunk(position).GetData().GetRaw(position);
			else return -1;
		}

		public int GetRaw(int x, int y, int z)
		{
			return GetRaw(new CubePosition(x, y, z));
		}

		public Optional<Cube> GetCube(CubePosition position)
		{
			if (IsInWorldBounds(position))
			{
				Chunk chunk = GetChunk(position);

				if (chunk == null)
					return new Optional<Cube>();

				return chunk.GetData().GetCube(position);
			}

			return new Optional<Cube>();
		}

		public Optional<Cube> GetCube(Vector3 position)
		{
			return GetCube(CubePosition.FromWorldSpace(position));
		}

		public Optional<Cube> GetCube(int x, int y, int z)
		{
			return GetCube(new CubePosition(x, y, z));
		}

		public Cube.CubeInstance GetCubeInstance(CubePosition position)
		{
			if (IsInWorldBounds(position))
			{
				Chunk chunk = GetChunk(position);

				if (chunk == null)
					return new Cube.CubeInstance();

				return chunk.GetData().GetCubeInstance(position);
			}
			else return new Cube.CubeInstance();
		}

		public Cube.CubeInstance GetCubeInstance(int x, int y, int z)
		{
			return GetCubeInstance(new CubePosition(x, y, z));
		}

		public Matrix GetTransform(ChunkPosition position)
		{
			return chunks[position.X, position.Y, position.Z].transform;
		}

		public void MarkGenerateDirty(ChunkPosition position)
		{
			// Already queued, ignore
			if (chunks[position.X, position.Y, position.Z].genQueued)
				return;

			if (chunks[position.X, position.Y, position.Z].chunk.GetData().GenStep == ChunkData.GenerationStep.Done)
				throw new Exception("Tried to mark a chunk to generate after it has already been generated.");

			// supports both generation step broad and detail, but not done.
			//chunks[position.X, position.Y, position.Z].genStep = GenerationStep.Broad;
			chunks[position.X, position.Y, position.Z].genQueued = true;
			chunksToGenerateQueue.Enqueue(position);
		}

		// Marks a chunk as dirty, meaning it needs to be remeshed.
		public void MarkDirty(ChunkPosition position)
		{
			chunks[position.X, position.Y, position.Z].meshDirty = true;
			/*if (chunksToMeshQueue.Count > 15 || chunksToGenerateQueue.Count > 2)
				priorityMeshChunks.Add(position);
			else
			{*/
				if (!chunksToMeshAlreadyAdded.Contains(position))
				{
					chunksToMeshQueue.Enqueue(position);
					chunksToMeshAlreadyAdded.Add(position);
				}
			//}
		}

		public void MarkDirty(int x, int y, int z)
		{
			MarkDirty(new ChunkPosition(x, y, z));
		}
	}
}
