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
		private Structure ellipsoidAtBottomOfHole;
		private Structure obelisk;
		private Structure house;

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

										SetCubeOrAdjacent(manager, chunk, posOffset, 6);    //Tree
									}
								}
								else if (GetRandom().Next(0, 256) == 0)
								{
									SetCubeOrAdjacent(manager, chunk, pos, Main.Registry.CubeRegistry.Get("sapling").Id);    //Sapling
								}
								else if (GetRandom().Next(0, 256) == 0)
								{
									SetCubeOrAdjacent(manager, chunk, pos, Main.Registry.CubeRegistry.Get("fibrous_plant").Id); //Fibrous plant
								}
							}
						}

						if (pos.Y < sample - 64)
						{
							double shouldDoBigCave = GetRandom().NextDouble();
							if (shouldDoBigCave < 1.0 / 200000.0)
							{
								numBigCavesGenerated++;

								/*int caveW = GetRandom().Next(16, 64);
								int caveH = GetRandom().Next(16, 64);
								int caveZ = GetRandom().Next(16, 64);*/

								Structure structure = structureBatchesGOL3D.Get(GetRandom().Next(0, structureBatchesGOL3D.num));

								PlaceStructureWithBlacklist(manager, chunk, structure, pos, BlacklistAir, Span<ushort>.Empty);
							}
						}

						if (pos.Y < sample - 64)
                        {
							bool doIron = GetRandom().NextFloat() < 1f / 512f;
							bool doGlow = GetRandom().NextFloat() < 1f / 400f;
							bool doTin = GetRandom().NextFloat() < 1f / 512f;
							bool doCopper = GetRandom().NextFloat() < 1f / 512f;
							if (doIron) 
								PlaceStructureWithBlacklist(manager, chunk, structureBatchesOreIron.Get(GetRandom().Next(0, structureBatchesOreIron.num)), pos, 
									BlacklistOre, Span<ushort>.Empty);

							if (doGlow)
								PlaceStructureWithBlacklist(manager, chunk, structureBatchesOreGlow.Get(GetRandom().Next(0, structureBatchesOreGlow.num)), pos, 
									BlacklistOre, Span<ushort>.Empty);

							if (doTin)
								PlaceStructureWithBlacklist(manager, chunk, structureBatchesOreTin.Get(GetRandom().Next(0, structureBatchesOreTin.num)), pos, 
									BlacklistOre, Span<ushort>.Empty);

							if (doCopper)
								PlaceStructureWithBlacklist(manager, chunk, structureBatchesOreCopper.Get(GetRandom().Next(0, structureBatchesOreCopper.num)), pos, 
									BlacklistOre, Span<ushort>.Empty);
						}
					}
				}
			}

			HandleCascaded();

			chunk.GetData().GenStep = ChunkData.GenerationStep.Done;
		}

        public override void PostGenerateDetail(ChunkManager manager)
        {
            base.PostGenerateDetail(manager);

			Vector2 holePos = new Vector2(holeLocationX, holeLocationY);

			PlaceStructureWithBlacklist(manager, null, ellipsoidAtBottomOfHole, 
				new CubePosition(holeLocationX - 32, 32, holeLocationY - 32, CubePosition.CoordinateSpace.CubeSpace), 
				BlacklistAir, Span<ushort>.Empty);

			while (true)
			{
				Vector2 offset = GetRandom().NextAngle() * (holeRadius + 16);

				var solidPos = manager.GetFirstSolidDown(new Vector3(holePos.X + offset.X, 512, holePos.Y + offset.Y) * Cube.CUBE_SCALE);

				if (solidPos.HasValue())
				{
					PlaceStructureWithBlacklist(manager, null, obelisk, solidPos.Get() - new CubePosition(0, 3, 0, CubePosition.CoordinateSpace.CubeSpace), 
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
					PlaceStructureWithBlacklist(manager, null, house, solidPos.Get() - new CubePosition(0, 3, 0, CubePosition.CoordinateSpace.CubeSpace),
						Span<ushort>.Empty, BlacklistAir);
				}
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="manager"></param>
		/// <param name="baseChunk"></param>
		/// <param name="structure"></param>
		/// <param name="pos"></param>
		/// <param name="overwriteWorldBlacklist">Structure cubes will not overwrite cubes of this type in the world.</param>
		/// <param name="dontwriteStructureBlacklist">If the structure encounters a cube of this type when placing, it will not place it.
		/// For instance, if your structure is padded by air, you might not want to overwrite the world with that.</param>
        private void PlaceStructureWithBlacklist(ChunkManager manager, Chunk baseChunk, Structure structure, CubePosition pos, 
			Span<ushort> overwriteWorldBlacklist, Span<ushort> dontwriteStructureBlacklist)
        {
			Chunk realBaseChunk = baseChunk;

			if (baseChunk == null)
				realBaseChunk = manager.GetChunk(pos);

			for (int x = 0; x < structure.size.X; x++)
            {
				for (int y = 0; y < structure.size.Y; y++)
                {
					for (int z = 0; z < structure.size.Z; z++)
                    {
						Util.ThreeDToOneD(new ValuePoint3D(x, y, z), new ValuePoint3D(structure.size.X, structure.size.Y, structure.size.Z), out int i);
						CubePosition realPos = new CubePosition(pos.X + x, pos.Y + y, pos.Z + z, pos.Coord);

						bool canWrite = true;
						//Allow structure cube to be overwritten (rather, not written) by world.
						if (!dontwriteStructureBlacklist.IsEmpty)
                        {
							for (int j = 0; j < dontwriteStructureBlacklist.Length; j++)
                            {
								if (structure.data[i] == dontwriteStructureBlacklist[j])
									canWrite = false;
                            }
                        }

						//Allow world cube to be overwritten by structure
						if (!overwriteWorldBlacklist.IsEmpty)
						{
							int overwritingId = manager.GetCube(realPos).GetOrDefault(Main.Registry.CubeRegistry.Air).Id;

							for (int j = 0; j < overwriteWorldBlacklist.Length; j++)
							{
								if (overwriteWorldBlacklist[j] == overwritingId)
									canWrite = false;
							}
						}

						if (canWrite)
							SetCubeOrAdjacent(manager, realBaseChunk, realPos, structure.data[i]);
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
						return GenerateCubeCaveLayer(chunk, cubeSpacePos, heightMap);
					}
					else
					{
						if (GetRandom().NextDouble() < 1.0 / Math.Pow(16.0, 3.0))
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
	}
}
