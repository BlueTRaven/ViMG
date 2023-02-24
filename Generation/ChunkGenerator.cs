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
		protected static ushort[] BlacklistNone = Array.Empty<ushort>();
		protected static ushort[] BlacklistAir = new ushort[1] { 0 };
		protected FastNoise noise;
		private Random random;
		private Random threadRandom;

		public readonly int Layer;
		public readonly int Seed;

		protected int layerYOffsetInCubes;
		protected int layerYOffsetInChunks;

		public ChunkGenerator(int layer, int seed = 1337)
		{
			this.Layer = layer;
			this.Seed = seed;

			random = new Random(seed);
			threadRandom = new Random(seed - 256);

			noise = new FastNoise(seed);
			noise.SetNoiseType(FastNoise.NoiseType.Simplex);
			noise.SetFrequency(1f / (float)Chunk.CHUNK_SIZE);

		}

		public virtual void Initialize(int sizeInCubesXZ, int sizeInChunksY)
		{
			layerYOffsetInChunks = Layer * sizeInChunksY;
			layerYOffsetInCubes = layerYOffsetInChunks * sizeInCubesXZ;
		}

		protected Random GetRandom()
        {
			if (Thread.CurrentThread == Main.MainThread)
				return random;
			else return threadRandom;
        }

		public abstract Vector3 GetPlayerPosition(World world, ChunkManager chunks);

		public abstract void GenerateChunkBroad(ChunkGeneratorTasker.BroadGenerationState state);

		private List<Chunk> detailCascadedChunks = new List<Chunk>();

		// Main thread version
		public abstract void GenerateChunkDetail(ChunkManager manager, ChunkPosition position);

		public virtual void PostGenerateDetail(World world, ChunkManager manager) 
		{
			
		}

		//Creates a random position for a structure in such a way that it will not interfere with other structures.
		protected Vector2 GetRandomPositionForStructure(Vector2 xzSize)
        {
			return Vector2.Zero;
        }
	}
}
