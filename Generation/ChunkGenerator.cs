using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using ViMG.Cubes;

namespace ViMG.Generation
{
	public abstract class ChunkGenerator
	{
		protected static ushort[] BlacklistAir = new ushort[1] { 0 };
		protected FastNoise noise;
		private Random random;
		private Random threadRandom;

		protected readonly int seed;
		public ChunkGenerator(int seed = 1337)
		{
			this.seed = seed;

			random = new Random(seed);
			threadRandom = new Random(seed - 256);

			noise = new FastNoise(seed);
			noise.SetNoiseType(FastNoise.NoiseType.Simplex);
			noise.SetFrequency(1f / (float)Chunk.CHUNK_SIZE);

		}

		public virtual void Initialize(World world)
		{
			
		}

		protected Random GetRandom()
        {
			if (Thread.CurrentThread == Main.MainThread)
				return random;
			else return threadRandom;
        }

		public Chunk MakeChunk(ChunkManager cm, ChunkPosition position)
		{
			return new Chunk(cm, position);
		}

		public abstract Vector3 GetPlayerPosition(World world, ChunkManager chunks);

		public abstract void GenerateChunkBroad(Chunk chunk);

		private List<Chunk> detailCascadedChunks = new List<Chunk>();

		// Main thread version
		public abstract void GenerateChunkDetail(ChunkManager manager, Chunk chunk, ChunkPosition position);

		public virtual void PostGenerateDetail(ChunkManager manager) { }

		[Obsolete]
		public void GenerateChunkDetail(ChunkGenerationThreadDataBus dataBus, Chunk chunk, ChunkPosition position, HashSet<Chunk> cascadedChunks)
		{
			
		}

		//Creates a random position for a structure in such a way that it will not interfere with other structures.
		protected Vector2 GetRandomPositionForStructure(Vector2 xzSize)
        {
			return Vector2.Zero;
        }

		protected void SetCube(Chunk chunk, CubePosition pos, ushort id)
		{
			chunk.GetData().SetCube(pos, id, false);
		}

		private Chunk cachedWorkingChunk;
		private ushort[] cachedWorkingCubes;

		protected void SetCubeOrAdjacent(ChunkManager manager, Chunk chunk, CubePosition pos, ushort id)
		{
			if (!manager.IsInWorldBounds(pos))
				return;

			// Chunks that have already been fully generated can be marked as dirty
			if (chunk.GetData().IsInChunkBounds(pos))
			{
				if (cachedWorkingChunk != chunk)
				{
					cachedWorkingChunk = chunk;
					cachedWorkingCubes = chunk.GetData().GetAll();
				}
				if (pos.Coord == CubePosition.CoordinateSpace.CubeSpace)
					pos = pos.InChunkSpace(chunk);

				cachedWorkingCubes[pos.X + Chunk.CHUNK_SIZE * (pos.Y + Chunk.CHUNK_SIZE * pos.Z)] = id;
				//chunk.GetData().SetCube(pos, id, false);
			}
			else
			{
				//Make no attempt to cache in this case. We'll likely miss
				ChunkPosition chunkPos = ChunkPosition.CubeChunk(pos.InCubeSpace(chunk));

				var posInNewChunk = pos.InChunkSpace(manager.GetChunk(chunkPos));

				if (manager.GetChunk(chunkPos).GetData().GenStep == ChunkData.GenerationStep.Broad)
					manager.GenerateChunkBroad(chunkPos);

				manager.GetChunk(chunkPos).GetData().SetCube(posInNewChunk, id, false);
				if (manager.GetChunk(chunkPos).Initialized)
					 detailCascadedChunks.Add(manager.GetChunk(chunkPos));
				manager.MarkDirty(chunkPos, false);
			}
		}

		protected void HandleCascaded()
        {
			foreach (Chunk cascadedChunk in detailCascadedChunks)
			{
				cascadedChunk.PostChunkGen(cascadedChunk.GetWorld());
			}

			detailCascadedChunks.Clear();
		}

		private void SetCubeOrAdjacent(ChunkGenerationThreadDataBus dataBus, Chunk chunk, CubePosition pos, ushort id, HashSet<Chunk> cascadedChunks)
		{
			if (pos.Coord == CubePosition.CoordinateSpace.ChunkSpace)
				throw new Exception("Cannot use chunk space");

			if (!dataBus.GetManager().IsInWorldBounds(pos))
				return;

			Chunk cchunk = dataBus.GetChunk(ChunkPosition.CubeChunk(pos), this);

			if (cchunk.GetData().GenStep == ChunkData.GenerationStep.Broad)
				GenerateChunkBroad(cchunk);

			cchunk.GetData().SetCube(pos.InChunkSpace(cchunk), id, false);

			if (!cascadedChunks.Contains(cchunk))
				cascadedChunks.Add(cchunk);

			return;

			/*if (cchunk.GetData().IsInChunkBounds(pos))
			{
				chunk.GetData().SetCube(pos, id, false);

				if (!cascadedChunks.Contains(chunk))
					cascadedChunks.Add(chunk);
			}
			else
			{
				ChunkPosition chunkPos = ChunkPosition.CubeChunk(pos.InCubeSpace(chunk));

				Chunk newChunk = dataBus.GetChunk(chunkPos, this);

				var posInNewChunk = pos.InChunkSpace(newChunk);

				if (newChunk.GetData().GenStep == ChunkData.GenerationStep.Broad)
					GenerateChunkBroad(newChunk, chunkPos);

				newChunk.GetData().SetCube(posInNewChunk, id, false);

				if (!cascadedChunks.Contains(newChunk))
					cascadedChunks.Add(newChunk);
			}*/
		}

		private int OldGen(Chunk chunk, CubePosition position)
		{
			position = position.InCubeSpace(chunk);

			float noiseHTop = (noise.GetNoise(position.X, position.Z) + 1f) / 2f;

			const float ampNoiseHTop = 5;
			const float top = 20;

			if (position.Y < top + noiseHTop * ampNoiseHTop)
			{
				float noiseHStone = (noise.GetSimplex(position.X, position.Z) + 1f) / 2f;

				const float ampNoiseHStone = 12;
				const float stone = 10;

				if (position.Y < stone + noiseHStone * ampNoiseHStone)
					return 3;
				else
				{
					if (position.Y == (int)(top + noiseHTop * ampNoiseHTop))
						return 2;
					else return 1;
				}
			}

			return 0;
		}
	}
}
