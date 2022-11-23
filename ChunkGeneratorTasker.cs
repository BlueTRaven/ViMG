using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

			generator.Initialize(manager.SizeInCubes, manager.SizeInChunksXZ);

			ProfilingHelper.Start("Beginning broad phase generation...");

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
			broadPhaseTasks.ForEach(x => {
				x.Wait();
			});

			broadPhaseTasks = null;

			ProfilingHelper.End("Broad phase generation done.");
			/*Console.WriteLine("Finished Broad Phase. Generated {0} total chunks in {1} seconds. ({2} seconds elapsed since start.)",
				total, broadWatch.Elapsed.Seconds, totalWatch.Elapsed.TotalSeconds);*/

			ProfilingHelper.Start("Beginning detail phase generation...");
			if (Main.DO_DETAIL)
			{
				num = 0;
				for (int i = 0; i < total; i++)
				{
					Util.OneDToThreeD(i, new ValuePoint3D(manager.SizeInChunksXZ), out ValuePoint3D point);

					generator.GenerateChunkDetail(manager, new ChunkPosition(point.x, point.y, point.z));

					num++;
				}

				generator.PostGenerateDetail(world, manager);
				//GenerateHeightmap();
			}

			ProfilingHelper.Start("Beginning post-detail generation...");
			num = 0;
			for (int i = 0; i < total; i++)
			{
				Util.OneDToThreeD(i, new ValuePoint3D(manager.SizeInChunksXZ), out ValuePoint3D point);

				PostChunkGen(manager, new ChunkPosition(point.x, point.y, point.z));
				num++;
			}
			ProfilingHelper.End("Post-detail generation done.");

			ProfilingHelper.End("Detail phase generation done.");

			/*Console.WriteLine("Finished Detail Phase. Generated {0} total chunks in {1} seconds. ({2} seconds elapsed since start.)",
				total, detailWatch.Elapsed.Seconds, totalWatch.Elapsed.TotalSeconds);*/

			//chunksToMeshQueue.Sort();

			ProfilingHelper.End("World generation done.");
		}

		private static void PostChunkGen(ChunkManager2 manager, ChunkPosition position)
		{
			for (int x = 0; x < Chunk.CHUNK_SIZE; x++)
            {
				for (int y = 0; y < Chunk.CHUNK_SIZE; y++)
                {
					for (int z = 0; z < Chunk.CHUNK_SIZE; z++)
                    {
						CubePosition cubePosition = new CubePosition(x, y, z, CubePosition.CoordinateSpace.ChunkSpace).InChunkSpace(position);

						Cube cube = manager.GetCube(cubePosition).GetOrDefault(Main.Registry.CubeRegistry.Air);
						cube.PostChunkGen(null, manager, cubePosition);
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
