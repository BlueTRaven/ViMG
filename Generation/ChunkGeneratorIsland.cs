using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Cubes;
using BrUtility;
using System.Runtime.CompilerServices;
using System.Diagnostics;

namespace ViMG.Generation
{
    public class ChunkGeneratorIsland : ChunkGenerator
    {
		private static ushort[] BlacklistOre = new ushort[] { 0, Main.Registry.CubeRegistry.Get("stone").Id };
        private delegate float EaseFunction(float scale);

		private float[,] presetHeightmap;

		private int holeLocationX;
		private int holeLocationY;
		private int holeRadius = 16;
		private int minHoleRadius = 8;

		private const int SEA_FLOOR = 128;
		public const int SEA_LEVEL = SEA_FLOOR + 48;
		private const int ISLAND_TOP = SEA_FLOOR + 64;
		private const int ISLAND_RANGE = ISLAND_TOP - SEA_FLOOR;

		private StructureGenerator.StructureGeneratorBatchCollection structureBatchesGOL3D;
		private StructureGenerator.StructureGeneratorBatchCollection structureBatchesOreIron;
		private StructureGenerator.StructureGeneratorBatchCollection structureBatchesOreGlow;
		private StructureGenerator.StructureGeneratorBatchCollection structureBatchesOreTin;
		private StructureGenerator.StructureGeneratorBatchCollection structureBatchesOreCopper;


		public ChunkGeneratorIsland(int seed = 1337) : base(seed)
        {
		}

		public override void Initialize(World world)
        {
            base.Initialize(world);

			holeLocationX = GetRandom().Next(192, 320);
			holeLocationY = GetRandom().Next(192, 320);

			Texture2D tex = Main.assetsManager.GetAsset<Texture2D>("island_preset_noise");
			Color[] colors = new Color[tex.Width * tex.Height];
			tex.GetData(colors);

			presetHeightmap = new float[tex.Width, tex.Height];

			for (int i = 0; i < tex.Width * tex.Height; i++)
			{
				int x = i % tex.Width;
				int y = i / tex.Height;

				presetHeightmap[x, y] = 1 - ((float)colors[i].R / 255f);
			}

			structureBatchesGOL3D = new StructureGeneratorGOL3D(seed, null).Generate(56, 8);
			structureBatchesOreIron = new StructureGeneratorOre(Main.Registry.CubeRegistry.Get("ore_iron").Id,
				3, 6, seed, null).Generate(18, 3);
			structureBatchesOreGlow = new StructureGeneratorOre(Main.Registry.CubeRegistry.Get("ore_glowdust").Id,
				4, 12, seed, null).Generate(18, 3);
			structureBatchesOreTin = new StructureGeneratorOre(Main.Registry.CubeRegistry.Get("ore_tin").Id,
				2, 5, seed, null).Generate(18, 3);
			structureBatchesOreCopper = new StructureGeneratorOre(Main.Registry.CubeRegistry.Get("ore_copper").Id,
				2, 5, seed, null).Generate(18, 3);

			/*Main.Registry.CubeRegistry.Get("ore_iron");
		Cube oreGlow = Main.Registry.CubeRegistry.Get("ore_glowdust");
		Cube oreTin = Main.Registry.CubeRegistry.Get("ore_tin");
		Cube oreCopper = Main.Registry.CubeRegistry.Get("ore_copper");*/
			/*GenerateOreDetail(manager, chunk, pos, 3, 6, sample - 64, 0, 1f / 1024f, Easings.EaseLinear,
							oreIron, stone);

			GenerateOreDetail(manager, chunk, pos, 4, 12, sample - 16, 0, 1f / 800f, Easings.EaseLinear,
				oreGlow, stone);

			GenerateOreDetail(manager, chunk, pos, 2, 5, sample - 16, 96, 1f / 1024f, Easings.EaseLinear,
				oreTin, stone);

			GenerateOreDetail(manager, chunk, pos, 2, 5, sample - 24, 48, 1f / 1024f, Easings.EaseLinear,
				oreCopper, stone);*/

			//Structure test = structureBatchesOreIron.Get(3);
		}

        public override Vector3 GetPlayerPosition(World world, ChunkManager chunks)
        {
			int x = Main.random.Next(world.sizeInCubes / 2 - 4, world.sizeInCubes / 2 + 4);
			int z = Main.random.Next(world.sizeInCubes / 2 - 4, world.sizeInCubes / 2 + 4);

			CubePosition playerPos = CubePosition.FromWorldSpace(new Vector3(world.sizeInCubes * Cube.CUBE_SCALE / 2f, world.sizeInCubes * Cube.CUBE_SCALE, world.sizeInCubes * Cube.CUBE_SCALE / 2f));
			playerPos.X = x;
			playerPos.Z = z;
			playerPos.Y = world.sizeInCubes;

			return world.GetFirstSolidDown(playerPos.InWorldSpace(null)).InWorldSpace(null) + new Vector3(0, Cube.CUBE_SCALE * 3, 0);
		}

