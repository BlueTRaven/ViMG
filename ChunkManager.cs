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
		public const int NUM_CHUNK_MESH_PASSES = 5;

		private readonly struct Layer
        {
			public readonly int index;
			public readonly ChunkGenerator generator;
			public readonly ManagedChunk[] chunks;
			//public readonly LayerStateManager layerStateManager;	//Do we need some method of performing layer-specific logic?

			public Layer(int index, ChunkGenerator generator, int totalChunks)
            {
                this.index = index;
                this.generator = generator;

				this.chunks = new ManagedChunk[totalChunks];
            }
        }

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

		public readonly struct BroadGenerationState
        {
            public readonly Chunk chunk;
            public readonly ChunkGenerator generator;
			public readonly Random random;

            public BroadGenerationState(Chunk chunk, ChunkGenerator generator)
            {
                this.chunk = chunk;
                this.generator = generator;

				random = new Random(generator.Seed);
			}
        }

		private struct ManagedChunk
		{
			public Chunk chunk;
			//public ChunkMesh mesh;
			public ChunkMesh[] meshes;
			//0: general geometry pass
			//1: transparents pass
			//2: fluid pass?
			public Matrix transform;

			//public GenerationStep genStep;
			// If true, the mesh is dirty and must be regenerated (or generated.)
			public bool meshDirty;
			public int meshVersion;

			// In the queue to be generated
			public bool genQueued;
			public bool meshQueued;

			public bool valid;

			public ManagedChunk(Chunk defaultChunk, int x, int y, int z)
			{
				chunk = defaultChunk;
				//chunk = new Chunk(chunkDatas, x, y, z);
				//mesh = null;
				meshes = new ChunkMesh[NUM_CHUNK_MESH_PASSES];

				transform = Matrix.Identity;

				meshDirty = true;
				meshVersion = 0;
				//genStep = GenerationStep.Broad;
				genQueued = false;
				meshQueued = false;

				valid = true;
			}

			public int GetMeshVersionCode()
            {
				return chunk.GetHashCode() + meshVersion;
			}
		}

		private Layer[] layerLookupTable;

		private int discoveredLayers;
		public int DiscoveredLayers => discoveredLayers + 1;

		public readonly int sizeInChunksXZ;
		public readonly int layerSizeInChunksY;
		public readonly int sizeInCubes;
		private readonly int totalNumChunks;
		private readonly int layerNumChunks;

        public readonly World world;
		private ChunkMesher mesher;

		//private ManagedChunk[] chunks;

		public GenericPool<ChunkData> ChunkDatas = new GenericPool<ChunkData>(() => new ChunkData());

		private PriorityQueue<ChunkPosition> chunksToMeshQueue = new PriorityQueue<ChunkPosition>(true, (x) => 
		{
			return (int)(Main.camera.Position - x.InWorldSpace()).Length(); 
		});
		private HashSet<ChunkPosition> chunksToMeshAlreadyAdded = new HashSet<ChunkPosition>();

		public float[] HeightmapRaw;
		public Texture2D Heightmap;
		public Texture2D HeightmapStrength;

		public static int QueueMesh = 0;
		public static int TotalQueueMesh = 0;

		public ChunkManager(GraphicsDevice device, int sizeInChunks, int sizeInCubes, World world)
		{
			this.sizeInChunksXZ = sizeInChunks;
			this.layerSizeInChunksY = sizeInChunks;
			this.sizeInCubes = sizeInCubes;
			layerNumChunks = sizeInChunksXZ * layerSizeInChunksY * sizeInChunksXZ;
			
			this.world = world;

			layerLookupTable = new Layer[2]
			{
				new Layer(0, new ChunkGeneratorIsland(0), sizeInChunksXZ * layerSizeInChunksY * sizeInChunksXZ),
				new Layer(1, new ChunkGeneratorFlat(1), sizeInChunksXZ * layerSizeInChunksY * sizeInChunksXZ)  //TODO
			};

			totalNumChunks = layerNumChunks * layerLookupTable.Length;

			Heightmap = new Texture2D(device, sizeInCubes, sizeInCubes, false, SurfaceFormat.Single);

			mesher = new ChunkMesher(device);
            //chunks = new ManagedChunk[sizeInChunksXZ * (layerSizeInChunksY * layerLookupTable.Length) * sizeInChunksXZ];
		}

		public void InitLayer(int layer)
		{
			int total = sizeInChunksXZ * sizeInChunksXZ * sizeInChunksXZ;
			int offset = total * layer;

			if (layer > discoveredLayers)
				discoveredLayers = layer;

			for (int i = 0; i < total; i++)
			{
				int x = i % sizeInChunksXZ;
				int y = (i / sizeInChunksXZ) % sizeInChunksXZ;
				int z = i / (sizeInChunksXZ * sizeInChunksXZ);

				if (layer > 0)
					y = layerSizeInChunksY - (layerSizeInChunksY * layer) - 1;

				layerLookupTable[layer].chunks[i] = new ManagedChunk(layerLookupTable[layer].generator.MakeChunk(this, new ChunkPosition(x, y, z)), 0, 0, 0);
			}
		}

		public void GenerateWorld(World world, int layer)
		{
			int num = 0;
			int total = sizeInChunksXZ * sizeInChunksXZ * sizeInChunksXZ;
			int offset = total * layer;

			Stopwatch totalWatch = Stopwatch.StartNew();

			layerLookupTable[layer].generator.Initialize(world);

			Stopwatch broadWatch = Stopwatch.StartNew();

			List<Task> broadPhaseTasks = new List<Task>();

			const int split = 8;

			for (int i = 0; i < total; i += split) 
			{
				int chunkStart = i;
				int chunkEnd = i + split;

				BroadChunkTaskState state = new BroadChunkTaskState(chunkStart, chunkEnd, total, layerLookupTable[layer].chunks, layerLookupTable[layer].generator);

				for (int j = chunkStart; j < chunkEnd; j++)
                {
					layerLookupTable[layer].chunks[j].chunk.SetData(ChunkDatas.Get());
					layerLookupTable[layer].chunks[j].chunk.Initialize(world);
                }

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

			Stopwatch detailWatch = Stopwatch.StartNew();

			if (Main.DO_DETAIL)
			{
				num = 0;
				for (int i = 0; i < total; i++)
				{
					int x = i % sizeInChunksXZ;
					int y = (i / sizeInChunksXZ) % sizeInChunksXZ;
					int z = i / (sizeInChunksXZ * sizeInChunksXZ);

					layerLookupTable[layer].generator.GenerateChunkDetail(this, layerLookupTable[layer].chunks[i].chunk, new ChunkPosition(x, y, z));

					num++;

					if (num % sizeInChunksXZ * sizeInChunksXZ == 0)
						Console.WriteLine("Detail: " + num + " / " + total);
				}

				layerLookupTable[layer].generator.PostGenerateDetail(this);
				//GenerateHeightmap();
			}

			num = 0;
			for (int i = 0; i < total; i++)
			{
				int x = i % sizeInChunksXZ;
				int y = (i / sizeInChunksXZ) % sizeInChunksXZ;
				int z = i / (sizeInChunksXZ * sizeInChunksXZ);

				layerLookupTable[layer].chunks[i].chunk.Initialize(world);
				layerLookupTable[layer].chunks[i].chunk.PostChunkGen(world);

				//MarkDirty(new ChunkPosition(x, y, z), false);
				num++;

				if (num % sizeInChunksXZ * sizeInChunksXZ == 0)
					Console.WriteLine("Init: " + num + " / " + total);
			}

			detailWatch.Stop();

			Console.WriteLine("Finished Detail Phase. Generated {0} total chunks in {1} seconds. ({2} seconds elapsed since start.)", 
				total, detailWatch.Elapsed.Seconds, totalWatch.Elapsed.TotalSeconds);

			chunksToMeshQueue.Sort();

			totalWatch.Stop();

			Console.WriteLine("Finished. Generated {0} total chunks in {1} seconds.", total, totalWatch.Elapsed.TotalSeconds);
		}

		private static Point[] sampleOffsets = new Point[]
		{
			new Point(-1, -1),
			new Point(-1, 0),
			new Point(-1, 1),
			new Point(0, -1),
			//new Point(0, 0),
			new Point(1, -1),
			new Point(1, 0),
			new Point(1, 1),
		};

		public void GenerateHeightmap()
        {
			HeightmapRaw = new float[sizeInCubes * sizeInCubes];

			for (int x = 0; x < sizeInCubes; x++)
			{
				for (int z = 0; z < sizeInCubes; z++)
				{
					int firstY = 0;
					for (int y = sizeInCubes; y >= 0; y--)
					{
						Cube cube = GetCube(x, y, z).GetOrDefault(Main.Registry.CubeRegistry.Air);
						if (cube.Transparency == Cube.TransparencyValue.Opaque)
						{
							firstY = y;
							break;
						}
					}

					int i = z * sizeInCubes + x;

					HeightmapRaw[i] = (float)firstY / (float)sizeInCubes;
				}
			}

			Heightmap.SetData(HeightmapRaw);
		}

		private static void GenerateChunkDetailTaskFn(object obj)
		{
			BroadChunkTaskState state = (BroadChunkTaskState)obj;
			int split = state.chunkEnd - state.chunkStart;

			for (int j = state.chunkStart; j < state.chunkEnd; j++)
			{
				state.generator.GenerateChunkBroad(new BroadGenerationState(state.chunks[j].chunk, state.generator));

				if (!Main.DO_DETAIL)
					state.chunks[j].chunk.GetData().GenStep = ChunkData.GenerationStep.Done;
				else state.chunks[j].chunk.GetData().GenStep = ChunkData.GenerationStep.Detail;
			}

			Console.WriteLine("Broad task {0}/{1} finished.", state.chunkStart / split, state.totalChunks / split);
		}

		public Vector3 GetPlayerSpawnPos(World world)
        {
			return layerLookupTable[0].generator.GetPlayerPosition(world, this);
        }

		public void ProcessChunkQueue(World world, int forceMode)
		{
			chunksToMeshQueue.Sort();

			//ProcessPriorityMeshChunks(world);

			ProcessChunkQueueSync(world, 1, 4);
		}

		private ChunkPosition LayerRelativePosition(ChunkPosition chunkPosition)
        {
			int y = chunkPosition.Y;

			if (chunkPosition.Y < 0)
				y = EngineMathHelper.Mod(chunkPosition.Y, layerSizeInChunksY);

			return new ChunkPosition(chunkPosition.X, y, chunkPosition.Z);
        }

		private CubePosition LayerRelativePosition(CubePosition cubePosition)
        {
			int y = cubePosition.Y;

			if (cubePosition.Y < 0)
				y = EngineMathHelper.Mod(cubePosition.Y, sizeInCubes) + 1;
			return new CubePosition(cubePosition.X, y, cubePosition.Z);
        }

		private int LayerFromPos(ChunkPosition position)
        {
			int y = position.Y + 1;
			//We subtract layer size from this because the range would otherwise be 512, 0, -512, etc. which would evaluate to 1, 0, -1 (which is index 1).
			return Math.Abs((y - layerSizeInChunksY) / layerSizeInChunksY);
        }

		private int LayerFromPos(CubePosition position)
		{
			int y = position.Y + 1;

			return Math.Abs((y - sizeInCubes) / sizeInCubes);
		}

		private int IndexFromPos(ChunkPosition position)
		{
			return position.X + sizeInChunksXZ * (position.Y + sizeInChunksXZ * position.Z);
		}

		private Chunk[] cs = new Chunk[6];

		// Synchronously processess chunks in the queue.
		public void ProcessChunkQueueSync(World world, int maxGen = -1, int maxMesh = -1)
		{
			QueueMesh = chunksToMeshQueue.Count;

			if (chunksToMeshQueue.Count > 0)
			{
				// we only manually process the meshing chunks if we don't have any to generate.
				//Queue<ChunkPosition> queue = new Queue<ChunkPosition>(chunksToMesh.Distinct());
				int num = 0;

				while (chunksToMeshQueue.Count > 0 && (maxMesh == -1 || num < maxMesh))
				{
					var pos = chunksToMeshQueue.Dequeue();

					var c = layerLookupTable[LayerFromPos(pos)].chunks[IndexFromPos(pos)];

					if (!c.meshDirty || !c.chunk.Initialized)
					{
						//Already meshed.
						//Alternatively, the chunk may have been unloaded.
						//Remove from list.
						if (chunksToMeshAlreadyAdded.Contains(pos))
							chunksToMeshAlreadyAdded.Remove(pos);

						continue;
					}

					if (pos.X - 1 >= 0)
					{
						ChunkPosition offsetPos = new ChunkPosition(pos.X - 1, pos.Y, pos.Z); 
						Chunk adjacent = layerLookupTable[LayerFromPos(offsetPos)].chunks[IndexFromPos(offsetPos)].chunk;
						if (adjacent != null && !adjacent.Initialized)
						{
							chunksToMeshQueue.EnqueueWithoutSorting(pos);
							num++;
							continue;
						}

						cs[0] = adjacent;
					}

					if (pos.Y - 1 >= -layerSizeInChunksY * (layerLookupTable.Length - 1))
					{
						ChunkPosition offsetPos = new ChunkPosition(pos.X, pos.Y - 1, pos.Z);
						Chunk adjacent = layerLookupTable[LayerFromPos(offsetPos)].chunks[IndexFromPos(LayerRelativePosition(offsetPos))].chunk;
						if (adjacent != null && !adjacent.Initialized)
						{
							chunksToMeshQueue.EnqueueWithoutSorting(pos);
							num++;
							continue;
						}

						cs[1] = adjacent;
					}

					if (pos.Z - 1 >= 0)
					{
						ChunkPosition offsetPos = new ChunkPosition(pos.X, pos.Y, pos.Z - 1);
						Chunk adjacent = layerLookupTable[LayerFromPos(offsetPos)].chunks[IndexFromPos(LayerRelativePosition(offsetPos))].chunk;

						if (adjacent != null && !adjacent.Initialized)
						{
							chunksToMeshQueue.EnqueueWithoutSorting(pos);
							num++;
							continue;
						}

						cs[2] = adjacent;
					}

					if (pos.X + 1 < sizeInChunksXZ)
					{
						ChunkPosition offsetPos = new ChunkPosition(pos.X + 1, pos.Y, pos.Z);
						Chunk adjacent = layerLookupTable[LayerFromPos(offsetPos)].chunks[IndexFromPos(LayerRelativePosition(offsetPos))].chunk;
					
						if (adjacent != null && !adjacent.Initialized)
						{
							chunksToMeshQueue.EnqueueWithoutSorting(pos);
							num++;
							continue;
						}

						cs[3] = adjacent;
					}

					if (pos.Y + 1 < sizeInChunksXZ)
					{
						ChunkPosition offsetPos = new ChunkPosition(pos.X, pos.Y + 1, pos.Z);
						Chunk adjacent = layerLookupTable[LayerFromPos(offsetPos)].chunks[IndexFromPos(LayerRelativePosition(offsetPos))].chunk;
						
						if (adjacent != null && !adjacent.Initialized)
						{
							chunksToMeshQueue.EnqueueWithoutSorting(pos);
							num++;
							continue;
						}

						cs[4] = adjacent;
					}

					if (pos.Z + 1 < sizeInChunksXZ)
					{
						ChunkPosition offsetPos = new ChunkPosition(pos.X, pos.Y, pos.Z + 1);
						Chunk adjacent = layerLookupTable[LayerFromPos(offsetPos)].chunks[IndexFromPos(LayerRelativePosition(offsetPos))].chunk;
					
						if (adjacent != null && !adjacent.Initialized)
						{
							chunksToMeshQueue.EnqueueWithoutSorting(pos);
							num++;
							continue;
						}

						cs[5] = adjacent;
					}

					MeshChunk(world, pos);
					chunksToMeshAlreadyAdded.Remove(pos);

					num++;
				}
			}
		}

		private Chunk[] allChunks;
		public Chunk[] GetChunks()
		{
			if (allChunks == null)
				allChunks = new Chunk[totalNumChunks];

			//Rebuild this every time as chunks may have changed.
			for (int i = 0; i < totalNumChunks; i++)
			{
				int layer = i / layerNumChunks;
				int index = i % layerNumChunks;
				allChunks[i] = layerLookupTable[layer].chunks[index].chunk;
			}

			return allChunks;
		}

		private void MeshChunk(World world, ChunkPosition pos)
		{
			Stopwatch watch = Stopwatch.StartNew();

			ref ManagedChunk mc = ref layerLookupTable[LayerFromPos(pos)].chunks[IndexFromPos(LayerRelativePosition(pos))];

			//First one must have forceUpdate = true,
			//but all subsequent mesh generations should be false.
			mc.meshes[(int)Cube.RenderPass.Opaque] = mesher.GenerateChunk(mc.chunk, world, Cube.RenderPass.Opaque, true);
			mc.meshes[(int)Cube.RenderPass.Transparent] = mesher.GenerateChunk(mc.chunk, world, Cube.RenderPass.Transparent, false);
			mc.meshes[(int)Cube.RenderPass.DepthOnly] = mesher.GenerateChunk(mc.chunk, world, Cube.RenderPass.DepthOnly, false);
			mc.meshes[(int)Cube.RenderPass.Fluid] = null;	//TODO fluids?
			mc.meshes[(int)Cube.RenderPass.Air] = mesher.GenerateChunk(mc.chunk, world, Cube.RenderPass.Air, false);
			mc.meshDirty = false;
			mc.meshQueued = false;

			mc.meshVersion++;

			watch.Stop();

			Console.WriteLine("Meshed chunk at " + pos.X + ", " + pos.Y + ", " + pos.Z + " with " + ChunkData.ChunkUpdate + " updates - took " + watch.Elapsed.TotalSeconds);
		}

		#region Get Things
		public void SetChunk(Chunk chunk)
		{
			GetManagedChunk(chunk.Position).chunk = chunk;
			//int layer = LayerFromPos(chunk.Position);
			//int index = IndexFromPos(chunk.Position);

			//layerLookupTable[layer].chunks[index].chunk = chunk;
		}

		public Chunk GetChunk(CubePosition position)
		{
			ChunkPosition chunkPos = ChunkPosition.CubeChunk(position);

			int layer = LayerFromPos(chunkPos);
			int index = IndexFromPos(LayerRelativePosition(chunkPos));

			Chunk c = layerLookupTable[layer].chunks[index].chunk;
			if (index < 0 || index >= layerNumChunks)
				return null;
			else return c;
		}

		public Chunk GetChunk(ChunkPosition position)
		{
			int layer = LayerFromPos(position);
			int index = IndexFromPos(LayerRelativePosition(position));

			return layerLookupTable[layer].chunks[index].chunk;
		}

		public ChunkMesh GetMesh(ChunkPosition position, Cube.RenderPass pass)
		{
			int layer = LayerFromPos(position);
			int index = IndexFromPos(LayerRelativePosition(position));

			return layerLookupTable[layer].chunks[index].meshes[(int)pass];
		}

		public int GetMeshVersionCode(ChunkPosition position)
        {
			return GetManagedChunk(position).GetMeshVersionCode();
        }

		public bool IsInWorldBounds(Vector3 position)
		{
			return IsInWorldBounds(CubePosition.FromWorldSpace(position));
		}

		public bool IsInWorldBounds(CubePosition position)
		{
			int sign = MathF.Sign(position.Y);

			int layer = LayerFromPos(position);

			if (layer > discoveredLayers)
				return false;

			position = LayerRelativePosition(position);

			if (position.Coord == CubePosition.CoordinateSpace.ChunkSpace)
				return false;
			else
			{
				//positive sign/0 (Sign(0) == 0)
				if (sign >= 0)
				{
					return position.X >= 0 && position.X < sizeInCubes &&
						position.Y >= 0 && position.Y < sizeInCubes &&
						position.Z >= 0 && position.Z < sizeInCubes;
				}
				else if (sign == -1)
				{
					return position.X > 0 && position.X <= sizeInCubes &&
						position.Y > 0 && position.Y <= sizeInCubes &&
						position.Z > 0 && position.Z <= sizeInCubes;
				}
				else return false;
			}
		}

		public bool IsInWorldBounds(ChunkPosition position)
		{
			return position.X >= 0 && position.X < sizeInChunksXZ &&
					LayerFromPos(position) <= layerLookupTable.Length &&
					position.Z >= 0 && position.Z < sizeInChunksXZ;
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
			{
                /*if (position.Y < 0)
                    Console.WriteLine("Aaa");*/
                Chunk c = GetChunk(position);
				if (c == null || !c.Initialized)
					return 0;
				else return c.GetData().GetRaw(position);
			}
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

				if (chunk == null || !chunk.Initialized)
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

				if (chunk == null || !chunk.Initialized)
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
			return GetManagedChunk(position).transform;
		}

		private ref ManagedChunk GetManagedChunk(ChunkPosition position)
        {
			return ref layerLookupTable[LayerFromPos(position)].chunks[IndexFromPos(position)];
        }

		public OptionalValue<CubePosition> GetFirstSolidDown(Vector3 start)
		{
			CubePosition startPos = CubePosition.FromWorldSpace(start);

			for (int y = 0; y < sizeInCubes; y++)
			{
				CubePosition pos = new CubePosition(startPos.X, startPos.Y - y, startPos.Z);
				if (IsInWorldBounds(pos) && GetRaw(pos) != 0)
					return new OptionalValue<CubePosition>(pos);
			}

			return new OptionalValue<CubePosition>();
		}

		public OptionalValue<CubePosition> GetFirstSolidDown(CubePosition start)
		{
			for (int y = 0; y < sizeInCubes; y++)
			{
				CubePosition pos = new CubePosition(start.X, start.Y - y, start.Z);

				/*if (pos.Y < 0)
					Console.WriteLine("aaa");*/
				//Null check here is the same as doing out of bounds check.
				Cube cubeAtPos = GetCube(pos).Get();
				if (cubeAtPos != null && (cubeAtPos.Touchable && cubeAtPos.Collision == Cube.CollisionValue.Collidable))
					return new OptionalValue<CubePosition>(pos);
			}

			return new OptionalValue<CubePosition>();
		}
		#endregion

		public void UnloadMesh(ChunkPosition position)
		{
			ref ManagedChunk c = ref GetManagedChunk(position);

			for (int i = 0; i < NUM_CHUNK_MESH_PASSES; i++)
            {
				if (c.meshes[i] != null && c.meshes[i] != ChunkMesh.Empty)
				{
					c.meshes[i].VBO.Dispose();
					c.meshes[i].IBO.Dispose();

					c.meshes[i] = null;
				}
			}
		}

		public void UnloadAllMeshes()
		{
			for (int i = 0; i < layerLookupTable.Length; i++)
			{
				for (int j = 0; j < layerNumChunks; j++)
				{
					for (int k = 0; k < NUM_CHUNK_MESH_PASSES; k++)
					{
						ChunkMesh mesh = layerLookupTable[i].chunks[j].meshes[k];
						if (mesh != null && mesh != ChunkMesh.Empty)
						{
							mesh.VBO.Dispose();
							mesh.IBO.Dispose();

							layerLookupTable[i].chunks[j].meshes[k] = null;
						}
					}
				}
			}
		}

		public void Unload(ChunkPosition position)
		{
			ref ManagedChunk c = ref GetManagedChunk(position);

			for (int i = 0; i < NUM_CHUNK_MESH_PASSES; i++)
			{
				if (c.meshes[i] != null && !c.meshes[i].IsEmpty)
				{
					c.meshes[i].VBO.Dispose();
					c.meshes[i].IBO.Dispose();

					c.meshes[i] = null;
				}
			}
			
			ChunkDatas.Return(c.chunk.GetData());
			c.chunk.SetData(null);
		}

		public void UnloadAll()
		{
			for (int i = 0; i < layerLookupTable.Length; i++)
			{
				for (int j = 0; j < layerNumChunks; j++)
				{
					ref ManagedChunk c = ref layerLookupTable[i].chunks[j];

					if (c.valid)
					{
						for (int k = 0; k < NUM_CHUNK_MESH_PASSES; k++)
						{
							if (c.meshes[k] != null && !c.meshes[k].IsEmpty)
							{
								c.meshes[k].VBO.Dispose();
								c.meshes[k].IBO.Dispose();

								c.meshes[k] = null;
							}
						}

						if (c.chunk.Initialized)
						{
							ChunkDatas.Return(c.chunk.GetData());
							c.chunk.SetData(null);
						}
					}
				}
			}
		}

		// Marks a chunk as dirty, meaning it needs to be remeshed.
		public void MarkDirty(ChunkPosition position, bool markModified)
		{
			GetManagedChunk(position).meshDirty = true;
			if (!chunksToMeshAlreadyAdded.Contains(position))
			{
				chunksToMeshQueue.EnqueueWithoutSorting(position);
				chunksToMeshAlreadyAdded.Add(position);
			}
		}

		public void MarkDirty(int x, int y, int z, bool markModified)
		{
			MarkDirty(new ChunkPosition(x, y, z), markModified);
		}
	}
}
