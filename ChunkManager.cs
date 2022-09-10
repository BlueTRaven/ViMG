using BrUtility;
using BrUtility.Ported;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Cubes;
using ViMG.Generation;

namespace ViMG
{
	public class ChunkManager
	{
		private readonly struct BroadChunkTaskState
		{
			public readonly int chunkStart;
			public readonly int chunkEnd;
			public readonly int totalChunks;
			public readonly ManagedChunk[] chunks;
			public readonly ChunkGenerator generator;

			public BroadChunkTaskState(int chunkStart, int chunkEnd, int totalChunks, ManagedChunk[] chunks, ChunkGenerator generator)
            {
                this.chunkStart = chunkStart;
                this.chunkEnd = chunkEnd;
                this.totalChunks = totalChunks;
                this.chunks = chunks;
				this.generator = generator;
            }
		};

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

			public ManagedChunk(Chunk defaultChunk, int x, int y, int z)
			{
				chunk = defaultChunk;
				//chunk = new Chunk(chunkDatas, x, y, z);
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
		private ChunkGenerationThread genThread;
		private ChunkGenerationThreadDataBus dataBus;

		private ManagedChunk[] chunks;
		private HashSet<ChunkPosition> modifiedChunks = new HashSet<ChunkPosition>();

		public GenericPool<ChunkData> ChunkDatas = new GenericPool<ChunkData>(() => new ChunkData());

		private PriorityQueue<ChunkPosition> chunksToMeshQueue = new PriorityQueue<ChunkPosition>(true, (x) => 
		{
			return (int)(Main.camera.Position - x.InWorldSpace()).Length(); 
		});
		private HashSet<ChunkPosition> chunksToMeshAlreadyAdded = new HashSet<ChunkPosition>();
		private PriorityQueue<ChunkPosition> chunksToGenerateQueue = new PriorityQueue<ChunkPosition>(true, (x) =>
		{
			return (int)(Main.camera.Position - x.InWorldSpace()).Length();
		});
		private HashSet<ChunkPosition> chunksToGenerateAlreadyAdded = new HashSet<ChunkPosition>();

		// Always meshed on the main thread, and before any others.
		private List<ChunkPosition> priorityMeshChunks = new List<ChunkPosition>();

		public static int QueueGenerate = 0;
		public static int QueueMesh = 0;
		public static int TotalQueueGenerate = 0;
		public static int TotalQueueMesh = 0;

		//private Queue<ChunkMesh> unuploadedMeshes = new Queue<ChunkMesh>();

		public ChunkManager(GraphicsDevice device, int sizeInChunks, int sizeInCubes, World world)
		{
			generator = new ChunkGeneratorIsland();
			//generator = new ChunkGeneratorFlat();
			mesher = new ChunkMesher(device);

			//dataBus = new ChunkGenerationThreadDataBus(this);
			//genThread = new ChunkGenerationThread(generator, dataBus);
			this.sizeInChunks = sizeInChunks;
			this.sizeInCubes = sizeInCubes;

			chunks = new ManagedChunk[sizeInChunks * sizeInChunks * sizeInChunks];

			for (int i = 0; i < chunks.Length; i++)
			{
				int x = i % sizeInChunks;
				int y = (i / sizeInChunks) % sizeInChunks;
				int z = i / (sizeInChunks * sizeInChunks);

				chunks[i] = new ManagedChunk(generator.MakeChunk(this, new ChunkPosition(x, y, z)), x, y, z);
			}

			/*for (int x = 0; x < sizeInChunks; x++)
			{
				for (int y = 0; y < sizeInChunks; y++)
				{
					for (int z = 0; z < sizeInChunks; z++)
					{
						chunks[x, y, z] = new ManagedChunk(ChunkDatas, defaultChunk, x, y, z);
					}
				}
			}*/
		}

		public void Initialize(World world)
		{
			//generator.Initialize(world);
			//genThread.Start();
		}

		public void GenerateWorld(World world)
		{
			int num = 0;
			int total = sizeInChunks * sizeInChunks * sizeInChunks;

			Stopwatch totalWatch = Stopwatch.StartNew();

			generator.Initialize(world);

			Stopwatch broadWatch = Stopwatch.StartNew();

			List<Task> broadPhaseTasks = new List<Task>();

			const int split = 8;

			for (int i = 0; i < total; i += split) 
			{
				int chunkStart = i;
				int chunkEnd = i + split;

				BroadChunkTaskState state = new BroadChunkTaskState(chunkStart, chunkEnd, total, chunks, generator);

				Task task = new Task(GenerateChunkDetailTaskFn, state);

				task.Start();
				broadPhaseTasks.Add(task);
			}

			broadPhaseTasks.ForEach(x => {
				x.Wait();
			});

			broadPhaseTasks = null;

			broadWatch.Stop();
			Console.WriteLine("Finished Broad Phase. Generated {0} total chunks in {1} seconds. ({2} seconds elapsed since start.)", 
				total, broadWatch.Elapsed.Seconds, totalWatch.Elapsed.TotalSeconds);

			if (Main.DO_DETAIL)
			{
				num = 0;
				for (int i = 0; i < total; i++)
				{
					int x = i % sizeInChunks;
					int y = (i / sizeInChunks) % sizeInChunks;
					int z = i / (sizeInChunks * sizeInChunks);

					generator.GenerateChunkDetail(this, chunks[i].chunk, new ChunkPosition(x, y, z));

					num++;

					if (num % sizeInChunks * sizeInChunks == 0)
						Console.WriteLine("Detail: " + num + " / " + total);
				}
			}

			Stopwatch detailWatch = Stopwatch.StartNew();

			num = 0;
			for (int i = 0; i < total; i++)
			{
				int x = i % sizeInChunks;
				int y = (i / sizeInChunks) % sizeInChunks;
				int z = i / (sizeInChunks * sizeInChunks);

				chunks[i].chunk.Initialize(world);
				chunks[i].chunk.PostChunkGen(world);

				//MarkDirty(new ChunkPosition(x, y, z), false);
				num++;

				if (num % sizeInChunks * sizeInChunks == 0)
					Console.WriteLine("Init: " + num + " / " + total);
			}

			detailWatch.Stop();

			Console.WriteLine("Finished Detail Phase. Generated {0} total chunks in {1} seconds. ({2} seconds elapsed since start.)", 
				total, detailWatch.Elapsed.Seconds, totalWatch.Elapsed.TotalSeconds);

			chunksToMeshQueue.Sort();

			totalWatch.Stop();

			Console.WriteLine("Finished. Generated {0} total chunks in {1} seconds.", total, totalWatch.Elapsed.TotalSeconds);
		}

		private static void GenerateChunkDetailTaskFn(object obj)
		{
			BroadChunkTaskState state = (BroadChunkTaskState)obj;
			int split = state.chunkEnd - state.chunkStart;

			for (int j = state.chunkStart; j < state.chunkEnd; j++)
			{
				state.generator.GenerateChunkBroad(state.chunks[j].chunk);

				if (!Main.DO_DETAIL)
					state.chunks[j].chunk.GetData().GenStep = ChunkData.GenerationStep.Done;
				else state.chunks[j].chunk.GetData().GenStep = ChunkData.GenerationStep.Detail;
			}

			Console.WriteLine("Broad task {0}/{1} finished.", state.chunkStart / split, state.totalChunks / split);
		}

		public Vector3 GetPlayerSpawnPos(World world)
        {
			return generator.GetPlayerPosition(world, this);
        }

		public void ProcessChunkQueue(World world, int forceMode)
		{
			chunksToMeshQueue.Sort();

			//ProcessPriorityMeshChunks(world);

			ProcessChunkQueueSync(world, 1, 1);
		}

		public void WaitForFinishGenerate(World world)
		{
			while (dataBus.HasChunksToGenerate())
			{
				ProcessChunkQueueSync(world);
			}
		}

		private int PosToIndex(ChunkPosition position)
		{
			return position.X + sizeInChunks * (position.Y + sizeInChunks * position.Z);
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

					var c = chunks[PosToIndex(pos)];

					if (!c.meshDirty)
					{
						// Already meshed, remove from list
						continue;
					}

					if (c.chunk.GetData().GenStep != ChunkData.GenerationStep.Done)
					{
						// Requeue - try again later.
						chunksToMeshQueue.Enqueue(pos);
						num++;
						continue;
						//throw new Exception("Cannot mesh chunk before it has been generated. Did you try to mark a chunk dirty before it has been generated?");
					}

					if (pos.X - 1 >= 0)
					{
						if (chunks[PosToIndex(new ChunkPosition(pos.X - 1, pos.Y, pos.Z))].chunk.GetData().GenStep != ChunkData.GenerationStep.Done)
						{
							chunksToMeshQueue.Enqueue(pos);
							num++;
							continue;
						}
					}

					if (pos.Y - 1 >= 0)
					{
						if (chunks[PosToIndex(new ChunkPosition(pos.X, pos.Y - 1, pos.Z))].chunk.GetData().GenStep != ChunkData.GenerationStep.Done)
						{
							chunksToMeshQueue.Enqueue(pos);
							num++;
							continue;
						}
					}

					if (pos.Z - 1 >= 0)
					{
						if (chunks[PosToIndex(new ChunkPosition(pos.X, pos.Y, pos.Z - 1))].chunk.GetData().GenStep != ChunkData.GenerationStep.Done)
						{
							chunksToMeshQueue.Enqueue(pos);
							num++;
							continue;
						}
					}

					if (pos.X + 1 < Chunk.CHUNK_SIZE)
					{
						if (chunks[PosToIndex(new ChunkPosition(pos.X + 1, pos.Y, pos.Z))].chunk.GetData().GenStep != ChunkData.GenerationStep.Done)
						{
							chunksToMeshQueue.Enqueue(pos);
							num++;
							continue;
						}
					}

					if (pos.Y + 1 < Chunk.CHUNK_SIZE)
					{
						if (chunks[PosToIndex(new ChunkPosition(pos.X, pos.Y + 1, pos.Z))].chunk.GetData().GenStep != ChunkData.GenerationStep.Done)
						{
							chunksToMeshQueue.Enqueue(pos);
							num++;
							continue;
						}
					}

					if (pos.Z + 1 < Chunk.CHUNK_SIZE)
					{
						if (chunks[PosToIndex(new ChunkPosition(pos.X, pos.Y, pos.Z + 1))].chunk.GetData().GenStep != ChunkData.GenerationStep.Done)
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
					if (chunks[PosToIndex(pos)].chunk.GetData().GenStep == ChunkData.GenerationStep.Done)
						continue;	//we've already generated this chunk

					Stopwatch watch = Stopwatch.StartNew();

					GenerateChunk(world, pos);
					watch.Stop();

					Console.WriteLine("Generated chunk at " + pos.X + ", " + pos.Y + ", " + pos.Z + ". Took " + watch.Elapsed.TotalSeconds);


					num++;
				}
			}
		}

