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
using ViMG.Entities;
using ViMG.GameStates;
using Engine.Items;
using ModGameBase.Generation;
using BrUtility.Src;
using Engine;

namespace ViMG.Generation
{
	// TODO:
	// Structure placement can be multithreaded.
	// Each structure should have a bounding box. Every structure that overlaps another gets a different color.
	// Two structures with the same color cannot be placed at the same time.
	// In addition we can probably multithread the placement of individual structures by divying up the blocks
	// to place (parallel for?)
    public class ChunkGeneratorIsland : ChunkGenerator
    {
		private static ushort[] BlacklistOre = new ushort[] 
		{
			0, 
			GlobalState.Registry.CubeRegistry.Get("water").Id,
			GlobalState.Registry.CubeRegistry.Get("sand").Id,
		};
		private static ushort[] BlacklistCave = new ushort[]
		{
			0,
			GlobalState.Registry.CubeRegistry.Get("sand").Id,
			GlobalState.Registry.CubeRegistry.Get("water").Id,
			GlobalState.Registry.CubeRegistry.Get("ore_tin").Id,
			GlobalState.Registry.CubeRegistry.Get("ore_copper").Id,
			GlobalState.Registry.CubeRegistry.Get("ore_glowdust").Id,
			GlobalState.Registry.CubeRegistry.Get("ore_iron").Id
		};

		private static string[] SpecialItemCaveChest = new string[]
		{
			"leather_gloves",
			"heart_metal",
			"stone_lily",
			"stone_blunderbuss",
			"sword_rusted",
			"sword_ornamental",
			"flintlock_pistol",
			"book_spell_bubble",
			"book_spell_winds"
		};

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

		private StructureGenerator.StructureGeneratorBatchCollection structureBatchesGOL3DAltarCaves;
		private StructureGenerator.StructureGeneratorBatchCollection structureBatchesGOL3DShroomCaves;
		private StructureGenerator.StructureGeneratorBatchCollection structureBatchesGOL3DWaterCaves;
		private StructureGenerator.StructureGeneratorBatchCollection structureBatchesOreIron;
		private StructureGenerator.StructureGeneratorBatchCollection structureBatchesOreGlow;
		private StructureGenerator.StructureGeneratorBatchCollection structureBatchesOreTin;
		private StructureGenerator.StructureGeneratorBatchCollection structureBatchesOreCopper;
		private Structure ellipsoidAtBottomOfHole;
		private Structure obelisk;
		private Structure house;
		private Structure geode;
		private Structure[] dungeon;
		private Structure[] shrine;

		public ChunkGeneratorIsland(int layer, int seed = 1337) : base(layer, seed)
        {
		}

        public override void Initialize(int sizeInCubesXZ, int sizeInChunksY)
		{
			base.Initialize(sizeInCubesXZ, sizeInChunksY);

			holeLocationX = GetRandom().Next(192, 320);
			holeLocationY = GetRandom().Next(192, 320);

			int width = 0, height = 0;
			Color[] colors;
			using (FileStream fs = new FileStream("Content/Textures/island_preset_noise.xnb", FileMode.Open, FileAccess.Read))
			{
				fs.Seek(65, SeekOrigin.Begin);
				using (BinaryReader reader = new BinaryReader(fs))
				{
					var surfaceFormat = (SurfaceFormat)reader.ReadInt32();
					Debug.Assert(surfaceFormat == SurfaceFormat.Color);

					width = reader.ReadInt32();
					height = reader.ReadInt32();
					colors = new Color[width * height];

					fs.Seek(81, SeekOrigin.Begin);
					int datalen = reader.ReadInt32();
					var start = fs.Position;

					int i = 0;
					while (fs.Position < start + datalen)
					{
						byte r = reader.ReadByte();
						byte g = reader.ReadByte();
						byte b = reader.ReadByte();
						byte a = reader.ReadByte();

						Color c = new Color(r, g, b, a);
						colors[i] = c;
						i += 1;
					}
				}
			}

			presetHeightmap = new float[width, height];

			for (int i = 0; i < width * height; i++)
			{
				int x = i % width;
				int y = i / height;

				presetHeightmap[x, y] = 1 - ((float)colors[i].R / 255f);
			}

			if (GlobalState.GEN_DETAIL)
			{
				
				structureBatchesGOL3DAltarCaves = new StructureGeneratorGOL3DAltar(Seed, null).Generate(128, 8);
				structureBatchesGOL3DShroomCaves = new StructureGeneratorGOL3DShrooms(Seed, null).Generate(64, 8);
				structureBatchesGOL3DWaterCaves = new StructureGeneratorGOL3DWaterCave(Seed, null).Generate(56, 8);
				structureBatchesOreIron = new StructureGeneratorOre(GlobalState.Registry.CubeRegistry.Get("ore_iron").Id,
					3, 6, Seed, null).Generate(18, 3);
				structureBatchesOreGlow = new StructureGeneratorOre(GlobalState.Registry.CubeRegistry.Get("ore_glowdust").Id,
					4, 12, Seed, null).Generate(18, 3);
				structureBatchesOreTin = new StructureGeneratorOre(GlobalState.Registry.CubeRegistry.Get("ore_tin").Id,
					2, 5, Seed, null).Generate(18, 3);
				structureBatchesOreCopper = new StructureGeneratorOre(GlobalState.Registry.CubeRegistry.Get("ore_copper").Id,
					2, 5, Seed, null).Generate(18, 3);

				ushort[] sd = new ushort[64 * 16 * 64];

				ushort stone = GlobalState.Registry.CubeRegistry.Get("stone").Id;

				for (int i = 0; i < sd.Length; i++)
					sd[i] = stone;

				ValuePoint3D center = new ValuePoint3D(32, 8, 32);
				for (int i = 0; i < sd.Length; i++)
				{
					Util.OneDToThreeD(i, new ValuePoint3D(64, 16, 64), out ValuePoint3D pos);

					float dist = new Vector3((pos.x - center.x) / 4, pos.y - center.y, (pos.z - center.z) / 4).Length();

					if (dist < 8)
					{
						sd[i] = 0;
					}
				}

				ellipsoidAtBottomOfHole = new Structure(new Point3D(64, 16, 64), sd);
				obelisk = GlobalState.assetsManager.GetAsset<Structure>("obelisk");
				house = GlobalState.assetsManager.GetAsset<Structure>("house");
				geode = GlobalState.assetsManager.GetAsset<Structure>("lava_geode");
				dungeon = new Structure[4]
				{
				GlobalState.assetsManager.GetAsset<Structure>("dungeon"),
				GlobalState.assetsManager.GetAsset<Structure>("dungeon_tall"),
				GlobalState.assetsManager.GetAsset<Structure>("dungeon_hallway"),
				GlobalState.assetsManager.GetAsset<Structure>("dungeon_staircase"),
				};
				shrine = new Structure[2]
				{
				GlobalState.assetsManager.GetAsset<Structure>("shrine_new"),
				GlobalState.assetsManager.GetAsset<Structure>("shrine_old"),
				};
			}
		}

