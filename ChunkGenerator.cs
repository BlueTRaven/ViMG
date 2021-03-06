using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ViMG.Cubes;

namespace ViMG
{
	public class ChunkGenerator
	{
		private FastNoise noise;

		private float[,] presetNoiseColors;

		private Random random;

		private int holeLocationX;
		private int holeLocationY;
		private int holeRadius = 16;
		private int minHoleRadius = 8;

		private const int SEA_FLOOR = 128;
		private const int SEA_LEVEL = SEA_FLOOR + 48;
		private const int ISLAND_TOP = SEA_FLOOR + 64;
		private const int ISLAND_RANGE = ISLAND_TOP - SEA_FLOOR;

		public ChunkGenerator(int seed = 1337)
		{
			random = new Random(seed);
			noise = new FastNoise(seed);
			noise.SetNoiseType(FastNoise.NoiseType.Simplex);
			noise.SetFrequency(1f / (float)Chunk.CHUNK_SIZE);

			Texture2D tex = Main.assetsManager.GetAsset<Texture2D>("island_preset_noise");
			Color[] colors = new Color[tex.Width * tex.Height];
			tex.GetData(colors);

			presetNoiseColors = new float[tex.Width, tex.Height];

			for (int i = 0; i < tex.Width * tex.Height; i++)
			{
				int x = i % tex.Width;
				int y = i / tex.Height;

				presetNoiseColors[x, y] = 1 - ((float)colors[i].R / 255f);
			}
		}

		public void Initialize(World world)
		{
			holeLocationX = random.Next(192, 320);
			holeLocationY = random.Next(192, 320);
		}

		public Chunk MakeChunk(GenericPool<ChunkData> chunkDatas, ChunkPosition position)
		{
			return new Chunk(chunkDatas, position);
		}

		public void GenerateChunkBroad(Chunk chunk, ChunkPosition position)
		{
			int[,] heightMap = GenerateHeight(chunk);

			for (int x = 0; x < Chunk.CHUNK_SIZE; x++)
			{
				for (int y = 0; y < Chunk.CHUNK_SIZE; y++)
				{
					for (int z = 0; z < Chunk.CHUNK_SIZE; z++)
					{	
						var pos = new CubePosition(x, y, z, CubePosition.CoordinateSpace.ChunkSpace);

						int id = GenerateCubeBroad(chunk, pos, heightMap);
						chunk.GetData().SetCube(pos, id, false);
					}
				}
			}
		}

		public void GenerateChunkDetail(Chunk chunk, ChunkPosition position)
		{
			int[,] heightMap = GenerateHeight(chunk);

			for (int x = 0; x < Chunk.CHUNK_SIZE; x++)
			{
				for (int y = 0; y < Chunk.CHUNK_SIZE; y++)
				{
					for (int z = 0; z < Chunk.CHUNK_SIZE; z++)
					{
						int sample = heightMap[x, z];

						if (y == sample + 1 && random.Next(0, 32) == 0)
						{
							
						}
					}
				}
			}

			for (int x = 0; x < Chunk.CHUNK_SIZE; x++)
			{
				for (int y = 0; y < Chunk.CHUNK_SIZE; y++)
				{
					for (int z = 0; z < Chunk.CHUNK_SIZE; z++)
					{
						var pos = new CubePosition(x, y, z, CubePosition.CoordinateSpace.ChunkSpace);

						chunk.GetData().GetCube(x, y, z).GetOrDefault(Main.Registry.CubeRegistry.Air).PostGenerate(chunk.GetData(), pos);
					}
				}
			}
		}

		private int[,] GenerateHeight(Chunk chunk)
		{
			int[,] heightMap = new int[Chunk.CHUNK_SIZE, Chunk.CHUNK_SIZE];

			for (int x = 0; x < Chunk.CHUNK_SIZE; x++)
			{
				for (int z = 0; z < Chunk.CHUNK_SIZE; z++)
				{
					Point cubePos = new Point(x + chunk.Position.X * Chunk.CHUNK_SIZE, z + chunk.Position.Z * Chunk.CHUNK_SIZE);

					heightMap[x, z] = (int)(((float)presetNoiseColors[cubePos.X, cubePos.Y] * ISLAND_RANGE) + SEA_FLOOR) + 
						(int)(noise.GetNoise(x + chunk.Position.X * Chunk.CHUNK_SIZE, z + chunk.Position.Z * Chunk.CHUNK_SIZE) * 4);
				}
			}

			return heightMap;
		}

		private int GenerateCubeBroad(Chunk chunk, CubePosition cubeSpacePos, int[,] heightMap)
		{
			CubePosition chunkSpacePos = cubeSpacePos;
			cubeSpacePos = cubeSpacePos.InCubeSpace(chunk);

			int hole = Hole(cubeSpacePos, chunkSpacePos, heightMap);

			if (hole != -1)
			{
				if (hole != 0)
					if (cubeSpacePos.Y < 48)
						return 2;

				return hole;
			}

			int sample = heightMap[chunkSpacePos.X, chunkSpacePos.Z];

			if (cubeSpacePos.Y <= sample)
			{
				if (cubeSpacePos.Y == sample && cubeSpacePos.Y >= SEA_LEVEL)
					return 2;
				else
				{
					if (cubeSpacePos.Y < sample - 8)
					{
						if (Main.random.Next(0, 32) == 0)
							return 5;
						else return 3;
					}
					else return 1;
				}
			}
			else
			{
				if (cubeSpacePos.Y == sample + 1 && random.Next(0, 32) == 0)
					return 6;
				
				if (cubeSpacePos.Y < SEA_LEVEL)
					return 4;
				else return 0;
			}
		}

		private int Hole(CubePosition cubeSpacePos, CubePosition chunkSpacePos, int[,] heightMap)
		{
			Vector2 dir = new Vector2(cubeSpacePos.X, cubeSpacePos.Z) - new Vector2(holeLocationX, holeLocationY);

			if (dir.Length() < holeRadius)
			{
				if (cubeSpacePos.Y <= heightMap[chunkSpacePos.X, chunkSpacePos.Z])
				{
					float percent = (float)cubeSpacePos.Y / ((float)heightMap[chunkSpacePos.X, chunkSpacePos.Z]);

					if (dir.Length() < MathHelper.Lerp(minHoleRadius, holeRadius, Ease(percent)))
						return 0;
				}
			}

			return -1;
		}

		private float Ease(float t)
		{
			return t == 0 ? 0 : (float)Math.Pow(2f, 10f * t - 10f);
			//return 1 - (float)Math.Sqrt(1 - (float)Math.Pow(t, 2));
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
