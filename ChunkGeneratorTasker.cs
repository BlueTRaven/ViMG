using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ViMG.Cubes;
using ViMG.Generation;

namespace ViMG
{
    public static class ChunkGeneratorTasker
    {
		private readonly struct BroadChunkTaskState
		{
			public readonly World world;
			public readonly int chunkStart;
			public readonly int chunkEnd;
			public readonly int totalChunks;
			public readonly ChunkPosition[] chunks;
			public readonly ChunkGenerator generator;

			public BroadChunkTaskState(World world, int chunkStart, int chunkEnd, int totalChunks, ChunkPosition[] chunks, ChunkGenerator generator)
			{
				this.world = world;
				this.chunkStart = chunkStart;
				this.chunkEnd = chunkEnd;
				this.totalChunks = totalChunks;
				this.chunks = chunks;
				this.generator = generator;
			}
		};

		public readonly struct BroadGenerationState
		{
			public readonly ChunkPosition position;
			public readonly ChunkManager2 manager;
			public readonly ChunkGenerator generator;
			public readonly Random random;

			public BroadGenerationState(ChunkPosition position, ChunkManager2 manager, ChunkGenerator generator)
			{
				this.position = position;
                this.manager = manager;
                this.generator = generator;

				random = new Random(generator.Seed);
			}
		}

		public static void GenerateWorld(World world, ChunkManager2 manager, ChunkGenerator generator)
		{
			int num = 0;
			int total = world.sizeInChunks * world.sizeInChunks * world.sizeInChunks;
			int offset = 0;

			ProfilingHelper.Start("Beginning world generation...");
			world.GameStateManager.TheIsland.LoadMessage = "Beginning world generation...";

			generator.Initialize(manager.SizeInCubes, manager.SizeInChunksXZ);

			ProfilingHelper.Start("Broad phase generation...");
			world.GameStateManager.TheIsland.LoadMessage = "Beginning broad phase generation...";

			ChunkPosition[] positions = new ChunkPosition[total];
			for (int i = 0; i < total; i++)
			{
				Util.OneDToThreeD(i, new ValuePoint3D(manager.SizeInChunksXZ), out ValuePoint3D point);
				positions[i] = new ChunkPosition(point.x, point.y, point.z);
			}

			List<Task> broadPhaseTasks = new List<Task>();

			//generate total / split tasks for broad phase generation.
			const int split = 8;
			for (int i = 0; i < total; i += split)
			{
				int chunkStart = i;
				int chunkEnd = i + split;

				BroadChunkTaskState state = new BroadChunkTaskState(world, chunkStart, chunkEnd, total, positions, generator);
				Task task = new Task(GenerateChunkDetailTaskFn, state);

				task.Start();
				broadPhaseTasks.Add(task);
			}

			//Can't really begin detail phase until broad phase is finished (for now)
			//So just wait for it all to finish.
			for (int i = 0; i < broadPhaseTasks.Count; i++)
            {
				Task task = broadPhaseTasks[i];

				while (!task.IsCompleted)
                {
					world.GameStateManager.TheIsland.LoadMessage = "Broad phase generation...\n" +
						i + "/" + broadPhaseTasks.Count;
					Thread.Sleep(100);
                }
            }

			broadPhaseTasks = null;

			ProfilingHelper.End("Broad phase generation done.");

			ProfilingHelper.Start("Beginning detail phase generation...");
			if (Main.DO_DETAIL)
			{
				world.GameStateManager.TheIsland.LoadMessage = "Detail phase generation...";
				
				num = 0;
				for (int i = 0; i < total; i++)
				{
					Util.OneDToThreeD(i, new ValuePoint3D(manager.SizeInChunksXZ), out ValuePoint3D point);

					generator.GenerateChunkDetail(manager, new ChunkPosition(point.x, point.y, point.z));

					num++;

					if (i % 8 == 0)
						world.GameStateManager.TheIsland.LoadMessage = "Detail phase generation...\n" +
							i + "/" + total;
				}

				world.GameStateManager.TheIsland.LoadMessage = "Post detail phase generation...\n" +
					"(This may take a while)";
				generator.PostGenerateDetail(world, manager);
				//GenerateHeightmap();
			}

			world.GameStateManager.TheIsland.LoadMessage = "Post generation...";
			ProfilingHelper.Start("Beginning post generation...");
			num = 0;
			for (int i = 0; i < total; i++)
			{
				Util.OneDToThreeD(i, new ValuePoint3D(manager.SizeInChunksXZ), out ValuePoint3D point);

				PostChunkGen(world, manager, new ChunkPosition(point.x, point.y, point.z));
				num++;
			}
			ProfilingHelper.End("Post generation done.");

			ProfilingHelper.End("Detail phase generation done.");

			/*Console.WriteLine("Finished Detail Phase. Generated {0} total chunks in {1} seconds. ({2} seconds elapsed since start.)",
				total, detailWatch.Elapsed.Seconds, totalWatch.Elapsed.TotalSeconds);*/

			//chunksToMeshQueue.Sort();

			ProfilingHelper.End("World generation done.");
		}

		private static void PostChunkGen(World world, ChunkManager2 manager, ChunkPosition position)
		{
			for (int x = 0; x < Chunk.CHUNK_SIZE; x++)
            {
				for (int y = 0; y < Chunk.CHUNK_SIZE; y++)
                {
					for (int z = 0; z < Chunk.CHUNK_SIZE; z++)
                    {
						CubePosition cubePosition = new CubePosition(x, y, z, CubePosition.CoordinateSpace.ChunkSpace).InCubeSpace(position);

						Cube cube = manager.InitializerView.GetCube(cubePosition).GetOrDefault(Main.Registry.CubeRegistry.Air);
						cube.PostChunkGen(world, manager, cubePosition);
                    }
				}
			}
		}

		private static void GenerateChunkDetailTaskFn(object obj)
		{
			BroadChunkTaskState state = (BroadChunkTaskState)obj;
			int split = state.chunkEnd - state.chunkStart;

			for (int j = state.chunkStart; j < state.chunkEnd; j++)
			{
				state.generator.GenerateChunkBroad(new BroadGenerationState(state.chunks[j], state.world.ChunkManager2, state.generator));
			}
		}
	}
}