		public override Vector3 GetPlayerPosition(ChunkManager chunkManager)
        {
			int x = GlobalState.random.Next(chunkManager.SizeInCubes / 2 - 4, chunkManager.SizeInCubes / 2 + 4);
			int z = GlobalState.random.Next(chunkManager.SizeInCubes / 2 - 4, chunkManager.SizeInCubes / 2 + 4);

			CubePosition playerPos = CubePosition.FromWorldSpace(new Vector3(chunkManager.SizeInCubes * Cube.CUBE_SCALE / 2f,
				chunkManager.SizeInCubes * Cube.CUBE_SCALE, chunkManager.SizeInCubes * Cube.CUBE_SCALE / 2f));
			playerPos.X = x;
			playerPos.Z = z;
			playerPos.Y = chunkManager.SizeInCubes;

			return (chunkManager.CubeView.GetFirstSolidDown(playerPos).GetOrDefault(playerPos) + new CubePosition(0, 2, 0)).InWorldSpaceCenter();
		}

        public override void GenerateChunkBroad(ChunkGeneratorTasker.BroadGenerationState state)
        {
			int[,] heightMap = GenerateHeight(state.position);

			for (int x = 0; x < Chunk.CHUNK_SIZE; x++)
			{
				for (int y = 0; y < Chunk.CHUNK_SIZE; y++)
				{
					for (int z = 0; z < Chunk.CHUNK_SIZE; z++)
					{
						var pos = new CubePosition(x, y, z, CubePosition.CoordinateSpace.ChunkSpace);

						ushort id = GenerateCubeBroad(pos, heightMap, state);

						//TODO: 
						state.world.ChunkManager.CubeView.SetCube(pos.InCubeSpace(state.position), id, false);
						//cubes[x + Chunk.CHUNK_SIZE * (y + Chunk.CHUNK_SIZE * z)] = id;
					}
				}
			}
		}

		public override void GenerateChunkDetail(WorldPrototype world, ChunkPosition position)
        {
			int[,] heightMap = GenerateHeight(position);

			for (int x = 0; x < Chunk.CHUNK_SIZE; x++)
			{
				for (int y = 0; y < Chunk.CHUNK_SIZE; y++)
				{
					for (int z = 0; z < Chunk.CHUNK_SIZE; z++)
					{
						var pos = new CubePosition(x, y, z, CubePosition.CoordinateSpace.ChunkSpace);
						pos = pos.InCubeSpace(position);

						int sample = heightMap[x, z];

						if (pos.Y == sample + 1 && pos.Y > SEA_LEVEL)
						{
							var posBelow = new CubePosition(x, y - 1, z, CubePosition.CoordinateSpace.ChunkSpace).InCubeSpace(position);

							if (world.ChunkManager.CubeView.GetCube(posBelow).GetOrDefault(GlobalState.Registry.CubeRegistry.Air) == GlobalState.Registry.CubeRegistry.Get("grass"))
							{
								int val = GetRandom().Next(0, 256);
								if (val == 0)
								{
									int num = GetRandom().Next(3, 12);

									world.EntityManager.Add(new Tree(pos.InWorldSpace() + new Vector3(Cube.CUBE_SCALE * 0.5f, 0, Cube.CUBE_SCALE * 0.5f),
										num, pos), true);
								}
								else if (val == 1)
									world.ChunkManager.CubeView.SetCube(pos, GlobalState.Registry.CubeRegistry.Get("sapling").Id, false);    //Sapling
								else if (val == 2)
									world.ChunkManager.CubeView.SetCube(pos, GlobalState.Registry.CubeRegistry.Get("fibrous_plant").Id, false); //Fibrous plant
								else if (val == 3)
									world.ChunkManager.CubeView.SetCube(pos, GlobalState.Registry.CubeRegistry.Get("azure_flower").Id, false); //Azure flower
							}
						}
					}
				}
			}
		}