		public Chunk[] GetChunks()
		{
			Chunk[] allChunks = new Chunk[chunks.Length];
			for (int i = 0; i < chunks.Length; i++)
			{
				allChunks[i] = chunks[i].chunk;
			}

			return allChunks;
		}

		private void GenerateChunk(World world, ChunkPosition position)
		{
			if (chunks[PosToIndex(position)].chunk.GetData().GenStep == ChunkData.GenerationStep.Broad)
			{
				GenerateChunkBroad(position);
				GenerateChunkDetail(world, position);
			}
			else if (chunks[PosToIndex(position)].chunk.GetData().GenStep == ChunkData.GenerationStep.Detail)
			{
				GenerateChunkDetail(world, position);
			}

			chunks[PosToIndex(position)].genQueued = false;
		}

		public void GenerateChunkBroad(ChunkPosition position)
		{
			Chunk chunk = generator.MakeChunk(this, position);
			generator.GenerateChunkBroad(chunk);

			chunks[PosToIndex(position)].chunk = chunk;
			//chunks[position.X, position.Y, position.Z].genStep = GenerationStep.Detail;
		}

		public void GenerateChunkDetail(World world, ChunkPosition position)
		{
			Chunk chunk = chunks[PosToIndex(position)].chunk;
			generator.GenerateChunkDetail(this, chunk, position);

			int index = PosToIndex(position);

			//chunks[position.X, position.Y, position.Z].genStep = GenerationStep.Done;
			chunks[index].chunk.Initialize(world);
			chunks[index].chunk.PostChunkGen(world);

			chunks[index].meshDirty = true;
			chunks[index].meshQueued = true;
			chunksToMeshQueue.Enqueue(position);
		}

