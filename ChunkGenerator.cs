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
						SetCube(chunk, pos, id);
					}
				}
			}

			chunk.GetData().GenStep = ChunkData.GenerationStep.Detail;
		}

		private List<Chunk> detailCascadedChunks = new List<Chunk>();

		public void GenerateChunkDetail(ChunkManager manager, Chunk chunk, ChunkPosition position)
		{
			int[,] heightMap = GenerateHeight(chunk);

			for (int x = 0; x < Chunk.CHUNK_SIZE; x++)
			{
				for (int y = 0; y < Chunk.CHUNK_SIZE; y++)
				{
					for (int z = 0; z < Chunk.CHUNK_SIZE; z++)
					{
						var pos = new CubePosition(x, y, z, CubePosition.CoordinateSpace.ChunkSpace);
						pos = pos.InCubeSpace(chunk);

						int sample = heightMap[x, z];

						if (pos.Y == sample + 1 && pos.Y > SEA_LEVEL && random.Next(0, 256) == 0)
						{
							int num = random.Next(3, 12);
							for (int i = 0; i < num; i++)
							{
								var posOffset = pos;
								posOffset.Y += i;

								SetCubeOrAdjacent(manager, chunk, posOffset, 6);
							}
						}

						if (pos.Y < sample - 64)
						{
							if (random.NextDouble() < 1.0 / 400000.0)
							{
								int caveW = random.Next(16, 64);
								int caveH = random.Next(16, 64);
								int caveZ = random.Next(16, 64);

								GOL3DSim sim = new GOL3DSim(random, caveW, caveH, caveZ, 15, 0.4f, 13, 10);

								sim.DoSim();

								List<CubePosition> altarSpawnPositions = new List<CubePosition>();

								for (int sx = 0; sx < caveW; sx++)
								{
									for (int sy = 0; sy < caveH; sy++)
									{
										for (int sz = 0; sz < caveZ; sz++)
										{
											CubePosition gol3dPos = pos + new CubePosition(sx, sy, sz, CubePosition.CoordinateSpace.CubeSpace);

											if (!sim.Get(sx, sy, sz))
											{
												SetCubeOrAdjacent(manager, chunk, gol3dPos, 0);
											}
											else
											{
												if (sy + 1 < caveH && !sim.Get(sx, sy + 1, sz))
												{
													if (random.NextDouble() < 0.25)
													{
														SetCubeOrAdjacent(manager, chunk, gol3dPos, Main.Registry.CubeRegistry.Get("altar_brick").Id);
													}

													if ((noise.GetSimplex(gol3dPos.X, gol3dPos.Y, gol3dPos.Z) + 1) / 2f < 0.25f)
													{
														if (random.NextDouble() < 0.0125)
														{
															gol3dPos.Y += 1;
															altarSpawnPositions.Add(gol3dPos);
														}
														SetCubeOrAdjacent(manager, chunk, gol3dPos, Main.Registry.CubeRegistry.Get("altar_brick").Id);
													}
												}
											}
										}
									}
								}

								foreach (CubePosition altarSpawnPosition in altarSpawnPositions)
								{
									SetCubeOrAdjacent(manager, chunk, altarSpawnPosition, Main.Registry.CubeRegistry.Get("ancient_altar").Id);
								}
							}
						}

						GenerateOreDetail(manager, chunk, pos, 3, 6, sample - 64, 0, 1f / 1024f, EaseLinear,
							Main.Registry.CubeRegistry.Get("ore_iron"), Main.Registry.CubeRegistry.Get("stone"));

						GenerateOreDetail(manager, chunk, pos, 4, 12, sample - 16, 0, 1f / 800f, EaseLinear,
							Main.Registry.CubeRegistry.Get("ore_glowdust"), Main.Registry.CubeRegistry.Get("stone"));

						GenerateOreDetail(manager, chunk, pos, 2, 5, sample - 16, 96, 1f / 1024f, EaseLinear,
							Main.Registry.CubeRegistry.Get("ore_tin"), Main.Registry.CubeRegistry.Get("stone"));

						GenerateOreDetail(manager, chunk, pos, 2, 5, sample - 24, 48, 1f / 1024f, EaseLinear,
							Main.Registry.CubeRegistry.Get("ore_copper"), Main.Registry.CubeRegistry.Get("stone"));
					}
				}
			}

			foreach (Chunk cascadedChunk in detailCascadedChunks)
			{
				cascadedChunk.PostChunkGen(cascadedChunk.GetWorld());
			}

			detailCascadedChunks.Clear();

			chunk.GetData().GenStep = ChunkData.GenerationStep.Done;
		}

		private void SetCube(Chunk chunk, CubePosition pos, int id)
		{
			chunk.GetData().SetCube(pos, id, false);
		}

		private void SetCubeOrAdjacent(ChunkManager manager, Chunk chunk, CubePosition pos, int id)
		{
			if (!manager.IsInWorldBounds(pos))
				return;

			// Chunks that have already been fully generated can be marked as dirty
			if (chunk.GetData().IsInChunkBounds(pos))
			{
				chunk.GetData().SetCube(pos, id, false);
			}
			else
			{
				ChunkPosition chunkPos = ChunkPosition.CubeChunk(pos.InCubeSpace(chunk));

				var posInNewChunk = pos.InChunkSpace(manager.GetChunk(chunkPos));

				if (manager.GetChunk(chunkPos).GetData().GenStep == ChunkData.GenerationStep.Broad)
					manager.GenerateChunkBroad(chunkPos);

				manager.GetChunk(chunkPos).GetData().SetCube(posInNewChunk, id, false);
				if (manager.GetChunk(chunkPos).Initialized)
					detailCascadedChunks.Add(manager.GetChunk(chunkPos));
				manager.MarkDirty(chunkPos);
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
				{
					return 2;
				}
				else
				{
					if (cubeSpacePos.Y >= sample - 4 && cubeSpacePos.Y <= sample && cubeSpacePos.Y <= SEA_LEVEL)
					{
						return Main.Registry.CubeRegistry.Get("sand").Id;
					}

					if (cubeSpacePos.Y < sample - 8)
					{
						return GenerateCubeCaveLayer(chunk, cubeSpacePos, heightMap);
					}
					else
					{
						if (random.NextDouble() < 1.0 / Math.Pow(16.0, 3.0))
							return Main.Registry.CubeRegistry.Get("brittle_bone_block").Id;
						return 1;
					}
				}
			}
			else
			{
				//water if below sea level, air otherwise
				if (cubeSpacePos.Y < SEA_LEVEL)
					return 4;
				else return 0;
			}
		}

		private int GenerateCubeCaveLayer(Chunk chunk, CubePosition cubeSpacePos, int[,] heightMap)
		{
			//return 3;

			CubePosition chunkSpacePosition = cubeSpacePos.InChunkSpace(chunk);

			int offset = random.Next();

			float noise3d1 = ((noise.GetNoise(cubeSpacePos.X, cubeSpacePos.Y, cubeSpacePos.Z) + 1) / 2);

			int distance = heightMap[chunkSpacePosition.X, chunkSpacePosition.Z] - cubeSpacePos.Y;
			int start = 12;
			int end = 32;

			float scalar = (float)(distance - start) / (float)(end - start);

			scalar = Math.Clamp(scalar, 0, 1);

			if (noise3d1 * scalar < 0.85f)
				return 3;
			else return 0;
		}

		private delegate float EaseFunction(float scale);

		private void GenerateOreDetail(ChunkManager manager, Chunk chunk, CubePosition startPos, int minSize, int maxSize, int minDepth, int maxDepth, float chance, EaseFunction spawnEaseFunction, Cube ore, Cube mediumCube)
		{
			if (startPos.Y >= maxDepth && startPos.Y < minDepth) 
			{
				float easeScale = (float)(startPos.Y - minDepth) / (float)(maxDepth - minDepth);

				float easeChance = spawnEaseFunction(easeScale);

				float realChance = easeChance * chance;

				bool shouldSpawn = random.NextFloat(0, 1) < realChance;

				if (!shouldSpawn)
					return;

				int size = random.Next(minSize, maxSize);

				CubePosition nextPos = startPos;
				Chunk nextChunk = chunk;
				int lastDirection = 0;

				while (size > 0 && chunk.GetData().GetCube(nextPos.InChunkSpace(nextChunk)).GetOrDefault(Main.Registry.CubeRegistry.Air) == mediumCube)
				{
					SetCubeOrAdjacent(manager, chunk, nextPos, ore.Id);
					nextChunk = manager.GetChunk(nextPos);

					lastDirection = random.Next(0, 6);

					switch (lastDirection)
					{
						case 0:
							nextPos = nextPos + new CubePosition(1, 0, 0, CubePosition.CoordinateSpace.CubeSpace);
							break;
						case 1:
							nextPos = nextPos - new CubePosition(1, 0, 0, CubePosition.CoordinateSpace.CubeSpace);
							break;
						case 2:
							nextPos = nextPos + new CubePosition(0, 1, 0, CubePosition.CoordinateSpace.CubeSpace);
							break;
						case 3:
							nextPos = nextPos - new CubePosition(0, 1, 0, CubePosition.CoordinateSpace.CubeSpace);
							break;
						case 4:
							nextPos = nextPos + new CubePosition(0, 0, 1, CubePosition.CoordinateSpace.CubeSpace);
							break;
						case 5:
							nextPos = nextPos - new CubePosition(0, 0, 1, CubePosition.CoordinateSpace.CubeSpace);
							break;
					}

					size--;
				}
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

		private float EaseLinear(float t)
		{
			return t;
		}

		private float Ease(float t)
		{
			return t == 0 ? 0 : (float)Math.Pow(2f, 10f * t - 10f);
			//return 1 - (float)Math.Sqrt(1 - (float)Math.Pow(t, 2));
		}

		private float EaseReverse(float t)
		{
			return 1 - Ease(t);
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