        public override void PostGenerateDetail(WorldPrototype world)
        {
            base.PostGenerateDetail(world);

			StructureGeneratorList l = new StructureGeneratorList();
			l.structuresToGen.Add(new StructureGeneratorList.StructureGeneration
			{
				structure = ellipsoidAtBottomOfHole,
				generatePos = new CubePosition(holeLocationX - 32, layerYOffsetInCubes + 32, holeLocationY - 32, CubePosition.CoordinateSpace.CubeSpace),
				dontwriteStructureBlacklist = Array.Empty<ushort>(),
				overwriteWorldBlacklist = BlacklistAir,
			});
			l.GenerateStructures(world.ChunkManager);

			Vector2 holePos = new Vector2(holeLocationX, holeLocationY);

			//ChunkHelper.PlaceStructureWithBlacklist(world.ChunkManager, ellipsoidAtBottomOfHole,
			//	new CubePosition(holeLocationX - 32, layerYOffsetInCubes + 32, holeLocationY - 32, CubePosition.CoordinateSpace.CubeSpace),
			//	BlacklistAir, Span<ushort>.Empty, false);

			//place one preset geode always located within the ellipsoid
			Vector2 geodeAng = GetRandom().NextAngle();
			float geodeDist = GetRandom().NextFloat(3, 24);

			CubePosition geodePos = new CubePosition(holeLocationX + (int)(geodeAng.X * geodeDist), layerYOffsetInCubes + 39, holeLocationY + (int)(geodeAng.Y * geodeDist));

			ChunkHelper.PlaceStructureWithBlacklist(world.ChunkManager, geode,
				geodePos, BlacklistNone, BlacklistNone, false);

			//also place a random number of other geodes throughout the world
			int numGeodes = GetRandom().Next(10, 15);

			for (int i = 0; i < numGeodes; i++)
            {
				int x = GetRandom().Next(0, world.ChunkManager.SizeInCubes);
				int z = GetRandom().Next(0, world.ChunkManager.SizeInCubes);

				ChunkHelper.PlaceStructureWithBlacklist(world.ChunkManager, geode,
					new CubePosition(x, layerYOffsetInCubes + 37, z), BlacklistNone, BlacklistNone, false);

				world.WorldInfo.pointsOfInterest.Add(new PointOfInterest(new CubePosition(x, layerYOffsetInCubes + 39, z), "geode", 1));
			}

			List<Rectangle3DI> cavePositions = new List<Rectangle3DI>();

			ProfilingHelper.Start("Generating Caves...");
			const int MAX_GOL_CAVES = 132;
			const int MAX_GOL_MCAVES = 128;
			GameStateTheIsland.ProgressMax = MAX_GOL_CAVES + MAX_GOL_MCAVES;
			GameStateTheIsland.ProgressMin = 0;
			for (int i = 0; i < MAX_GOL_CAVES; i++)
            {
				CubePosition randomPos = new CubePosition(GetRandom().Next(0, world.ChunkManager.SizeInCubes), 
					layerYOffsetInCubes + GetRandom().Next(0, SEA_FLOOR + 16), GetRandom().Next(0, world.ChunkManager.SizeInCubes));
				
				Structure structure = structureBatchesGOL3DAltarCaves.Get(i % structureBatchesGOL3DAltarCaves.num);
				cavePositions.Add(new Rectangle3DI(new Point3D(randomPos.X, randomPos.Y, randomPos.Z), structure.size));

				ChunkHelper.PlaceStructureWithBlacklist(world.ChunkManager, structure, randomPos, BlacklistCave, Span<ushort>.Empty, false);

                GameStateTheIsland.ProgressMin++;
            }

			for (int i = 0; i < MAX_GOL_MCAVES; i++)
			{
				CubePosition randomPos = new CubePosition(GetRandom().Next(0, world.ChunkManager.SizeInCubes), 
					layerYOffsetInCubes + GetRandom().Next(0, SEA_FLOOR + 16), GetRandom().Next(0, world.ChunkManager.SizeInCubes));

				Structure structure = structureBatchesGOL3DShroomCaves.Get(i % structureBatchesGOL3DShroomCaves.num);
				cavePositions.Add(new Rectangle3DI(new Point3D(randomPos.X, randomPos.Y, randomPos.Z), structure.size));

				ChunkHelper.PlaceStructureWithBlacklist(world.ChunkManager, structure, randomPos, BlacklistCave, Span<ushort>.Empty, false);

                GameStateTheIsland.ProgressMin++;
            }
			ProfilingHelper.End("Done.");

			/*ProfilingHelper.Start("Generating water caves and flood filling...");
			for (int i = 0; i < 216; i++)
            {
				CubePosition randomPos = new CubePosition(GetRandom().Next(0, manager.sizeInCubes), layerYOffsetInCubes + GetRandom().Next(0, SEA_FLOOR + 16), GetRandom().Next(0, manager.sizeInCubes));

				Structure structure = structureBatchesGOL3DWaterCaves.Get(GetRandom().Next(0, structureBatchesGOL3DWaterCaves.num));
				cavePositions.Add(new Rectangle3DI(new Point3D(randomPos.X, randomPos.Y, randomPos.Z), structure.size));

				StructureGeneratorGOL3DWaterCave.PlaceInWorld(manager, manager.GetChunk(randomPos), structure, randomPos);
			}
			ProfilingHelper.End("Done.");*/

			if (GlobalState.GEN_CAVES)
			{
				ProfilingHelper.Start("Generating cave connections...");
				const int MAX_CAVES = 800;
				GameStateTheIsland.ProgressMax = MAX_CAVES;
				GameStateTheIsland.ProgressMin = 0;
				for (int i = 0; i < MAX_CAVES; i++)
				{
					GenerateCaveConnection(world.ChunkManager, cavePositions);
					GameStateTheIsland.ProgressMin++;
				}
				ProfilingHelper.End("Done.");
			}

			ProfilingHelper.Start("Generating ores...");
			ProfilingHelper.Start("Copper...");
			//copper: 82037
			for (int i = 0; i < 80000; i++) 
			{
				CubePosition pos = new CubePosition(GetRandom().Next(0, world.ChunkManager.SizeInCubes),
					layerYOffsetInCubes + GetRandom().Next(0, ISLAND_TOP - 24), GetRandom().Next(0, world.ChunkManager.SizeInCubes), CubePosition.CoordinateSpace.CubeSpace);

				ChunkHelper.PlaceStructureWithBlacklist(world.ChunkManager, structureBatchesOreCopper.Get(GetRandom().Next(0, structureBatchesOreCopper.num)), pos,
					BlacklistOre, BlacklistAir, false);
			}
			ProfilingHelper.End("Done.");

			ProfilingHelper.Start("Tin...");
			//tin: 87799
			for (int i = 0; i < 90000; i++)
            {
				CubePosition pos = new CubePosition(GetRandom().Next(0, world.ChunkManager.SizeInCubes),
					layerYOffsetInCubes + GetRandom().Next(0, ISLAND_TOP - 16), GetRandom().Next(0, world.ChunkManager.SizeInCubes), CubePosition.CoordinateSpace.CubeSpace);

				ChunkHelper.PlaceStructureWithBlacklist(world.ChunkManager, structureBatchesOreTin.Get(GetRandom().Next(0, structureBatchesOreTin.num)), pos,
					BlacklistOre, BlacklistAir, false);
			}
			ProfilingHelper.End("Done.");

			ProfilingHelper.Start("Glow...");
			//glow: 114338
			for (int i = 0; i < 116000; i++)
            {
				CubePosition pos = new CubePosition(GetRandom().Next(0, world.ChunkManager.SizeInCubes),
					layerYOffsetInCubes + GetRandom().Next(0, ISLAND_TOP - 24), GetRandom().Next(0, world.ChunkManager.SizeInCubes), CubePosition.CoordinateSpace.CubeSpace);

				ChunkHelper.PlaceStructureWithBlacklist(world.ChunkManager, structureBatchesOreGlow.Get(GetRandom().Next(0, structureBatchesOreGlow.num)), pos,
					BlacklistOre, BlacklistAir, false);
			}
			ProfilingHelper.End("Done.");

			ProfilingHelper.Start("Iron...");
			//iron: 76813
			for (int i = 0; i < 75000; i++)
            {
				CubePosition pos = new CubePosition(GetRandom().Next(0, world.ChunkManager.SizeInCubes),
					layerYOffsetInCubes + GetRandom().Next(0, SEA_FLOOR - 32), GetRandom().Next(0, world.ChunkManager.SizeInCubes), CubePosition.CoordinateSpace.CubeSpace);

				ChunkHelper.PlaceStructureWithBlacklist(world.ChunkManager, structureBatchesOreIron.Get(GetRandom().Next(0, structureBatchesOreIron.num)), pos,
					BlacklistOre, BlacklistAir, false);
			}
			ProfilingHelper.End("Done.");
			ProfilingHelper.End("Done.");

			const int NUM_CAVE_CHESTS = 300;
			int spawnNum = NUM_CAVE_CHESTS;
			Span<CubePosition> positions = stackalloc CubePosition[NUM_CAVE_CHESTS];
			int lastPosition = 0;

			while (spawnNum > 0)
            {
				CubePosition pos = new CubePosition(GetRandom().Next(0, world.ChunkManager.SizeInCubes),
					layerYOffsetInCubes + GetRandom().Next(16, SEA_FLOOR), GetRandom().Next(0, world.ChunkManager.SizeInCubes), CubePosition.CoordinateSpace.CubeSpace);

				if (!world.ChunkManager.CubeView.GetCube(pos).GetOrDefault(GlobalState.Registry.CubeRegistry.Air).Solid)
                {
					var solidDown = world.ChunkManager.CubeView.GetFirstSolidDown(pos);

					if (solidDown.HasValue())
                    {
						CubePosition actualGenPos = solidDown.Get() + new CubePosition(0, 1, 0);

						if (IsNotNearAny(positions, lastPosition, actualGenPos, 16 * Cube.CUBE_SCALE))
						{
							world.ChunkManager.CubeView.SetCube(actualGenPos, GlobalState.Registry.CubeRegistry.Get("chest_wood").Id, false);

							int randomFace = GetRandom().Next();

							world.EntityManager.Add(new EntityChest(actualGenPos, GenerateGenericLoot(world.InventoryManager), 3, 3, GetRandom().RandomHorizontalFace()), true);
							world.WorldInfo.pointsOfInterest.Add(new PointOfInterest(actualGenPos, "chest", 1));

							positions[lastPosition++] = actualGenPos;

							spawnNum--;
						}
                    }
				}
			}

			ProfilingHelper.Start("Generating Dungeons...");
			const int NUM_DUNGEONS = 800;
			spawnNum = NUM_DUNGEONS;
			lastPosition = 0;

			positions = stackalloc CubePosition[NUM_DUNGEONS];

			while (spawnNum > 0)
			{
				CubePosition pos = new CubePosition(GetRandom().Next(0, world.ChunkManager.SizeInCubes),
					layerYOffsetInCubes + GetRandom().Next(16, SEA_FLOOR), GetRandom().Next(0, world.ChunkManager.SizeInCubes), CubePosition.CoordinateSpace.CubeSpace);

				if (world.ChunkManager.CubeView.GetCube(pos).GetOrDefault(GlobalState.Registry.CubeRegistry.Air).Solid)
				{
					if (IsNotNearAny(positions, lastPosition, pos, 16 * Cube.CUBE_SCALE))
					{
						ChunkHelper.PlaceStructureWithBlacklist(world.EntityManager, world.InventoryManager, world.ChunkManager, dungeon[GetRandom().Next(0, 4)], pos,
											Span<ushort>.Empty, PlaceDungeon, false);

						world.WorldInfo.pointsOfInterest.Add(new PointOfInterest(pos, "dungeon", 1));

						positions[lastPosition++] = pos;

						spawnNum--;
					}
				}
			}
			ProfilingHelper.End("Done.");

			const int NUM_GRAVES = 400;
			spawnNum = NUM_GRAVES;
			lastPosition = 0;

			positions = stackalloc CubePosition[NUM_GRAVES];

			ProfilingHelper.Start("Generating Graves...");
            while (spawnNum > 0)
            {
                CubePosition pos = new CubePosition(GetRandom().Next(0, world.ChunkManager.SizeInCubes),
                    layerYOffsetInCubes + GetRandom().Next(16, SEA_FLOOR), GetRandom().Next(0, world.ChunkManager.SizeInCubes), CubePosition.CoordinateSpace.CubeSpace);

                if (!world.ChunkManager.CubeView.GetCube(pos).GetOrDefault(GlobalState.Registry.CubeRegistry.Air).Solid)
                {
                    var solidDown = world.ChunkManager.CubeView.GetFirstSolidDown(pos);

                    if (solidDown.HasValue())
                    {
						CubePosition actualGenPos = solidDown.Get();

                        int which = GetRandom().Next(0, 2);

                        if (IsNotNearAny(positions, lastPosition, actualGenPos, 16 * Cube.CUBE_SCALE))
                        {
                            positions[lastPosition++] = actualGenPos;
                            
							world.ChunkManager.CubeView.SetCube(actualGenPos, GlobalState.Registry.CubeRegistry.Get("brittle_bone_block").Id, false);
                            world.ChunkManager.CubeView.SetCube(actualGenPos + new CubePosition(0, 1, 0), 
								GlobalState.Registry.CubeRegistry.Get("grave").Id, false);
							if (which == 1)
								world.ChunkManager.CubeView.SetCube(actualGenPos + new CubePosition(0, 2, 0), 
									GlobalState.Registry.CubeRegistry.Get("grave").Id, false);

                            world.WorldInfo.pointsOfInterest.Add(new PointOfInterest(actualGenPos, "grave", 1));

                            spawnNum--;
                        }
                    }
                }
            }
			ProfilingHelper.End("Done.");

            const int NUM_SHRINES = 300;
			spawnNum = NUM_SHRINES;
			lastPosition = 0;

			positions = stackalloc CubePosition[NUM_SHRINES];

			while (spawnNum > 0)
			{
				CubePosition pos = new CubePosition(GetRandom().Next(0, world.ChunkManager.SizeInCubes),
					layerYOffsetInCubes + GetRandom().Next(16, SEA_FLOOR), GetRandom().Next(0, world.ChunkManager.SizeInCubes), CubePosition.CoordinateSpace.CubeSpace);

				if (!world.ChunkManager.CubeView.GetCube(pos).GetOrDefault(GlobalState.Registry.CubeRegistry.Air).Solid)
				{
					var solidDown = world.ChunkManager.CubeView.GetFirstSolidDown(pos);

					if (solidDown.HasValue())
					{
						CubePosition actualGenPos;

						int which = GetRandom().Next(0, 3);

						if (which < 2)
							actualGenPos = solidDown.Get();
						else
							actualGenPos = solidDown.Get() + new CubePosition(0, 1, 0);

						if (IsNotNearAny(positions, lastPosition, actualGenPos, 16 * Cube.CUBE_SCALE))
						{
							positions[lastPosition++] = actualGenPos;
							if (which < 2)
								ChunkHelper.PlaceStructureWithBlacklist(world.EntityManager, world.InventoryManager, world.ChunkManager, shrine[which], actualGenPos,
									Span<ushort>.Empty, PlaceAltar, false);
							else world.ChunkManager.CubeView.SetCube(actualGenPos, ChunkHelper.ChooseShrine(GetRandom()).Id, false);

							world.WorldInfo.pointsOfInterest.Add(new PointOfInterest(actualGenPos, "shrine", 1));

							spawnNum--;
						}
					}
				}
			}

			const int NUM_HEARTS = 300;
			spawnNum = NUM_HEARTS;
			lastPosition = 0;

			positions = stackalloc CubePosition[NUM_HEARTS];

			while (spawnNum > 0)
			{
				CubePosition pos = new CubePosition(GetRandom().Next(0, world.ChunkManager.SizeInCubes),
					GetRandom().Next(16, SEA_FLOOR), GetRandom().Next(0, world.ChunkManager.SizeInCubes), CubePosition.CoordinateSpace.CubeSpace);

				if (!world.ChunkManager.CubeView.GetCube(pos).GetOrDefault(GlobalState.Registry.CubeRegistry.Air).Solid)
				{
					var solidDown = world.ChunkManager.CubeView.GetFirstSolidDown(pos);

					if (solidDown.HasValue())
					{
						CubePosition actualGenPos = solidDown.Get() + new CubePosition(0, 3, 0);

						//cheesy way of checking we're an air block without actually checking.
						//We can ensure any that anything <= pos.Y is an air block (since we traveled down to get to solidDown); not so if it's above it.
						if (actualGenPos.Y <= pos.Y)
						{
							if (IsNotNearAny(positions, lastPosition, actualGenPos, 32 * Cube.CUBE_SCALE))
							{
								positions[lastPosition++] = actualGenPos;

								world.EntityManager.Add(new Entities.Heart(actualGenPos.InWorldSpace() + new Vector3(Cube.CUBE_SCALE / 2f)), true);
								spawnNum--;
							}
						}
					}
				}
			}

			while (true)
			{
				Vector2 offset = GetRandom().NextAngle() * (holeRadius + 16);

				var solidPos = world.ChunkManager.CubeView.GetFirstSolidDown(CubePosition.FromWorldSpace(new Vector3(holePos.X + offset.X, 512, holePos.Y + offset.Y) * Cube.CUBE_SCALE));

				if (solidPos.HasValue())
				{
					ChunkHelper.PlaceStructureWithBlacklist(world.ChunkManager, obelisk, solidPos.Get() - new CubePosition(0, 3, 0, CubePosition.CoordinateSpace.CubeSpace), 
						Span<ushort>.Empty, BlacklistAir, false);

					world.WorldInfo.pointsOfInterest.Add(new PointOfInterest(solidPos.Get(), "obelisk", 1));
					break;
				}
			}

			{
				//Start at the center of the world (in cube space).
				//Get the angle from the center to the hole position, then rotate it by a random amount
				//All of this basically just to avoid putting the house anywhere near the hole.
				Vector2 housePos = new Vector2(512, 512) / 2;
				Angle ang = Angle.FromVector2(housePos - holePos);
				ang += Angle.FromDegrees(GetRandom().Next(45, 360 - 45));

				housePos += ang.Vector * (GetRandom().NextFloat(64, 196) * Cube.CUBE_SCALE);
				var solidPos = world.ChunkManager.CubeView.GetFirstSolidDown(CubePosition.FromWorldSpace(new Vector3(housePos.X, 512, housePos.Y) * Cube.CUBE_SCALE));

				if (solidPos.HasValue())
				{
				 	ChunkHelper.PlaceStructureWithBlacklist(world.EntityManager, world.InventoryManager, world.ChunkManager, house, solidPos.Get() - new CubePosition(0, 3, 0, CubePosition.CoordinateSpace.CubeSpace),
						Span<ushort>.Empty, PlaceHouse, false);

					world.WorldInfo.pointsOfInterest.Add(new PointOfInterest(solidPos.Get(), "house", 1));
				}
			}
		}