		private void MeshChunk(World world, ChunkPosition pos)
		{
			Stopwatch watch = Stopwatch.StartNew();

			chunks[PosToIndex(pos)].mesh = mesher.GenerateChunk(chunks[PosToIndex(pos)].chunk, world, true);
			chunks[PosToIndex(pos)].meshDirty = false;
			chunks[PosToIndex(pos)].meshQueued = false;

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

					if (chunks[PosToIndex(pos)].chunk.GetData().GenStep != ChunkData.GenerationStep.Done)
					{
						throw new Exception("Cannot mesh chunk before it has been generated. Did you try to mark a chunk dirty before it has been generated?");
					}

					MeshChunk(world, pos);
				}

				priorityMeshChunks.Clear();
			}
		}

		#region Get Things
		public HashSet<ChunkPosition> GetModifiedChunks()
		{
			return modifiedChunks;
		}

		public void ResetModifiedChunks()
		{
			modifiedChunks.Clear();
		}

		public bool IsChunkGenerated(CubePosition position)
		{
			ChunkPosition chunkPos = ChunkPosition.CubeChunk(position);

			return chunks[PosToIndex(chunkPos)].chunk.GetData().GenStep == ChunkData.GenerationStep.Done;
		}

		public bool IsChunkGenerated(ChunkPosition position)
		{
			return chunks[PosToIndex(position)].chunk.GetData().GenStep == ChunkData.GenerationStep.Done;
		}

		public ChunkData.GenerationStep GetChunkGenerationStep(CubePosition position)
		{
			ChunkPosition chunkPos = ChunkPosition.CubeChunk(position);

			return chunks[PosToIndex(chunkPos)].chunk.GetData().GenStep;
		}

		public ChunkData.GenerationStep GetChunkGenerationStep(ChunkPosition position)
		{
			return chunks[PosToIndex(position)].chunk.GetData().GenStep;
		}

		public void SetChunk(Chunk chunk)
		{
			chunks[PosToIndex(chunk.Position)].chunk = chunk;
		}

		public Chunk GetChunk(CubePosition position)
		{
			ChunkPosition chunkPos = ChunkPosition.CubeChunk(position);

			int ind = PosToIndex(chunkPos);

			if (ind < 0 || ind >= chunks.Length)
				return null;
			else return chunks[ind].chunk;
		}

		public Chunk GetChunk(int x, int y, int z)
		{
			return chunks[PosToIndex(new ChunkPosition(x, y, z))].chunk;
		}

		public Chunk GetChunk(ChunkPosition position)
		{
			return chunks[PosToIndex(position)].chunk;
		}

		public ChunkMesh GetMesh(ChunkPosition position)
		{
			return chunks[PosToIndex(position)].mesh;
		}

		public ChunkMesh GetMesh(int x, int y, int z)
		{
			return chunks[PosToIndex(new ChunkPosition(x, y, z))].mesh;
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

		public ushort GetRaw(CubePosition position)
		{
			if (position.Coord == CubePosition.CoordinateSpace.ChunkSpace)
			{
				Console.WriteLine("Warning: cannot use World.GetRaw with Chunk Space CubePosition.");
				return 0;
			}

			if (IsInWorldBounds(position))
				return GetChunk(position).GetData().GetRaw(position);
			else return 0;
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
			return chunks[PosToIndex(position)].transform;
		}
#endregion

		public void UnloadMesh(ChunkPosition position)
		{
			chunks[PosToIndex(position)].mesh = null;
		}

		public void UnloadAllMeshes()
		{
			for (int i = 0; i < sizeInChunks * sizeInChunks * sizeInChunks; i++)
			{
				chunks[i].mesh = null;
			}
		}

		public void Unload(ChunkPosition position)
		{
			chunks[PosToIndex(position)].mesh = null;
			chunks[PosToIndex(position)].chunk.SetData(null);
		}

		public void UnloadAll()
		{
			for (int i = 0; i < sizeInChunks * sizeInChunks * sizeInChunks; i++)
			{
				chunks[i].mesh = null;
				chunks[i].chunk.SetData(null);
			}
		}

		public void MarkGenerateDirty(ChunkPosition position)
		{
			// Already queued, ignore
			/*if (chunks[PosToIndex(position)].genQueued)
				return;

			if (chunks[PosToIndex(position)].chunk != null && chunks[PosToIndex(position)].chunk.GetData().GenStep == ChunkData.GenerationStep.Done)
				throw new Exception("Tried to mark a chunk to generate after it has already been generated.");

			// supports both generation step broad and detail, but not done.
			if (dataBus.AddChunkToGenerate(position, generator, ChunkDatas))// generator.MakeChunk(ChunkDatas, position));
				chunks[PosToIndex(position)].genQueued = true;*/
		}

		// Marks a chunk as dirty, meaning it needs to be remeshed.
		public void MarkDirty(ChunkPosition position, bool markModified)
		{
			chunks[PosToIndex(position)].meshDirty = true;
			if (!chunksToMeshAlreadyAdded.Contains(position))
			{
				chunksToMeshQueue.EnqueueWithoutSorting(position);
				chunksToMeshAlreadyAdded.Add(position);
			}

			if (!modifiedChunks.Contains(position))
				modifiedChunks.Add(position);
		}

		public void MarkDirty(int x, int y, int z, bool markModified)
		{
			MarkDirty(new ChunkPosition(x, y, z), markModified);
		}
	}
}