        public override void GenerateChunkBroad(Chunk chunk)
        {
			if (chunk.GetData().GenStep != ChunkData.GenerationStep.Broad)
				throw new Exception("");

			var cubes = chunk.GetData().GetAll();

			int[,] heightMap = GenerateHeight(chunk);

			for (int x = 0; x < Chunk.CHUNK_SIZE; x++)
			{
				for (int y = 0; y < Chunk.CHUNK_SIZE; y++)
				{
					for (int z = 0; z < Chunk.CHUNK_SIZE; z++)
					{
						var pos = new CubePosition(x, y, z, CubePosition.CoordinateSpace.ChunkSpace);

						ushort id = GenerateCubeBroad(chunk, pos, heightMap);

						if (id == 9)
							Console.WriteLine("???");
						cubes[x + Chunk.CHUNK_SIZE * (y + Chunk.CHUNK_SIZE * z)] = id;
						//SetCube(chunk, pos, id);
					}
				}
			}

			chunk.GetData().GenStep = ChunkData.GenerationStep.Detail;
		}

		private static int numBigCavesGenerated = 0;

        public override void GenerateChunkDetail(ChunkManager manager, Chunk chunk, ChunkPosition position)
        {
			Cube stone = Main.Registry.CubeRegistry.Get("stone");
			Cube oreIron = Main.Registry.CubeRegistry.Get("ore_iron");
			Cube oreGlow = Main.Registry.CubeRegistry.Get("ore_glowdust");
			Cube oreTin = Main.Registry.CubeRegistry.Get("ore_tin");
			Cube oreCopper = Main.Registry.CubeRegistry.Get("ore_copper");
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

						if (pos.Y == sample + 1 && pos.Y > SEA_LEVEL && GetRandom().Next(0, 256) == 0)
						{
							int num = GetRandom().Next(3, 12);
							for (int i = 0; i < num; i++)
							{
								var posOffset = pos;
								posOffset.Y += i;

								SetCubeOrAdjacent(manager, chunk, posOffset, 6);
							}
						}

						if (pos.Y < sample - 64)
						{
							double shouldDoBigCave = GetRandom().NextDouble();
							if (shouldDoBigCave < 1.0 / 400000.0)
							{
								numBigCavesGenerated++;

								/*int caveW = GetRandom().Next(16, 64);
								int caveH = GetRandom().Next(16, 64);
								int caveZ = GetRandom().Next(16, 64);*/

								Structure structure = structureBatchesGOL3D.Get(GetRandom().Next(0, structureBatchesGOL3D.num));

								PlaceStructure(manager, chunk, structure, pos, BlacklistAir);// Span<int>.Empty);

								/*GOL3DSim sim = new GOL3DSim(GetRandom(), caveW, caveH, caveZ, 15, 0.4f, 13, 10);
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
													if (GetRandom().NextDouble() < 0.25)
													{
														SetCubeOrAdjacent(manager, chunk, gol3dPos, Main.Registry.CubeRegistry.Get("altar_brick").Id);
													}

													if ((noise.GetSimplex(gol3dPos.X, gol3dPos.Y, gol3dPos.Z) + 1) / 2f < 0.25f)
													{
														if (GetRandom().NextDouble() < 0.0125)
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
								}*/
							}
						}

						if (pos.Y < sample - 64)
                        {
							bool doIron = GetRandom().NextFloat() < 1f / 1024f;
							bool doGlow = GetRandom().NextFloat() < 1f / 800f;
							bool doTin = GetRandom().NextFloat() < 1f / 1024f;
							bool doCopper = GetRandom().NextFloat() < 1f / 1024f;
							if (doIron) 
								PlaceStructure(manager, chunk, structureBatchesOreIron.Get(GetRandom().Next(0, structureBatchesOreIron.num)), pos, BlacklistOre);

							if (doGlow)
								PlaceStructure(manager, chunk, structureBatchesOreGlow.Get(GetRandom().Next(0, structureBatchesOreGlow.num)), pos, BlacklistOre);

							if (doTin)
								PlaceStructure(manager, chunk, structureBatchesOreTin.Get(GetRandom().Next(0, structureBatchesOreTin.num)), pos, BlacklistOre);

							if (doCopper)
								PlaceStructure(manager, chunk, structureBatchesOreCopper.Get(GetRandom().Next(0, structureBatchesOreCopper.num)), pos, BlacklistOre);
						}

						/*GenerateOreDetail(manager, chunk, pos, 3, 6, sample - 64, 0, 1f / 1024f, Easings.EaseLinear,
							oreIron, stone);

						GenerateOreDetail(manager, chunk, pos, 4, 12, sample - 16, 0, 1f / 800f, Easings.EaseLinear,
							oreGlow, stone);

						GenerateOreDetail(manager, chunk, pos, 2, 5, sample - 16, 96, 1f / 1024f, Easings.EaseLinear,
							oreTin, stone);

						GenerateOreDetail(manager, chunk, pos, 2, 5, sample - 24, 48, 1f / 1024f, Easings.EaseLinear,
							oreCopper, stone);*/
					}
				}
			}

			HandleCascaded();

			chunk.GetData().GenStep = ChunkData.GenerationStep.Done;
		}

		private void PlaceStructure(ChunkManager manager, Chunk baseChunk, Structure structure, CubePosition pos, Span<ushort> overwriteBlacklist)
        {
			for (int x = 0; x < structure.size.x; x++)
            {
				for (int y = 0; y < structure.size.y; y++)
                {
					for (int z = 0; z < structure.size.z; z++)
                    {
						Util.ThreeDToOneD(new ValuePoint3D(x, y, z), new ValuePoint3D(structure.size.x, structure.size.y, structure.size.z), out int i);
						CubePosition realPos = new CubePosition(pos.X + x, pos.Y + y, pos.Z + z, pos.Coord);

						if (!overwriteBlacklist.IsEmpty)
						{
							int overwritingId = manager.GetCube(realPos).GetOrDefault(Main.Registry.CubeRegistry.Air).Id;

							bool canOverwrite = true;
							for (int j = 0; j < overwriteBlacklist.Length; j++)
							{
								if (overwriteBlacklist[j] == overwritingId)
									canOverwrite = false;
							}

							if (canOverwrite)
								SetCubeOrAdjacent(manager, baseChunk, realPos, structure.data[i]);
						}
						else
						{
							//No restrictions on overwriting, so this is slightly faster than checking.
							SetCubeOrAdjacent(manager, baseChunk, realPos, structure.data[i]);
						}
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

					heightMap[x, z] = (int)(((float)presetHeightmap[cubePos.X, cubePos.Y] * ISLAND_RANGE) + SEA_FLOOR) +
						(int)(noise.GetNoise(x + chunk.Position.X * Chunk.CHUNK_SIZE, z + chunk.Position.Z * Chunk.CHUNK_SIZE) * 4);
				}
			}

			return heightMap;
		}

		private ushort GenerateCubeBroad(Chunk chunk, CubePosition cubeSpacePos, int[,] heightMap)
		{
			CubePosition chunkSpacePos = cubeSpacePos;
			cubeSpacePos = cubeSpacePos.InCubeSpace(chunk);

			if (Hole(cubeSpacePos, chunkSpacePos, heightMap))
			{
				if (cubeSpacePos.Y < 48)
					return 2;
				else return 0;
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
						if (GetRandom().NextDouble() < 1.0 / Math.Pow(16.0, 3.0))
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

		private ushort GenerateCubeCaveLayer(Chunk chunk, CubePosition cubeSpacePos, int[,] heightMap)
		{
			//return 3;

			CubePosition chunkSpacePosition = cubeSpacePos.InChunkSpace(chunk);

			//int offset = random.Next();

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

		private bool Hole(CubePosition cubeSpacePos, CubePosition chunkSpacePos, int[,] heightMap)
		{
			Vector2 dir = new Vector2(cubeSpacePos.X, cubeSpacePos.Z) - new Vector2(holeLocationX, holeLocationY);

			if (dir.Length() < holeRadius)
			{
				if (cubeSpacePos.Y <= heightMap[chunkSpacePos.X, chunkSpacePos.Z])
				{
					float percent = (float)cubeSpacePos.Y / ((float)heightMap[chunkSpacePos.X, chunkSpacePos.Z]);

					if (dir.Length() < MathHelper.Lerp(minHoleRadius, holeRadius, Easings.Ease(percent)))
						return true;
				}
			}

			return false;
		}

		private void GenerateOreDetail(ChunkManager manager, Chunk chunk, CubePosition startPos, int minSize, int maxSize, int minDepth, int maxDepth, float chance,
			EaseFunction spawnEaseFunction, Cube ore, Cube mediumCube)
		{
			if (startPos.Y >= maxDepth && startPos.Y < minDepth)
			{
				float easeScale = (float)(startPos.Y - minDepth) / (float)(maxDepth - minDepth);

				float easeChance = spawnEaseFunction(easeScale);

				float realChance = easeChance * chance;

				bool shouldSpawn = GetRandom().NextFloat(0, 1) < realChance;

				if (!shouldSpawn)
					return;

				int size = GetRandom().Next(minSize, maxSize);

				CubePosition nextPos = startPos;
				Chunk nextChunk = chunk;

				while (nextChunk != null && size > 0 && chunk.GetData().GetCube(nextPos.InChunkSpace(nextChunk))
					.GetOrDefault(Main.Registry.CubeRegistry.Air) == mediumCube)
				{
					SetCubeOrAdjacent(manager, chunk, nextPos, ore.Id);

					int nextDirection = GetRandom().Next(0, 6);

					switch (nextDirection)
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

					nextChunk = manager.GetChunk(nextPos);

					size--;
				}
			}
		}
	}
}