		private void GenerateCaveConnection(ChunkManager manager, List<Rectangle3DI> cavePositions)
		{
			int startCaveIndex = GetRandom().Next(0, cavePositions.Count);

			var startCave = cavePositions[startCaveIndex];

			var buf = new CubePosition[startCave.Size.X * startCave.Size.Y * startCave.Size.Z];
            FastStackList<CubePosition> airs = new(buf);
			ChunkHelper.SelectInArea(airs, manager, cavePositions[startCaveIndex], 0);
			if (airs.Length <= 0)
				return;
			CubePosition startPosition = airs[GetRandom().Next(0, airs.Length)];

			//Find the nearest 3 CubePositions to this point.
			int ni = 0;
			Span<int> nearests = stackalloc int[3];

			for (int i = 0; i < 3; i++)
			{
				int currentPosition = -1;
				float currentDistance = float.MaxValue;

				for (int j = 0; j < cavePositions.Count; j++)
				{
					//So we don't compare against self
					if (startCaveIndex == j)
						continue;

					CubePosition pos = new CubePosition(cavePositions[j].Position);
					pos = startPosition - pos;

					float compareDistance = MathF.Sqrt((pos.X * pos.X) + (pos.Y * pos.Y) + (pos.Z * pos.Z));

					//Make sure this value isn't already used.
					bool isUsed = false;
					for (int k = 0; k < ni; k++)
					{
						if (nearests[k] == j)
						{
							isUsed = true;
							break;
						}
					}

					if (!isUsed && compareDistance > 16 && compareDistance < currentDistance)
					{
						currentDistance = compareDistance;
						currentPosition = j;
					}
				}

				nearests[ni++] = currentPosition;
			}

			//all that just to choose between one of the three closest caves.
			int nearestIndex = GetRandom().Next(0, 3);
			var nearestR = cavePositions[nearests[nearestIndex]];
			buf = new CubePosition[nearestR.Size.X * nearestR.Size.Y * nearestR.Size.Z];
            airs = new(buf);
			ChunkHelper.SelectInArea(airs, manager, nearestR, 0);
			if (airs.Length <= 0)
				return;
			CubePosition endPosition = airs[GetRandom().Next(0, airs.Length)];//new CubePosition(cavePositions[nearests[GetRandom().Next(0, 3)]].Position);
			Vector3 dir = new Vector3(endPosition.X - startPosition.X, endPosition.Y - startPosition.Y, endPosition.Z - startPosition.Z);

			const float RADIUS_MIN = 2;
			const float RADIUS_MAX = 4;
			const float PERTURBATION = 8;
			int numSegments = (int)(dir.Length() % 8); 

			CubePosition[] segments = new CubePosition[numSegments];
			float[] radii = new float[numSegments];

			Vector3 dirNorm = Vector3.Normalize(dir);
			dir /= numSegments;
			for (int i = 0; i < numSegments; i++)
			{
				Vector3 segment = dir * (i + 1);

				if (i > 0 && i < numSegments - 1)
				{
					//https://answers.unity.com/questions/1618126/given-a-vector-how-do-i-generate-a-random-perpendi.html
					//Choose a random perpendicular vector to dir
					//We choose a perpendicular so as to not end up backtracking on ourselves.
					float du = Vector3.Dot(dirNorm, Vector3.Up);
					float df = Vector3.Dot(dirNorm, Vector3.Forward);
					Vector3 angle = MathF.Abs(du) < MathF.Abs(df) ? Vector3.Up : Vector3.Forward;

					Vector3 perp = Vector3.Cross(angle, dirNorm);

					float rotateBy = GetRandom().NextFloat(0, 360);
					perp = Vector3.Transform(perp, Quaternion.CreateFromAxisAngle(dirNorm, MathHelper.ToRadians(rotateBy)));

					segment += perp * GetRandom().NextFloat(0, PERTURBATION);
					/*segment += new Vector3(GetRandom().NextFloat(-PERTURBATION, PERTURBATION),
						GetRandom().NextFloat(-PERTURBATION, PERTURBATION),
						GetRandom().NextFloat(-PERTURBATION, PERTURBATION));*/
				}

				segments[i] = new CubePosition(startPosition.X + (int)segment.X, startPosition.Y + (int)segment.Y, startPosition.Z + (int)segment.Z);
				radii[i] = GetRandom().NextFloat(RADIUS_MIN, RADIUS_MAX);
			}

			/*Color color = new Color(GetRandom().NextFloat(), GetRandom().NextFloat(), GetRandom().NextFloat(), 1f);
			Main.Renderer.DEBUGMarkersSphere.Add(new Rendering.RendererDeferred.DEBUGDraw()
            {
				Position = startPosition.InWorldSpace(),
				Color = color * 1.25f,
				Scale = Vector3.One
            });

			Main.Renderer.DEBUGMarkersSphere.Add(new Rendering.RendererDeferred.DEBUGDraw()
			{
				Position = endPosition.InWorldSpace(),
				Color = color * 1.25f,
				Scale = Vector3.One
			});*/
			for (int i = 0; i < numSegments; i++)
			{
				/*Main.Renderer.DEBUGMarkersSphere.Add(new Rendering.RendererDeferred.DEBUGDraw()
				{
					Position = segments[i].InWorldSpace(),
					Color = color,
					Scale = Vector3.One / 4f
				});*/

				CubePosition prevSegmentPos = i == 0 ? startPosition : segments[i - 1];

				Vector3 x1 = new Vector3(segments[i].X, segments[i].Y, segments[i].Z);
				Vector3 x2 = new Vector3(prevSegmentPos.X, prevSegmentPos.Y, prevSegmentPos.Z);
				float len = (x2 - x1).Length();

				CubePosition basePos = segments[i];
				HashSet<CubePosition> used = new HashSet<CubePosition>();
				Queue<CubePosition> floodFills = new Queue<CubePosition>();
				floodFills.Enqueue(basePos);

				while (floodFills.Count > 0)
				{
					CubePosition toFill = floodFills.Dequeue();
					Vector3 x0 = new Vector3(toFill.X, toFill.Y, toFill.Z);

					//first check to make sure we lie on the line segment.
					//we can do this: |normalize(x2 - x1) dot x1 - x0| < |x2 - x1| and >= 0.
					float dot = Vector3.Dot(Vector3.Normalize(x2 - x1), x0 - x1);

					if (dot >= 0 && dot < len)
					{
						//https://mathworld.wolfram.com/Point-LineDistance3-Dimensional.html
						//distance from line equation, where toFill = x0, chosenPosition = x1, and usingPosition = x2
						float numerator = Vector3.Cross(x2 - x1, x1 - x0).Length();
						float denominator = (x2 - x1).Length();

						float distance = numerator / denominator;

						if (distance < radii[i] && !used.Contains(toFill) && manager.IsInWorldBounds(toFill))
						{
							used.Add(toFill);
							manager.CubeView.SetCube(toFill, 0, false);
							floodFills.Enqueue(new CubePosition(toFill.X - 1, toFill.Y, toFill.Z));
							floodFills.Enqueue(new CubePosition(toFill.X + 1, toFill.Y, toFill.Z));
							floodFills.Enqueue(new CubePosition(toFill.X, toFill.Y - 1, toFill.Z));
							floodFills.Enqueue(new CubePosition(toFill.X, toFill.Y + 1, toFill.Z));
							floodFills.Enqueue(new CubePosition(toFill.X, toFill.Y, toFill.Z - 1));
							floodFills.Enqueue(new CubePosition(toFill.X, toFill.Y, toFill.Z + 1));
						}
					}
				}
			}

			/*Vector3 x1 = new Vector3(chosenPosition.X, chosenPosition.Y, chosenPosition.Z);
			Vector3 x2 = new Vector3(usingPosition.X, usingPosition.Y, usingPosition.Z);
			float len = (x2 - x1).Length();

			CubePosition basePos = chosenPosition;
			HashSet<CubePosition> used = new HashSet<CubePosition>();
			Queue<CubePosition> floodFills = new Queue<CubePosition>();
			floodFills.Enqueue(basePos);

			while (floodFills.Count > 0)
			{
				CubePosition toFill = floodFills.Dequeue();
				Vector3 x0 = new Vector3(toFill.X, toFill.Y, toFill.Z);

				//first check to make sure we lie on the line segment.
				//we can do this: |normalize(x2 - x1) dot x1 - x0| < |x2 - x1| and >= 0.
				float dot = Vector3.Dot(Vector3.Normalize(x2 - x1), x0 - x1);

				if (dot >= 0 && dot < len)
				{
					//https://mathworld.wolfram.com/Point-LineDistance3-Dimensional.html
					//distance from line equation, where toFill = x0, chosenPosition = x1, and usingPosition = x2
					float numerator = Vector3.Cross(x2 - x1, x1 - x0).Length();
					float denominator = (x2 - x1).Length();

					float distance = numerator / denominator;

					if (distance < RADIUS && !used.Contains(toFill) && manager.IsInWorldBounds(toFill))
					{
						Chunk chunk = manager.GetChunk(toFill);

						if (chunk != null && chunk.Initialized)
						{
							used.Add(toFill);
							chunk.GetData().SetCube(toFill, 0, false, false);
							floodFills.Enqueue(new CubePosition(toFill.X - 1, toFill.Y, toFill.Z));
							floodFills.Enqueue(new CubePosition(toFill.X + 1, toFill.Y, toFill.Z));
							floodFills.Enqueue(new CubePosition(toFill.X, toFill.Y - 1, toFill.Z));
							floodFills.Enqueue(new CubePosition(toFill.X, toFill.Y + 1, toFill.Z));
							floodFills.Enqueue(new CubePosition(toFill.X, toFill.Y, toFill.Z - 1));
							floodFills.Enqueue(new CubePosition(toFill.X, toFill.Y, toFill.Z + 1));
						}
					}
				}
			}*/
		}

