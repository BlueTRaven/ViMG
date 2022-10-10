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
		private static ushort[] BlacklistOre = new ushort[] 
		{
			0, 
			Main.Registry.CubeRegistry.Get("water").Id,
			Main.Registry.CubeRegistry.Get("sand").Id,
		};
		private static ushort[] BlacklistCave = new ushort[]
		{
			0,
			Main.Registry.CubeRegistry.Get("sand").Id,
			Main.Registry.CubeRegistry.Get("water").Id,
			Main.Registry.CubeRegistry.Get("ore_tin").Id,
			Main.Registry.CubeRegistry.Get("ore_copper").Id,
			Main.Registry.CubeRegistry.Get("ore_glowdust").Id,
			Main.Registry.CubeRegistry.Get("ore_iron").Id
		};

		private static string[] SpecialItemCaveChest = new string[]
		{
			"leather_gloves",
			"metal_heart",
			"stone_lily",
			"stone_blunderbuss",
			//TODO "ancient_sword",
			//TODO "ornamental_sword",
			"flintlock_pistol",
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
		private StructureGenerator.StructureGeneratorBatchCollection structureBatchesGOL3DWaterCaves;
		private StructureGenerator.StructureGeneratorBatchCollection structureBatchesOreIron;
		private StructureGenerator.StructureGeneratorBatchCollection structureBatchesOreGlow;
		private StructureGenerator.StructureGeneratorBatchCollection structureBatchesOreTin;
		private StructureGenerator.StructureGeneratorBatchCollection structureBatchesOreCopper;
		private Structure ellipsoidAtBottomOfHole;
		private Structure obelisk;
		private Structure house;
		private Structure geode;

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

			structureBatchesGOL3DAltarCaves = new StructureGeneratorGOL3DAltar(Seed, null).Generate(56, 8);
			structureBatchesGOL3DWaterCaves = new StructureGeneratorGOL3DWaterCave(Seed, null).Generate(56, 8);
			structureBatchesOreIron = new StructureGeneratorOre(Main.Registry.CubeRegistry.Get("ore_iron").Id,
				3, 6, Seed, null).Generate(18, 3);
			structureBatchesOreGlow = new StructureGeneratorOre(Main.Registry.CubeRegistry.Get("ore_glowdust").Id,
				4, 12, Seed, null).Generate(18, 3);
			structureBatchesOreTin = new StructureGeneratorOre(Main.Registry.CubeRegistry.Get("ore_tin").Id,
				2, 5, Seed, null).Generate(18, 3);
			structureBatchesOreCopper = new StructureGeneratorOre(Main.Registry.CubeRegistry.Get("ore_copper").Id,
				2, 5, Seed, null).Generate(18, 3);

			ushort[] sd = new ushort[64 * 16 * 64];

			ushort stone = Main.Registry.CubeRegistry.Get("stone").Id;

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
			obelisk = Main.assetsManager.GetAsset<Structure>("obelisk");
			house = Main.assetsManager.GetAsset<Structure>("house");
			geode = Main.assetsManager.GetAsset<Structure>("lava_geode");
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

        public override void GenerateChunkBroad(ChunkManager.BroadGenerationState state)
        {
			if (state.chunk.GetData().GenStep != ChunkData.GenerationStep.Broad)
				throw new Exception("");

			var cubes = state.chunk.GetData().GetAll();

			int[,] heightMap = GenerateHeight(state.chunk);

			for (int x = 0; x < Chunk.CHUNK_SIZE; x++)
			{
				for (int y = 0; y < Chunk.CHUNK_SIZE; y++)
				{
					for (int z = 0; z < Chunk.CHUNK_SIZE; z++)
					{
						var pos = new CubePosition(x, y, z, CubePosition.CoordinateSpace.ChunkSpace);

						ushort id = GenerateCubeBroad(pos, heightMap, state);

						cubes[x + Chunk.CHUNK_SIZE * (y + Chunk.CHUNK_SIZE * z)] = id;
					}
				}
			}

			state.chunk.GetData().GenStep = ChunkData.GenerationStep.Detail;
		}

		public override void GenerateChunkDetail(ChunkManager manager, Chunk chunk, ChunkPosition position)
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

						if (pos.Y == sample + 1 && pos.Y > SEA_LEVEL)
						{
							var posBelow = new CubePosition(x, y - 1, z, CubePosition.CoordinateSpace.ChunkSpace).InCubeSpace(chunk);

							if (chunk.GetData().GetCube(posBelow).GetOrDefault(Main.Registry.CubeRegistry.Air) == Main.Registry.CubeRegistry.Get("grass"))
							{
								if (GetRandom().Next(0, 256) == 0)
								{
									int num = GetRandom().Next(3, 12);
									for (int i = 0; i < num; i++)
									{
										var posOffset = pos;
										posOffset.Y += i;

										ChunkHelper.SetCubeOrAdjacent(manager, chunk, posOffset, 6);    //Tree
									}
								}
								else if (GetRandom().Next(0, 256) == 0)
								{
									ChunkHelper.SetCubeOrAdjacent(manager, chunk, pos, Main.Registry.CubeRegistry.Get("sapling").Id);    //Sapling
								}
								else if (GetRandom().Next(0, 256) == 0)
								{
									ChunkHelper.SetCubeOrAdjacent(manager, chunk, pos, Main.Registry.CubeRegistry.Get("fibrous_plant").Id); //Fibrous plant
								}
							}
						}
					}
				}
			}

			//HandleCascaded();

			chunk.GetData().GenStep = ChunkData.GenerationStep.Done;
		}

        public override void PostGenerateDetail(ChunkManager manager)
        {
            base.PostGenerateDetail(manager);

			Vector2 holePos = new Vector2(holeLocationX, holeLocationY);

			ChunkHelper.PlaceStructureWithBlacklist(manager.world, manager, null, ellipsoidAtBottomOfHole, 
				new CubePosition(holeLocationX - 32, 32, holeLocationY - 32, CubePosition.CoordinateSpace.CubeSpace), 
				BlacklistAir, Span<ushort>.Empty);

			//place one preset geode always located within the ellipsoid
			Vector2 geodeAng = GetRandom().NextAngle();
			float geodeDist = GetRandom().NextFloat(3, 24);

			CubePosition geodePos = new CubePosition((int)(geodeAng.X * geodeDist), 37, (int)(geodeAng.Y * geodeDist));

			ChunkHelper.PlaceStructureWithBlacklist(manager.world, manager, null, geode,
				geodePos, BlacklistNone, BlacklistNone);

			//also place a random number of other geodes throughout the world
			int numGeodes = GetRandom().Next(10, 15);

			for (int i = 0; i < numGeodes; i++)
            {
				int x = GetRandom().Next(0, manager.sizeInCubes);
				int z = GetRandom().Next(0, manager.sizeInCubes);

				ChunkHelper.PlaceStructureWithBlacklist(manager.world, manager, null, geode,
					new CubePosition(x, 37, z), BlacklistNone, BlacklistNone);
			}

			for (int i = 0; i < 132; i++)
            {
				CubePosition randomPos = new CubePosition(GetRandom().Next(0, manager.sizeInCubes), GetRandom().Next(0, SEA_FLOOR + 16), GetRandom().Next(0, manager.sizeInCubes));
				
				Structure structure = structureBatchesGOL3DAltarCaves.Get(GetRandom().Next(0, structureBatchesGOL3DAltarCaves.num));

				ChunkHelper.PlaceStructureWithBlacklist(manager.world, manager, manager.GetChunk(randomPos), structure, randomPos, BlacklistCave, Span<ushort>.Empty);
			}

			for (int i = 0; i < 216; i++)
            {
				CubePosition randomPos = new CubePosition(GetRandom().Next(0, manager.sizeInCubes), GetRandom().Next(0, SEA_FLOOR + 16), GetRandom().Next(0, manager.sizeInCubes));

				Structure structure = structureBatchesGOL3DWaterCaves.Get(GetRandom().Next(0, structureBatchesGOL3DWaterCaves.num));

				StructureGeneratorGOL3DWaterCave.PlaceInWorld(manager, manager.GetChunk(randomPos), structure, randomPos);
			}

			//copper: 82037
			for (int i = 0; i < 80000; i++) 
			{
				CubePosition pos = new CubePosition(GetRandom().Next(0, manager.sizeInCubes),
					GetRandom().Next(0, ISLAND_TOP - 24), GetRandom().Next(0, manager.sizeInCubes), CubePosition.CoordinateSpace.CubeSpace);

				ChunkHelper.PlaceStructureWithBlacklist(manager.world, manager, manager.GetChunk(pos), structureBatchesOreCopper.Get(GetRandom().Next(0, structureBatchesOreCopper.num)), pos,
					BlacklistOre, BlacklistAir);
			}

			//tin: 87799
			for (int i = 0; i < 90000; i++)
            {
				CubePosition pos = new CubePosition(GetRandom().Next(0, manager.sizeInCubes), 
					GetRandom().Next(0, ISLAND_TOP - 16), GetRandom().Next(0, manager.sizeInCubes), CubePosition.CoordinateSpace.CubeSpace);

				ChunkHelper.PlaceStructureWithBlacklist(manager.world, manager, manager.GetChunk(pos), structureBatchesOreTin.Get(GetRandom().Next(0, structureBatchesOreTin.num)), pos,
					BlacklistOre, BlacklistAir);
			}

			//glow: 114338
			for (int i = 0; i < 116000; i++)
            {
				CubePosition pos = new CubePosition(GetRandom().Next(0, manager.sizeInCubes), 
					GetRandom().Next(0, ISLAND_TOP - 24), GetRandom().Next(0, manager.sizeInCubes), CubePosition.CoordinateSpace.CubeSpace);

				ChunkHelper.PlaceStructureWithBlacklist(manager.world, manager, manager.GetChunk(pos), structureBatchesOreGlow.Get(GetRandom().Next(0, structureBatchesOreGlow.num)), pos,
					BlacklistOre, BlacklistAir);
			}

			//iron: 76813
			for (int i = 0; i < 75000; i++)
            {
				CubePosition pos = new CubePosition(GetRandom().Next(0, manager.sizeInCubes), 
					GetRandom().Next(0, SEA_FLOOR - 16), GetRandom().Next(0, manager.sizeInCubes), CubePosition.CoordinateSpace.CubeSpace);

				ChunkHelper.PlaceStructureWithBlacklist(manager.world, manager, manager.GetChunk(pos), structureBatchesOreIron.Get(GetRandom().Next(0, structureBatchesOreIron.num)), pos,
					BlacklistOre, BlacklistAir);
			}

			const int NUM_CAVE_CHESTS = 300;
			int spawnNum = NUM_CAVE_CHESTS;
			Span<ChunkPosition> chunks = stackalloc ChunkPosition[NUM_CAVE_CHESTS];
			int lastChunk = 0;

			while (spawnNum > 0)
            {
				CubePosition pos = new CubePosition(GetRandom().Next(0, manager.sizeInCubes),
					GetRandom().Next(16, SEA_FLOOR), GetRandom().Next(0, manager.sizeInCubes), CubePosition.CoordinateSpace.CubeSpace);

				if (!manager.GetCube(pos).GetOrDefault(Main.Registry.CubeRegistry.Air).Solid)
                {
					var solidDown = manager.GetFirstSolidDown(pos);

					if (solidDown.HasValue())
                    {
						CubePosition actualGenPos = solidDown.Get() + new CubePosition(0, 1, 0);
						ChunkPosition chunkPos = ChunkPosition.CubeChunk(actualGenPos);

						for (int i = 0; i < lastChunk; i++)
						{
							if (chunkPos == chunks[i])
								continue;
						}

						manager.GetChunk(actualGenPos).GetData().SetCube(actualGenPos, Main.Registry.CubeRegistry.Get("chest_wood").Id, false, false);

						int randomFace = GetRandom().Next();

                        manager.GetChunk(actualGenPos).GetWorld().EntityManager.Add(new Entities.EntityChest(actualGenPos, GenerateGenericLoot(), 3, 3, GetRandom().RandomHorizontalFace()));

						chunks[lastChunk++] = chunkPos;

						spawnNum--;
                    }
				}
			}

			while (true)
			{
				Vector2 offset = GetRandom().NextAngle() * (holeRadius + 16);

				var solidPos = manager.GetFirstSolidDown(new Vector3(holePos.X + offset.X, 512, holePos.Y + offset.Y) * Cube.CUBE_SCALE);

				if (solidPos.HasValue())
				{
					ChunkHelper.PlaceStructureWithBlacklist(manager.world, manager, null, obelisk, solidPos.Get() - new CubePosition(0, 3, 0, CubePosition.CoordinateSpace.CubeSpace), 
						Span<ushort>.Empty, BlacklistAir);
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
				var solidPos = manager.GetFirstSolidDown(new Vector3(housePos.X, 512, housePos.Y) * Cube.CUBE_SCALE);

				if (solidPos.HasValue())
				{
				 	ChunkHelper.PlaceStructureWithBlacklist(manager.world, manager, null, house, solidPos.Get() - new CubePosition(0, 3, 0, CubePosition.CoordinateSpace.CubeSpace),
						Span<ushort>.Empty, PlaceHouse);
				}
			}
		}

		private bool PlaceHouse(World world, ChunkManager chunkManager, CubePosition position, ref ushort id) 
		{
			if (id == 0)
				return false;

			if (id == Main.Registry.CubeRegistry.Get("structure_replace_00").Id)
            {
				id = Main.Registry.CubeRegistry.Get("furnace_t1").Id;
				world.EntityManager.Add(new Entities.EntityFurnace(position, MeshHelper.CubeFace.RIGHT));

				return true;
            }

			if (id == Main.Registry.CubeRegistry.Get("structure_replace_01").Id)
			{
				id = Main.Registry.CubeRegistry.Get("chest_wood").Id;
				world.EntityManager.Add(new Entities.EntityChest(position, GenerateHouseLoot(), 3, 3, MeshHelper.CubeFace.RIGHT));

				return true;
			}

			return true;
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

		private ushort GenerateCubeBroad(CubePosition cubeSpacePos, int[,] heightMap, ChunkManager.BroadGenerationState state)
		{
			CubePosition chunkSpacePos = cubeSpacePos;
			cubeSpacePos = cubeSpacePos.InCubeSpace(state.chunk);

			if (Hole(cubeSpacePos, chunkSpacePos, heightMap, state))
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
					return Main.Registry.CubeRegistry.Get("grass").Id;
				}
				else
				{
					if (cubeSpacePos.Y >= sample - 4 && cubeSpacePos.Y <= sample && cubeSpacePos.Y <= SEA_LEVEL)
					{
						return Main.Registry.CubeRegistry.Get("sand").Id;
					}

					if (cubeSpacePos.Y < sample - 8)
					{
						return GenerateCubeCaveLayer(state.chunk, cubeSpacePos, heightMap, state);
					}
					else
					{
						//TODO GetRandom causes issues when multithreading, sometimes always returning 0 for this
						//(thus replacing the entire dirt layer with brittle bone blocks)
						if (state.random.NextDouble() < 1.0 / Math.Pow(16.0, 3.0))
							return Main.Registry.CubeRegistry.Get("brittle_bone_block").Id;
						return Main.Registry.CubeRegistry.Get("dirt").Id;
					}
				}
			}
			else
			{
				//water if below sea level, air otherwise
				if (cubeSpacePos.Y < SEA_LEVEL)
					return Main.Registry.CubeRegistry.Get("water").Id;
				else return 0;
			}
		}

		private ushort GenerateCubeCaveLayer(Chunk chunk, CubePosition cubeSpacePos, int[,] heightMap, ChunkManager.BroadGenerationState state)
		{
			CubePosition chunkSpacePosition = cubeSpacePos.InChunkSpace(chunk);

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

		private bool Hole(CubePosition cubeSpacePos, CubePosition chunkSpacePos, int[,] heightMap, ChunkManager.BroadGenerationState state)
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

		private Inventory GenerateHouseLoot()
        {
			List<Items.ItemInstance> inventoryItems = new List<Items.ItemInstance>();

			inventoryItems.Add(new Items.ItemInstance(Main.Registry.ItemRegistry.Get("book_spell_ember"), 1, 1));

			Inventory inventory = new Inventory(9);

			for (int i = 0; i < inventoryItems.Count; i++)
			{
				inventory.Set(inventoryItems[i], i);
			}

			return inventory;
		}

		private Inventory GenerateGenericLoot()
        {
			List<Items.ItemInstance> inventoryItems = new List<Items.ItemInstance>();

			string specialItem = SpecialItemCaveChest[GetRandom().Next(0, SpecialItemCaveChest.Length)];

			inventoryItems.Add(new Items.ItemInstance(Main.Registry.ItemRegistry.Get(specialItem), 1, 1));

			if (GetRandom().NextCoinFlip())
				inventoryItems.Add(new Items.ItemInstance(Main.Registry.ItemRegistry.Get("flask_healthpotion1"), GetRandom().Next(1, 4), 1));

			if (GetRandom().NextFloat() < 0.75f)
				inventoryItems.Add(new Items.ItemInstance(Main.Registry.ItemRegistry.Get("string"), GetRandom().Next(1, 2), 1));

			if (GetRandom().NextCoinFlip())
				inventoryItems.Add(new Items.ItemInstance(Main.Registry.ItemRegistry.Get("ammo_arrow_stone"), GetRandom().Next(10, 20), 1));

			if (GetRandom().NextCoinFlip())
				inventoryItems.Add(new Items.ItemInstance(Main.Registry.ItemRegistry.Get("ingot_iron"), GetRandom().Next(1, 2), 1));
			else inventoryItems.Add(new Items.ItemInstance(Main.Registry.ItemRegistry.Get("ingot_bronze"), GetRandom().Next(1, 3), 1));

			int ingotGenPattern = GetRandom().Next(1, 3);
			if (ingotGenPattern == 0)
				inventoryItems.Add(new Items.ItemInstance(Main.Registry.ItemRegistry.Get("ingot_tin"), GetRandom().Next(1, 4), 1));
			else if (ingotGenPattern == 1)
				inventoryItems.Add(new Items.ItemInstance(Main.Registry.ItemRegistry.Get("ingot_copper"), GetRandom().Next(1, 4), 1));
			else
            {
				inventoryItems.Add(new Items.ItemInstance(Main.Registry.ItemRegistry.Get("ingot_tin"), GetRandom().Next(1, 2), 1));
				inventoryItems.Add(new Items.ItemInstance(Main.Registry.ItemRegistry.Get("ingot_copper"), GetRandom().Next(1, 2), 1));
			}

			Inventory inventory = new Inventory(9);

			for (int i = 0; i < inventoryItems.Count; i++)
            {
				inventory.Set(inventoryItems[i], i);
            }

			return inventory;
        }
	}
}
