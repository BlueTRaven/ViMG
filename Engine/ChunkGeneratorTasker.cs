using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ViMG.Cubes;
using ViMG.Entities;
using ViMG.GameStates;
using ViMG.Generation;

namespace ViMG
{
    public static class ChunkGeneratorTasker
    {
		private readonly struct BroadChunkTaskState
		{
			public readonly WorldPrototype world;
			public readonly int chunkStart;
			public readonly int chunkEnd;
			public readonly int totalChunks;
			public readonly ChunkPosition[] chunks;
			public readonly ChunkGenerator generator;

			public BroadChunkTaskState(WorldPrototype world, int chunkStart, int chunkEnd, int totalChunks, ChunkPosition[] chunks, ChunkGenerator generator)
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
			public readonly WorldPrototype world;
			public readonly ChunkGenerator generator;
			public readonly Random random;

			public BroadGenerationState(ChunkPosition position, WorldPrototype world, ChunkGenerator generator)
			{
				this.position = position;
				this.world = world;
                this.generator = generator;

				random = new Random(generator.Seed);
			}
		}

		public static void GenerateWorld(WorldPrototype world, ChunkGenerator generator)
		{
			int num = 0;
			int total = world.ChunkManager.SizeInChunksXZ * world.ChunkManager.SizeInChunksXZ * world.ChunkManager.SizeInChunksXZ;
			int offset = 0;

			ProfilingHelper.Start("Beginning world generation...");
            GameStateTheIsland.LoadMessage = "Beginning world generation...";

			generator.Initialize(world.ChunkManager.SizeInCubes, world.ChunkManager.SizeInChunksXZ);

			ProfilingHelper.Start("Broad phase generation...");
            GameStateTheIsland.LoadMessage = "Beginning broad phase generation...";

			ChunkPosition[] positions = new ChunkPosition[total];
			for (int i = 0; i < total; i++)
			{
				Util.OneDToThreeD(i, new ValuePoint3D(world.ChunkManager.SizeInChunksXZ), out ValuePoint3D point);
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

				if (Main.MULTITHREAD_BROAD_PHASE)
					task.Start();
				else task.RunSynchronously();

				broadPhaseTasks.Add(task);
			}

			//Can't really begin detail phase until broad phase is finished (for now)
			//So just wait for it all to finish.
			for (int i = 0; i < broadPhaseTasks.Count; i++)
            {
				Task task = broadPhaseTasks[i];

				while (!task.IsCompleted)
                {
                    GameStateTheIsland.LoadMessage = "Broad phase generation...\n" +
						i + "/" + broadPhaseTasks.Count;
					Thread.Sleep(10);
                }
            }

			broadPhaseTasks = null;

			ProfilingHelper.End("Broad phase generation done.");

			ProfilingHelper.Start("Beginning detail phase generation...");
			if (Main.DO_DETAIL)
			{
                GameStateTheIsland.LoadMessage = "Detail phase generation...";
				
				num = 0;
				for (int i = 0; i < total; i++)
				{
					Util.OneDToThreeD(i, new ValuePoint3D(world.ChunkManager.SizeInChunksXZ), out ValuePoint3D point);

					generator.GenerateChunkDetail(world, new ChunkPosition(point.x, point.y, point.z));

					num++;

					if (i % 8 == 0)
                        GameStateTheIsland.LoadMessage = "Detail phase generation...\n" +
							i + "/" + total;
				}

                GameStateTheIsland.LoadMessage = "Post detail phase generation...\n" +
					"(This may take a while)";
				generator.PostGenerateDetail(world);
				//GenerateHeightmap();
			}

            GameStateTheIsland.LoadMessage = "Post generation...";
			ProfilingHelper.Start("Beginning post generation...");
			num = 0;
			for (int i = 0; i < total; i++)
			{
				Util.OneDToThreeD(i, new ValuePoint3D(world.ChunkManager.SizeInChunksXZ), out ValuePoint3D point);

				PostChunkGen(world, new ChunkPosition(point.x, point.y, point.z));
				num++;
			}
			ProfilingHelper.End("Post generation done.");

			ProfilingHelper.End("Detail phase generation done.");

			/*Console.WriteLine("Finished Detail Phase. Generated {0} total chunks in {1} seconds. ({2} seconds elapsed since start.)",
				total, detailWatch.Elapsed.Seconds, totalWatch.Elapsed.TotalSeconds);*/

			//chunksToMeshQueue.Sort();

			ProfilingHelper.End("World generation done.");
		}

		private static void PostChunkGen(WorldPrototype world, ChunkPosition position)
		{
			for (int z = 0; z < Chunk.CHUNK_SIZE; z++)
            {
				for (int y = 0; y < Chunk.CHUNK_SIZE; y++)
                {
					for (int x = 0; x < Chunk.CHUNK_SIZE; x++)
                    {
						CubePosition cubePosition = new CubePosition(x, y, z, CubePosition.CoordinateSpace.ChunkSpace).InCubeSpace(position);

						Cube cube = world.ChunkManager.CubeView.GetCube(cubePosition).GetOrDefault(Main.Registry.CubeRegistry.Air);
						cube.PostChunkGen(world, cubePosition);
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
				state.generator.GenerateChunkBroad(new BroadGenerationState(state.chunks[j], state.world, state.generator));
			}
		}
	}
}