		//Returns whether or not placeAt is near (near being within minDistance distance) any position in alreadyPlacedPositions. This is O(n) over lastPlaced elements; short-circuits as soon as possible if false.
		private bool IsNotNearAny(Span<CubePosition> alreadyPlacedPositions, int lastPlaced, CubePosition placeAt, float minDistance)
        {
			for (int i = 0; i < lastPlaced; i++)
			{
				CubePosition pos = alreadyPlacedPositions[i];

				float distance = (pos.InWorldSpace() - placeAt.InWorldSpace()).Length();

				if (distance < minDistance)
					return false;
			}

			return true;
		}

		private bool PlaceHouse(EntityManager entityManager, InventoryManager inventoryManager, ChunkManager chunkManager, CubePosition position, Structure structure, int structureIndex, ref ushort id) 
		{
			if (id == 0)
				return false;

			if (id == GlobalState.Registry.CubeRegistry.Get("structure_replace_00").Id)
            {
				id = GlobalState.Registry.CubeRegistry.Get("furnace_t1").Id;
				entityManager.Add(new Entities.EntityFurnace(position, MeshHelper.CubeFace.RIGHT), true);

				return true;
            }

			if (id == GlobalState.Registry.CubeRegistry.Get("structure_replace_01").Id)
			{
				id = GlobalState.Registry.CubeRegistry.Get("chest_wood").Id;
				entityManager.Add(new Entities.EntityChest(position, GenerateHouseLoot(inventoryManager), 3, 3, MeshHelper.CubeFace.RIGHT), true);

				return true;
			}

			return true;
		}

		private bool PlaceDungeon(EntityManager entityManager, InventoryManager inventoryManager, ChunkManager chunkManager, CubePosition position, Structure structure, int structureIndex, ref ushort id)
        {
			Util.OneDToThreeD(structureIndex, new ValuePoint3D(structure.size), out ValuePoint3D structurePosition);

			//TODO replace structure_replace_00 with chest; 01 with bodies? skeletons? Something I haven't made yet. For now, air

			if (id == GlobalState.Registry.CubeRegistry.Get("structure_replace_00").Id)
			{
				id = GlobalState.Registry.CubeRegistry.Get("chest_wood").Id;
				entityManager.Add(new Entities.EntityChest(position, GenerateGenericLoot(inventoryManager), 3, 3, GetRandom().RandomHorizontalFace()), true);

				return true;
			}

			if (id == GlobalState.Registry.CubeRegistry.Get("structure_replace_01").Id)
			{
				id = (GetRandom().Next(0, 4) == 0) ? GlobalState.Registry.CubeRegistry.Get("bonepile").Id : (ushort)0;
				return true;
			}

			return true;
        }

		private bool PlaceAltar(EntityManager entityManager, InventoryManager inventoryManager, ChunkManager chunkManager, CubePosition position, Structure structure, int structureIndex, ref ushort id)
        {
			if (id == 0)
				return false;

			if (id == GlobalState.Registry.CubeRegistry.Get("structure_replace_00").Id)
			{
				id = ChunkHelper.ChooseShrine(GetRandom()).Id;

				return true;
			}

			return true;
		}

		private int[,] GenerateHeight(ChunkPosition position)
		{
			int[,] heightMap = new int[Chunk.CHUNK_SIZE, Chunk.CHUNK_SIZE];

			for (int x = 0; x < Chunk.CHUNK_SIZE; x++)
			{
				for (int z = 0; z < Chunk.CHUNK_SIZE; z++)
				{
					Point cubePos = new Point(x + position.X * Chunk.CHUNK_SIZE, z + position.Z * Chunk.CHUNK_SIZE);

					heightMap[x, z] = (int)(((float)presetHeightmap[cubePos.X, cubePos.Y] * ISLAND_RANGE) + SEA_FLOOR) +
						(int)(noise.GetNoise(x + position.X * Chunk.CHUNK_SIZE, z + position.Z * Chunk.CHUNK_SIZE) * 4);
				}
			}

			return heightMap;
		}

		private ushort GenerateCubeBroad(CubePosition cubeSpacePos, int[,] heightMap, ChunkGeneratorTasker.BroadGenerationState state)
		{
			CubePosition chunkSpacePos = cubeSpacePos;
			cubeSpacePos = cubeSpacePos.InCubeSpace(state.position);

			if (Hole(cubeSpacePos, chunkSpacePos, heightMap))
			{
				if (cubeSpacePos.Y < 48)
					return 3;
				else return 0;
			}

			int sample = heightMap[chunkSpacePos.X, chunkSpacePos.Z];

			if (cubeSpacePos.Y <= sample)
			{
				if (cubeSpacePos.Y == sample && cubeSpacePos.Y >= SEA_LEVEL)
				{
					return GlobalState.Registry.CubeRegistry.Get("grass").Id;
				}
				else
				{
					if (cubeSpacePos.Y >= sample - 4 && cubeSpacePos.Y <= sample && cubeSpacePos.Y <= SEA_LEVEL)
					{
						return GlobalState.Registry.CubeRegistry.Get("sand").Id;
					}

					if (cubeSpacePos.Y < sample - 8)
					{
						return GenerateCubeCaveLayer(cubeSpacePos, heightMap);
					}
					else
					{
						//TODO GetRandom causes issues when multithreading, sometimes always returning 0 for this
						//(thus replacing the entire dirt layer with brittle bone blocks)
						if (state.random.NextDouble() < 1.0 / Math.Pow(16.0, 3.0))
							return GlobalState.Registry.CubeRegistry.Get("brittle_bone_block").Id;
						return GlobalState.Registry.CubeRegistry.Get("dirt").Id;
					}
				}
			}
			else
			{
				//water if below sea level, air otherwise
				if (cubeSpacePos.Y < SEA_LEVEL)
					return GlobalState.Registry.CubeRegistry.Get("water").Id;
				else return 0;
			}
		}

		private ushort GenerateCubeCaveLayer(CubePosition cubeSpacePos, int[,] heightMap)
		{
			float noise3d1 = ((noise.GetNoise(cubeSpacePos.X, cubeSpacePos.Y, cubeSpacePos.Z) + 1) / 2);

			int distance = heightMap[cubeSpacePos.X % Chunk.CHUNK_SIZE, cubeSpacePos.Z % Chunk.CHUNK_SIZE] - cubeSpacePos.Y;
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

					if (dir.Length() < MathHelper.Lerp(minHoleRadius, holeRadius, Easings.EaseInExpo(percent)))
						return true;
				}
			}

			return false;
		}

		private InventoryManager.InventoryReference GenerateHouseLoot(InventoryManager inventoryManager)
        {
			List<Items.ItemInstance> inventoryItems = new List<Items.ItemInstance>();

			inventoryItems.Add(new Items.ItemInstance(GlobalState.Registry.ItemRegistry.Get("book_spell_ember"), 1, 1));
			inventoryItems.Add(new Items.ItemInstance(GlobalState.Registry.ItemRegistry.Get("run_leather_boots"), 1, 1));
			//inventoryItems.Add(new Items.ItemInstance(GlobalState.Registry.ItemRegistry.Get("book_story_01"), 1, 1));	//TODO

			var inventoryRef = inventoryManager.Add(new Inventory.InventoryConfig(9));
			var inventory = inventoryManager.Get(inventoryRef);

			for (int i = 0; i < inventoryItems.Count; i++)
			{
				inventory.Set(inventoryItems[i], i);
			}

			return inventoryRef;
		}

		private InventoryManager.InventoryReference GenerateGenericLoot(InventoryManager inventoryManager)
        {
			List<Items.ItemInstance> inventoryItems = new List<Items.ItemInstance>();

			string specialItem = SpecialItemCaveChest[GetRandom().Next(0, SpecialItemCaveChest.Length)];

			inventoryItems.Add(new Items.ItemInstance(GlobalState.Registry.ItemRegistry.Get(specialItem), 1, 1));

			if (GetRandom().NextCoinFlip())
				inventoryItems.Add(new Items.ItemInstance(GlobalState.Registry.ItemRegistry.Get("flask_healthpotion1"), GetRandom().Next(1, 4), 1));

			if (GetRandom().NextCoinFlip())
				inventoryItems.Add(new Items.ItemInstance(GlobalState.Registry.ItemRegistry.Get("ammo_arrow_stone"), GetRandom().Next(10, 20), 1));

			if (GetRandom().NextCoinFlip())
				inventoryItems.Add(new Items.ItemInstance(GlobalState.Registry.ItemRegistry.Get("ingot_iron"), GetRandom().Next(1, 2), 1));
			else inventoryItems.Add(new Items.ItemInstance(GlobalState.Registry.ItemRegistry.Get("ingot_bronze"), GetRandom().Next(1, 3), 1));

			int ingotGenPattern = GetRandom().Next(1, 3);
			if (ingotGenPattern == 0)
				inventoryItems.Add(new Items.ItemInstance(GlobalState.Registry.ItemRegistry.Get("ingot_tin"), GetRandom().Next(1, 4), 1));
			else if (ingotGenPattern == 1)
				inventoryItems.Add(new Items.ItemInstance(GlobalState.Registry.ItemRegistry.Get("ingot_copper"), GetRandom().Next(1, 4), 1));
			else
            {
				inventoryItems.Add(new Items.ItemInstance(GlobalState.Registry.ItemRegistry.Get("ingot_tin"), GetRandom().Next(1, 2), 1));
				inventoryItems.Add(new Items.ItemInstance(GlobalState.Registry.ItemRegistry.Get("ingot_copper"), GetRandom().Next(1, 2), 1));
			}

            var inventoryRef = inventoryManager.Add(new Inventory.InventoryConfig(9));
            var inventory = inventoryManager.Get(inventoryRef);

			for (int i = 0; i < inventoryItems.Count; i++)
            {
				inventory.Set(inventoryItems[i], i);
            }

			return inventoryRef;
        }
	}
}
