using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
//using SimplexNoise;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ViMG.Cubes;
using ViMG.Entities;
using ViMG.GameStates;
using ViMG.Generation;
using ViMG.Items;
using ViMG.Spawners;

namespace ViMG
{
	public class World
	{
		public string LoadedFolderName;

		public const float GRAVITY = -9.8f / 20f * Cube.CUBE_SCALE;
		public const float DAY_CYCLE_TIME = 60f * 10f;

		private const float SUN_DISTANCE = -6 * Chunk.CHUNK_SIZE * Cube.CUBE_SCALE;
		private const float SUN_ANGLE = 5f;	//rotate 5 degrees
		public readonly int sizeInChunks;
		public readonly int sizeInCubes;

		public ChunkManager ChunkManager;

		private static SimpleMesh<VertexPositionColor, int> meshWireframeCube;
		private static SimpleMesh<VertexPositionColor, int> meshWireframeUnscaled;
		private static SimpleMesh<VertexCube, int> meshMiningCube;
		private static SimpleMesh<VertexCube, int> meshSun;
		private static (VertexBuffer VBO, IndexBuffer IBO) meshLavaQuad;

		private static (VertexBuffer VBO, IndexBuffer IBO) nightSkybox;
		private static SimpleMesh<VertexCube, int> meshMaxDrawDistBottom;
		private static bool meshesLoaded;

		public GameStateManager GameStateManager;
		public Player player;
		//private Vector3 playerSpawnPos;

		public int DrawDistanceHoriz = 6;   //radius in chunks that we should be able to see
		public int DrawDistanceVert = 6;
		public int DrawRadius = 6;

		public HitboxManager HitboxManager = new HitboxManager(32);
		public ProjectileManager ProjectileManager;
		public EntityManager EntityManager;
		public LightManager LightManager;
		public PassiveSpawnerManager PassiveSpawnerManager;
		public List<PointOfInterest> PointsOfInterest;

		public Color SkyColor = new Color(94, 107, 154);

		public List<ChunkPosition> CulledChunkDrawPositions = new List<ChunkPosition>();
		private bool chunkDrawPositionsDirty = true;
		private ChunkPosition oldChunkPosition;
		private Vector3 oldCameraRotation;

		private struct MinedCube
		{
			public CubePosition position;
			public Chunk chunk;
			public float timer;
			public int progress;	//goes up one per "mine"
		}

		private Dictionary<CubePosition, MinedCube> miningCubes = new Dictionary<CubePosition, MinedCube>();
		private List<CubePosition> miningRemove = new List<CubePosition>();
		private List<MinedCube> miningUpdate = new List<MinedCube>();

		private int lavaLight = -1;

		private WorldSaver saver;

		private WorldInfoIO worldInfoIO;
		private ChunkManagerIO chunkIO;
		private EntityManagerIO entIO;

		public ChunkLoadManager ChunkLoadManager;

		private const float SUN_LIGHT_DISTANCE = -Cube.CUBE_SCALE * 10;
		public DirectionalLight directionalLight;
		private int currentCascadeDebug;

		private static Color[] duskColors = new Color[] { Color.White, Color.Salmon, Color.DarkBlue, Color.Black, Color.White };

		public World(GameStateManager gameStateManager, GraphicsDevice device, int worldSize)
		{
			this.GameStateManager = gameStateManager;

			//TEMP start in night time
			//alive = DAY_CYCLE_TIME * 0.65f;

			this.sizeInCubes = worldSize;

			sizeInChunks = (int)((float)worldSize / Chunk.CHUNK_SIZE);

			if (!meshesLoaded)
				CreateMeshes(device);

			ChunkManager = new ChunkManager(device, sizeInChunks, sizeInCubes, this);

			ProjectileManager = new ProjectileManager(this, device);
			EntityManager = new EntityManager(this);
			LightManager = new LightManager(device);

			PassiveSpawnerManager = new PassiveSpawnerManager(EntityManager);

			PointsOfInterest = new List<PointOfInterest>();

			Main.CubeLitEffect.Parameters["WorldSize"].SetValue(new Vector3(worldSize));
			Main.CubeLitEffect.Parameters["CubeSize"].SetValue(new Vector3(Cube.CUBE_SCALE));
			Main.CubeUnlitEffect.Parameters["WorldSize"].SetValue(new Vector3(worldSize));
			Main.CubeUnlitEffect.Parameters["CubeSize"].SetValue(new Vector3(Cube.CUBE_SCALE));

			directionalLight = new DirectionalLight(device, Main.camera, Main.camera.Near, Main.camera.Far / 50f, 
				new float[] { 1f / 50f,  1f / 25f, 1f / 10f, 1f / 2f});
			directionalLight.WorldheightMap = Main.assetsManager.GetAsset<Texture2D>("sun_worldheight_map");
		}

		private void CreateMeshes(GraphicsDevice device)
        {
			meshWireframeUnscaled = MeshHelper.MakeCubeVertexPositionColor(device, Vector3.Zero, new Vector3(1), MeshHelper.CubeFace.ALL, Color.White, DrawHelper.WhitePixel);
			meshMiningCube = MeshHelper.MakeCubeVertexPositionColorTextureNormal(device, Vector3.Zero, Vector3.One * Cube.CUBE_SCALE, MeshHelper.CubeFace.ALL, Color.White, null);

			Vector3 min = Vector3.Zero;
			Vector3 max = new Vector3(DrawDistanceHoriz * 2 * (Chunk.CHUNK_SIZE * Cube.CUBE_SCALE));
			max.Y = 0;
			Vector3 a = new Vector3(min.X, min.Y, min.Z);
			Vector3 b = new Vector3(max.X, min.Y, min.Z);
			Vector3 c = new Vector3(max.X, min.Y, max.Z);
			Vector3 d = new Vector3(min.X, min.Y, max.Z);

			List<VertexCube> vertices = new List<VertexCube>();
			List<int> indices = new List<int>();

			indices.Add(0);
			indices.Add(1);
			indices.Add(3);
			indices.Add(1);
			indices.Add(2);
			indices.Add(3);

			vertices.Add(new VertexCube(a, Color.Black, Vector2.Zero, new Vector3(0, 1, 0)));
			vertices.Add(new VertexCube(b, Color.Black, Vector2.Zero, new Vector3(0, 1, 0)));
			vertices.Add(new VertexCube(c, Color.Black, Vector2.Zero, new Vector3(0, 1, 0)));
			vertices.Add(new VertexCube(d, Color.Black, Vector2.Zero, new Vector3(0, 1, 0)));

			max.Y = DrawDistanceHoriz * 2 * (Chunk.CHUNK_SIZE * Cube.CUBE_SCALE);
			Vector3 l_t_f = new Vector3(0, 0, 1);
			Vector3 r_t_f = new Vector3(1, 0, 1);
			Vector3 r_t_n = new Vector3(1, 0, 0);
			Vector3 l_t_n = new Vector3(0, 0, 0);

			Vector3 l_b_n = new Vector3(0, 1, 0);
			Vector3 r_b_n = new Vector3(1, 1, 0);
			Vector3 r_b_f = new Vector3(1, 1, 1);
			Vector3 l_b_f = new Vector3(0, 1, 1);

			int offset = vertices.Count;
			indices.Add(offset + 0);
			indices.Add(offset + 1);
			indices.Add(offset + 3);
			indices.Add(offset + 1);
			indices.Add(offset + 2);
			indices.Add(offset + 3);

			vertices.Add(new VertexCube(r_t_n, Color.White, new Vector2(1, 1), new Vector3(0, 0, 1)));
			vertices.Add(new VertexCube(l_t_n, Color.White, new Vector2(0, 1), new Vector3(0, 0, 1)));
			vertices.Add(new VertexCube(l_b_n, Color.White, new Vector2(0, 0), new Vector3(0, 0, 1)));
			vertices.Add(new VertexCube(r_b_n, Color.White, new Vector2(1, 0), new Vector3(0, 0, 1)));

			offset = vertices.Count;
			indices.Add(offset + 0);
			indices.Add(offset + 1);
			indices.Add(offset + 3);
			indices.Add(offset + 1);
			indices.Add(offset + 2);
			indices.Add(offset + 3);

			vertices.Add(new VertexCube(r_t_f, Color.White, new Vector2(1, 1), new Vector3(-1, 0, 0)));
			vertices.Add(new VertexCube(r_t_n, Color.White, new Vector2(0, 1), new Vector3(-1, 0, 0)));
			vertices.Add(new VertexCube(r_b_n, Color.White, new Vector2(0, 0), new Vector3(-1, 0, 0)));
			vertices.Add(new VertexCube(r_b_f, Color.White, new Vector2(1, 0), new Vector3(-1, 0, 0)));

			offset = vertices.Count;
			indices.Add(offset + 0);
			indices.Add(offset + 1);
			indices.Add(offset + 3);
			indices.Add(offset + 1);
			indices.Add(offset + 2);
			indices.Add(offset + 3);

			vertices.Add(new VertexCube(l_t_f, Color.White, new Vector2(1, 1), new Vector3(0, 0, -1)));
			vertices.Add(new VertexCube(r_t_f, Color.White, new Vector2(0, 1), new Vector3(0, 0, -1)));
			vertices.Add(new VertexCube(r_b_f, Color.White, new Vector2(0, 0), new Vector3(0, 0, -1)));
			vertices.Add(new VertexCube(l_b_f, Color.White, new Vector2(1, 0), new Vector3(0, 0, -1)));

			offset = vertices.Count;
			indices.Add(offset + 0);
			indices.Add(offset + 1);
			indices.Add(offset + 3);
			indices.Add(offset + 1);
			indices.Add(offset + 2);
			indices.Add(offset + 3);

			vertices.Add(new VertexCube(l_t_n, Color.White, new Vector2(1, 1), new Vector3(1, 0, 0)));
			vertices.Add(new VertexCube(l_t_f, Color.White, new Vector2(0, 1), new Vector3(1, 0, 0)));
			vertices.Add(new VertexCube(l_b_f, Color.White, new Vector2(0, 0), new Vector3(1, 0, 0)));
			vertices.Add(new VertexCube(l_b_n, Color.White, new Vector2(1, 0), new Vector3(1, 0, 0)));

			offset = vertices.Count;
			indices.Add(offset + 0);
			indices.Add(offset + 1);
			indices.Add(offset + 3);
			indices.Add(offset + 1);
			indices.Add(offset + 2);
			indices.Add(offset + 3);

			vertices.Add(new VertexCube(l_b_f, Color.White, new Vector2(1, 1), new Vector3(0, -1, 0)));
			vertices.Add(new VertexCube(r_b_f, Color.White, new Vector2(0, 1), new Vector3(0, -1, 0)));
			vertices.Add(new VertexCube(r_b_n, Color.White, new Vector2(0, 0), new Vector3(0, -1, 0)));
			vertices.Add(new VertexCube(l_b_n, Color.White, new Vector2(1, 0), new Vector3(0, -1, 0)));

			meshMaxDrawDistBottom = new SimpleMesh<VertexCube, int>(device, vertices, indices, DrawHelper.WhitePixel);
			nightSkybox = DrawHelper3D.MakeUVSphere(device, 1, true);

			List<VertexCube> sunVertices = new List<VertexCube>();
			List<int> sunIndices = new List<int>();

			sunIndices.Add(0);
			sunIndices.Add(1);
			sunIndices.Add(3);
			sunIndices.Add(1);
			sunIndices.Add(2);
			sunIndices.Add(3);

			Color sunColor = Color.Yellow;
			float sunVertDist = Cube.CUBE_SCALE * 4;

			if (LoadedFolderName == "coconut")
			{
				sunVertDist = Cube.CUBE_SCALE * 128;
				sunColor = Color.White;
			}

			sunVertices.Add(new VertexCube(new Vector3(-sunVertDist, -sunVertDist, 0), sunColor, new Vector2(0, 0), new Vector3(0, 0, -1)));
			sunVertices.Add(new VertexCube(new Vector3(-sunVertDist, sunVertDist, 0), sunColor, new Vector2(1, 0), new Vector3(0, 0, -1)));
			sunVertices.Add(new VertexCube(new Vector3(sunVertDist, sunVertDist, 0), sunColor, new Vector2(1, 1), new Vector3(0, 0, -1)));
			sunVertices.Add(new VertexCube(new Vector3(sunVertDist, -sunVertDist, 0), sunColor, new Vector2(0, 1), new Vector3(0, 0, -1)));

			meshSun = new SimpleMesh<VertexCube, int>(device, sunVertices, sunIndices);

			vertices = new List<VertexCube>();
			indices = new List<int>();

			offset = vertices.Count;
			indices.Add(offset + 3);
			indices.Add(offset + 1);
			indices.Add(offset + 0);
			indices.Add(offset + 3);
			indices.Add(offset + 2);
			indices.Add(offset + 1);

			vertices.Add(new VertexCube(new Vector3(-Cube.CUBE_SCALE, 0, -Cube.CUBE_SCALE), Color.White, new Vector2(1, 1), new Vector3(0, 1, 0)));
			vertices.Add(new VertexCube(new Vector3(-Cube.CUBE_SCALE, 0, Cube.CUBE_SCALE), Color.White, new Vector2(0, 1), new Vector3(0, 1, 0)));
			vertices.Add(new VertexCube(new Vector3(Cube.CUBE_SCALE, 0, Cube.CUBE_SCALE), Color.White, new Vector2(0, 0), new Vector3(0, 1, 0)));
			vertices.Add(new VertexCube(new Vector3(Cube.CUBE_SCALE, 0, -Cube.CUBE_SCALE), Color.White, new Vector2(1, 0), new Vector3(0, 1, 0)));

			meshLavaQuad = MeshHelper.MakeSimplerMesh(device, vertices, indices);

			meshesLoaded = true;
		}

		public void LoadWorld(GraphicsDevice device, string folderName)
		{
			//ChunkManager.Initialize(this);

			int spawnX = Main.random.Next(sizeInCubes / 2 - 4, sizeInCubes / 2 + 4);
			int spawnZ = Main.random.Next(sizeInCubes / 2 - 4, sizeInCubes / 2 + 4);

			CubePosition playerPos = CubePosition.FromWorldSpace(new Vector3(sizeInCubes * Cube.CUBE_SCALE / 2f, sizeInCubes * Cube.CUBE_SCALE, sizeInCubes * Cube.CUBE_SCALE / 2f));
			playerPos.X = spawnX;
			playerPos.Z = spawnZ;
			playerPos.Y = sizeInCubes;

			saver = new WorldSaver(ChunkManager, EntityManager, Main.SessionInformation);

			worldInfoIO = new WorldInfoIO();
			chunkIO = new ChunkManagerIO(ChunkManager, "test");
			entIO = new EntityManagerIO(EntityManager);

			if (!saver.DoesSaveExist(folderName))
			{
				ChunkManager.InitLayer(0);
				ChunkManager.InitLayer(1);
				//generate island layer
				ChunkManager.GenerateWorld(this, 0);
				ChunkManager.GenerateWorld(this, 1);

				worldInfoIO.Save(folderName, this, PointsOfInterest);

				Console.WriteLine("Saving Chunks...");
				Stopwatch watch = Stopwatch.StartNew();
				//saver.Save(folderName);
				chunkIO.SerializeAll();
				chunkIO.Save(folderName);

				watch.Stop();
				Console.WriteLine("Done. {0}s.", watch.Elapsed.TotalSeconds);

				ChunkLoadManager = new ChunkLoadManager(saver, ChunkManager, EntityManager, 6, 6, 8, chunkIO, entIO);

				player = new Player();
				EntityManager.Add(player);
				player.FirstCreated();

				Vector3 playerSpawnPosition = ChunkManager.GetPlayerSpawnPos(this);
				player.Position = playerSpawnPosition;
				player.SpawnPosition = CubePosition.FromWorldSpace(playerSpawnPosition);

				Console.WriteLine("Saving Entities...");
				watch = Stopwatch.StartNew();

				entIO.SerializeAll(this);
				entIO.Save(folderName);

				watch.Stop();
				Console.WriteLine("Done. {0}s.", watch.Elapsed.TotalSeconds);

				Console.WriteLine("Reloading...");
				watch = Stopwatch.StartNew();

				//This will unload everything, then reload only the things nearby.
				ChunkLoadManager.UnloadAll();
				ChunkLoadManager.UpdateLoadTarget(playerSpawnPosition);
				ChunkLoadManager.LoadAroundTarget(this);	//enqueue to be loaded...
				ChunkLoadManager.FlushLoadQueue(this);	//actually load.

                Console.WriteLine("Done. {0}s.", watch.Elapsed.TotalSeconds);

				//This includes the player, so this.player needs to be set again. (Kinda awkward, I know.)
				if (EntityManager.GetAll<Player>().Count > 0)
				{
					player = EntityManager.GetAll<Player>().First() as Player;

					Main.camera.Position = player.Position;
				}
			}
			else
			{
				WorldIO.LoadError error = worldInfoIO.Load(folderName, this, PointsOfInterest);
				if (error == WorldIO.LoadError.InvalidVersion)
					Console.WriteLine("World Info file could not be loaded. The current file version ({0}) is not supported.", worldInfoIO.Version);

				error = chunkIO.Load(folderName);
				if (error == WorldIO.LoadError.InvalidVersion)
					Console.WriteLine("Chunk file could not be loaded. The current file version ({0}) is not supported.", chunkIO.Version);

				error = entIO.Load(folderName);//saver.Load(device, this, folderName);
				if (error == WorldIO.LoadError.InvalidVersion)
					Console.WriteLine("Entity file could not be loaded. The current file version ({0}) is not supported.", entIO.Version);

				ChunkLoadManager = new ChunkLoadManager(saver, ChunkManager, EntityManager, 6, 6, 8, chunkIO, entIO);
				ChunkManager.InitLayer(0);
				ChunkManager.InitLayer(1);

				entIO.DeserializePlayerChunk();

				if (EntityManager.GetAll<Player>().Count > 0)
				{
					player = EntityManager.GetAll<Player>().First() as Player;

					ChunkLoadManager.UpdateLoadTarget(player.Position);
					ChunkLoadManager.LoadAroundTarget(this);

					Main.camera.Position = player.Position;
				}
				else
				{
					//Player somehow has not been created?
					player = new Player();
					EntityManager.Add(player);
					player.FirstCreated();

					ChunkLoadManager.UpdateLoadTarget(playerPos.InWorldSpace(null));
					ChunkLoadManager.LoadColumn(this);

					Vector3 playerSpawnPos = GetFirstSolidDown(playerPos.InWorldSpace(null)).InWorldSpace(null) + new Vector3(0, Cube.CUBE_SCALE * 3, 0);
					player.SpawnPosition = CubePosition.FromWorldSpace(playerSpawnPos);
					player.Position = playerSpawnPos;

					Main.camera.Position = player.Position;
				}
			}

			Main.FogManager.Set(1300f, 1700f, Main.assetsManager.GetAsset<Texture2D>("heightmap_layer1_day"), Main.assetsManager.GetAsset<Texture2D>("heightmap_layer1_night"), 0);
			//ChunkLoadManager2 = new ChunkLoadManager(saver2, ChunkManager2, 6, 6, 8);

			//EntityManager.Add(new EntityLeviathan());
			LoadedFolderName = folderName;
			Main.SessionInformation.LastLoadedSave = LoadedFolderName;
		}

		public void UnfixedUpdate()
		{
		}

		private float alive;

		public void Update(double deltaTime)
		{
			ChunkLoadManager.UpdateLoadTarget(player.Position);

			alive += (float)deltaTime;

			ChunkManager.ProcessChunkQueue(this, 0);
			ChunkLoadManager.Update(deltaTime, this);

			ProjectileManager.Update(deltaTime);
			EntityManager.Update(deltaTime);

			if (player.Position.Y / Cube.CUBE_SCALE < 140)
			{
				Vector3 lavaPosition = new Vector3(player.Position.X, Cube.CUBE_SCALE * 40.5f, player.Position.Z);

				if (player.Position.Y < lavaPosition.Y)
					player.Kill();

				if (lavaLight == -1)
					lavaLight = LightManager.Add(lavaPosition, 32 * Cube.CUBE_SCALE, 32 * Cube.CUBE_SCALE, Color.OrangeRed);
				else LightManager.Update(lavaLight, lavaPosition, 32 * Cube.CUBE_SCALE, 32 * Cube.CUBE_SCALE, Color.OrangeRed);
			}
			else
			{
				if (lavaLight != -1)
				{
					LightManager.Remove(lavaLight);
					lavaLight = -1;
				}
			}

			foreach (var mined in miningCubes)
			{
				MinedCube mc = mined.Value;

				if (mc.chunk != null && mc.chunk.Initialized)
				{
					Cube cube = mc.chunk.GetData().GetCube(mc.position).GetOrDefault(Main.Registry.CubeRegistry.Air);

					mc.timer -= (float)deltaTime;
					if (mc.timer <= 0)
					{
						mc.progress--;
						mc.timer = 2;
					}

					if (mc.progress <= 0 || cube == Main.Registry.CubeRegistry.Air)
						miningRemove.Add(mc.position);
					else miningUpdate.Add(mc);
				}
				else
					miningRemove.Add(mc.position);
			}

			foreach (var pos in miningRemove)
			{
				miningCubes.Remove(pos);
			}

			foreach (var mc in miningUpdate)
			{
				miningCubes[mc.position] = mc;
			}

			miningRemove.Clear();
			miningUpdate.Clear();

			/*foreach (ChunkPosition loadedPosition in ChunkLoadManager.GetLoadedChunks())
			{
				Chunk chunk = ChunkManager.GetChunk(loadedPosition);

				if (chunk != null && chunk.Initialized)
				{
					int num = Main.random.Next(0, Chunk.NUM_CUBES_IN_CHUNK);
					int id = chunk.GetData().GetAll()[num];

					Cube cube = Main.Registry.CubeRegistry.Get(id);

					if (cube != null)
					{
						Util.OneDToThreeD(num, new ValuePoint3D(Chunk.CHUNK_SIZE, Chunk.CHUNK_SIZE, Chunk.CHUNK_SIZE), out ValuePoint3D point3d);
						cube.OnRandomUpdate(this, ChunkManager, new CubePosition(point3d.x, point3d.y, point3d.z, CubePosition.CoordinateSpace.ChunkSpace).InCubeSpace(chunk));
					}
				}
			}*/

			PassiveSpawnerManager.Update(deltaTime, this);

			ChunkPosition camPos = ChunkPosition.WorldSpaceChunk(Main.camera.Position);

			if (chunkDrawPositionsDirty || Main.camera.IsDirty)
			{
				CulledChunkDrawPositions.Clear();

				for (int x = Math.Max(0, camPos.X - DrawDistanceHoriz); x <= Math.Min(sizeInChunks, camPos.X + DrawDistanceHoriz); x++)
				{
					for (int y = Math.Max(-ChunkManager.layerSizeInChunksY * (ChunkManager.DiscoveredLayers - 1), camPos.Y - DrawDistanceVert); y <= Math.Min(sizeInChunks, camPos.Y + DrawDistanceVert); y++)
					{
						for (int z = Math.Max(0, camPos.Z - DrawDistanceHoriz); z <= Math.Min(sizeInChunks, camPos.Z + DrawDistanceHoriz); z++)
						{
							ChunkPosition chunkPos = new ChunkPosition(x, y, z);

							int length = (int)(new Vector3(chunkPos.X, chunkPos.Y, chunkPos.Z) - new Vector3(camPos.X, camPos.Y, camPos.Z)).Length();

							if (ChunkManager.IsInWorldBounds(chunkPos) && length < DrawRadius && 
								Main.camera.FrustumIntersects(new Rectangle3D(chunkPos.InWorldSpace(), new Vector3(Chunk.CHUNK_SIZE * Cube.CUBE_SCALE))))
							{
								CulledChunkDrawPositions.Add(chunkPos);
							}
						}
					}
				}

				chunkDrawPositionsDirty = false;
			}

			oldCameraRotation = Main.camera.Rotation;
			oldChunkPosition = camPos;

			//below this point, don't even bother updating the directional light as we can't see any of it anyway. It should have no contribution to the scene.
			if (CubePosition.FromWorldSpace(player.Position).Y > 140)
			{
				Main.Renderer.DoCSMLight = true;

				if ((int)((alive * 60f) % 5f) == 0 || Main.camera.IsDirty)
				{
					Color color = Color.White * (1 - GetTimeOfDay());

					if (GetDuskTime() > 0)
					{
						duskColors[0] = color;  //so that we don't snap to the wrong color...
						duskColors[^1] = color;
						color = Utility.MultiLerp(GetDuskTime(), Color.Lerp, duskColors);
					}

					float angle = 360 * ((alive % DAY_CYCLE_TIME) / DAY_CYCLE_TIME);
					directionalLight.UpdateCameras(Vector3.Transform(new Vector3(0, 0, SUN_LIGHT_DISTANCE),
						Matrix.CreateRotationX(MathHelper.ToRadians(angle)) *
						Matrix.CreateRotationY(MathHelper.ToRadians(SUN_ANGLE))), color);

					float ambient = 1 - GetTimeOfDay(dawnEndOffsetScale: 1.25f);
					//Main.CubeLitEffect.Parameters["AmbientStrength"].SetValue(ambient);
					Main.Renderer.EffectGBuffer.Parameters["AmbientStrength"].SetValue(ambient);
					if (!Main.inputManager.IsHeld(Keys.F6))
						Main.Renderer.EffectGBuffer.Parameters["WorldheightMapAmb"].SetValue(Main.assetsManager.GetAsset<Texture2D>("sun_worldheight_map"));
					else Main.Renderer.EffectGBuffer.Parameters["WorldheightMapAmb"].SetValue(DrawHelper.WhitePixel);
					//Main.Renderer.EffectGBuffer.Parameters["Heightmap"].SetValue(ChunkManager.Heightmap);
					Main.Renderer.EffectTransparent.Parameters["AmbientStrength"].SetValue(ambient);
					Main.Renderer.EffectTransparent.Parameters["WorldheightMapAmb"].SetValue(Main.assetsManager.GetAsset<Texture2D>("sun_worldheight_map"));
				}
			}
			else
			{
				Main.Renderer.DoCSMLight = false;

				if (Main.inputManager.IsHeld(Keys.F6)) 
					Main.Renderer.EffectGBuffer.Parameters["WorldheightMapAmb"].SetValue(DrawHelper.WhitePixel);
			}
		}

		public void SaveWorld()
        {
			//TODO open pause GUI. This maybe should be done in Main.cs instead?
			//Flush the load queue so we don't end up not saving chunks that are currently loading in.
			//This is probably unnecessary (why would data in newly loaded chunks change ever?) but it's best to be on the safe side.
			ChunkLoadManager.FlushLoadQueue(this);
			//Serialize all the chunks that are currently loaded
			chunkIO.Serialize(ChunkLoadManager.GetLoadedChunks());
			entIO.Serialize(ChunkLoadManager.GetLoadedChunks());

			//Save serialized data to disk
			chunkIO.Save(LoadedFolderName);
			entIO.Save(LoadedFolderName);

			//Deduplicate/decache serialized entity data
			entIO.DecacheCurrentlySerialized();
		}

		//Gets a list of all chunks that should be rendered by the main camera.
		public List<ChunkPosition> GetChunkDrawPositions()
        {
			return CulledChunkDrawPositions;
        }

		public static int NumChunksDrawn;
		public static double ChunkDrawTime;

		public void Draw(GraphicsDevice device, Effect effect)
		{
			NumChunksDrawn = 0;
			ChunkDrawTime = 0;

			Stopwatch drawTime = Stopwatch.StartNew();

			directionalLight.DrawShadowmap(device, this);
			directionalLight.Bind(Main.Renderer.EffectLightAccumCSM);

			LightManager.UpdateDatas(Main.Renderer.EffectLightAccumPointLight);
			LightManager.DrawShadowmap(device, this);
			LightManager.Draw(device);

			bool drawSkybox = true;

			float dist = DrawDistanceHoriz * Chunk.CHUNK_SIZE * Cube.CUBE_SCALE - (16 * Cube.CUBE_SCALE);
			Vector3 camChunkPosWS = Main.camera.Position;
			if (camChunkPosWS.Y < Cube.CUBE_SCALE * 100)
				dist = MathHelper.Lerp(64 * Cube.CUBE_SCALE, dist, camChunkPosWS.Y / (Cube.CUBE_SCALE * 100));
			else if (camChunkPosWS.Y < -200f)
				dist = 64 * Cube.CUBE_SCALE;

			camChunkPosWS.X -= DrawDistanceHoriz * Chunk.CHUNK_SIZE * Cube.CUBE_SCALE;
			camChunkPosWS.Y -= dist;
			camChunkPosWS.Z -= DrawDistanceHoriz * Chunk.CHUNK_SIZE * Cube.CUBE_SCALE;

			foreach (ChunkPosition pos in CulledChunkDrawPositions)
			{
				Matrix transform = ChunkManager.GetTransform(pos);

				Texture2D emissiveTexture = Main.assetsManager.GetAsset<Texture2D>("cubes_textures_emissive");
				if (player.GetBuffManager().HasBuff("emissive_ores"))
					emissiveTexture = Main.assetsManager.GetAsset<Texture2D>("cubes_textures_emissive_ores");

                ChunkMesh mesh = ChunkManager.GetMesh(pos, Cubes.Cube.RenderPass.Opaque);
                if (mesh != null && mesh != ChunkMesh.Empty)
                {
                    Main.Renderer.DrawsPassGBuffer.Add(new Rendering.RendererDeferred.GBufferDraw(Main.assetsManager.GetAsset<Texture2D>("cubes_textures"),
                        DrawHelper.BlackPixel, emissiveTexture, mesh.VBO, mesh.IBO,
                        transform, null));
                }

                mesh = ChunkManager.GetMesh(pos, Cubes.Cube.RenderPass.Transparent);
                if (mesh != null && mesh != ChunkMesh.Empty)
                {
                    Vector3 minBounds = Main.camera.Position - pos.InWorldSpace();
                    Vector3 maxBounds = Main.camera.Position - minBounds + new Vector3(Chunk.CHUNK_SIZE * Cube.CUBE_SCALE);

                    Vector3 min = new Vector3(Math.Min(minBounds.X, maxBounds.X), Math.Min(minBounds.Y, maxBounds.Y), Math.Min(minBounds.Z, maxBounds.Z));
                    //Vector3 max = new Vector3(Math.Max(minBounds.X, maxBounds.X), Math.Max(minBounds.Y, maxBounds.Y), Math.Max(minBounds.Z, maxBounds.Z));

                    Main.Renderer.DrawsTransparentPass.Add(new Rendering.RendererDeferred.TransparentDraw((int)min.Length(), transform,
                        Main.assetsManager.GetAsset<Texture2D>("cubes_textures"),
                        emissiveTexture,
                        mesh.VBO, mesh.IBO, null, null));
                }

				if (Main.Renderer.EffectEmptyEnabled)
				{
					mesh = ChunkManager.GetMesh(pos, Cubes.Cube.RenderPass.Air);
					if (mesh != null && mesh != ChunkMesh.Empty)
					{
						Vector3 minBounds = Main.camera.Position - pos.InWorldSpace();
						Vector3 maxBounds = Main.camera.Position - minBounds + new Vector3(Chunk.CHUNK_SIZE * Cube.CUBE_SCALE);

						Vector3 min = new Vector3(Math.Min(minBounds.X, maxBounds.X), Math.Min(minBounds.Y, maxBounds.Y), Math.Min(minBounds.Z, maxBounds.Z));

						Main.Renderer.DrawsEmptyPass.Add(new Rendering.RendererDeferred.TransparentDraw((int)min.Length(), transform,
							Main.assetsManager.GetAsset<Texture2D>("cubes_textures"),
							DrawHelper.WhitePixel,
							mesh.VBO, mesh.IBO));
					}
				}

				NumChunksDrawn++;

				if (Main.Debug && Main.DebugChunks)
				{
					ChunkManager.GetChunk(pos).DrawDebug(device);

					device.RasterizerState = Main.genericRS;
					device.DepthStencilState = Main.genericDSS;
				}
			}

			if (drawSkybox)
			{
				float angle = 360 * ((alive % DAY_CYCLE_TIME) / DAY_CYCLE_TIME);

				float alphaDay = 1 - GetTimeOfDay();
				float alphaNight = GetTimeOfNight();

				if (alphaDay > 0)
				{
					Main.Renderer.DrawsTransparentPass.Add(new Rendering.RendererDeferred.TransparentDraw(1000,
						Matrix.CreateTranslation(new Vector3(-0.5f)) *
						Matrix.CreateScale(DrawDistanceHoriz * 2 * (Chunk.CHUNK_SIZE * Cube.CUBE_SCALE)) *
						Matrix.CreateTranslation(Main.camera.Position),
						Main.assetsManager.GetAsset<Texture2D>("skybox_day"), DrawHelper.BlackPixel,
						meshMaxDrawDistBottom.VBO, meshMaxDrawDistBottom.IBO, null, Color.White * alphaDay));
				}

				if (alphaDay < 1)
				{
					const float mp = (DAY_CYCLE_TIME * 1.5f);
					const float my = (DAY_CYCLE_TIME * 1.34f);
					float p = MathF.Sin(MathF.PI * 2 * ((alive % mp) / mp));
					float y = MathF.Sin(MathF.PI * 2 * ((alive % my) / my));

					Main.Renderer.DrawsTransparentPass.Add(new Rendering.RendererDeferred.TransparentDraw(1001,
						Matrix.CreateTranslation(new Vector3(-0.5f)) *
						Matrix.CreateScale(DrawDistanceHoriz * 1.95f * (Chunk.CHUNK_SIZE * Cube.CUBE_SCALE)) *
						Matrix.CreateFromYawPitchRoll(y, p, 0) *
						Matrix.CreateTranslation(Main.camera.Position),
						Main.assetsManager.GetAsset<Texture2D>("skybox_night"), DrawHelper.WhitePixel,
						meshMaxDrawDistBottom.VBO, meshMaxDrawDistBottom.IBO, null, Color.White));
				}

				Texture2D sunTexture = DrawHelper.WhitePixel;

				if (LoadedFolderName == "coconut")
					sunTexture = Main.assetsManager.GetAsset<Texture2D>("coconut");

				Main.Renderer.DrawsPassGBuffer.Add(new Rendering.RendererDeferred.GBufferDraw(sunTexture,
					DrawHelper.BlackPixel, DrawHelper.WhitePixel, meshSun.VBO, meshSun.IBO,
					Matrix.CreateTranslation(new Vector3(0, 0, SUN_DISTANCE)) *
					Matrix.CreateRotationX(MathHelper.ToRadians(angle)) *
					//Matrix.CreateRotationY(MathHelper.ToRadians(SUN_ANGLE)) *
					Matrix.CreateTranslation(player.Position), null));

				if (Main.Debug)
					HitboxManager.DrawDebug(device);
				/*Main.CubeLitEffect.Parameters["TintColor"].SetValue(Color.White.ToVector3());
				Main.FogManager.Disable();

				Main.FogManager.Enable();*/
			}

			foreach (var mined in miningCubes)
			{
				Cube cube = mined.Value.chunk.GetData().GetCube(mined.Value.position).GetOrDefault(Main.Registry.CubeRegistry.Air);

				if (cube != Main.Registry.CubeRegistry.Air)
				{
					float percent = (float)mined.Value.progress / (float)cube.MineProgressToBreak;

					float stepped = ((int)(percent * 8f)) / 8f;

					RectangleF sourceRect = new RectangleF(128f * stepped, 0, 16, 16);

					Main.Renderer.DrawsPassGBuffer.Add(new Rendering.RendererDeferred.GBufferDraw(Main.assetsManager.GetAsset<Texture2D>("mine"),
						DrawHelper.BlackPixel, DrawHelper.BlackPixel, meshMiningCube.VBO, meshMiningCube.IBO,
						Matrix.CreateTranslation(mined.Value.position.InWorldSpace(mined.Value.chunk)), sourceRect));
				}
			}

			if (player.Position.Y / Cube.CUBE_SCALE < 140)
			{
				Matrix mat = Matrix.CreateScale(Cube.CUBE_SCALE * 512, 1, Cube.CUBE_SCALE * 512) *
					Matrix.CreateTranslation(player.Position.X, Cube.CUBE_SCALE * 40.5f, player.Position.Z);

				Main.Renderer.DrawsPassGBuffer.Add(new Rendering.RendererDeferred.GBufferDraw(Main.assetsManager.GetAsset<Texture2D>("lava"),
					DrawHelper.BlackPixel, DrawHelper.WhitePixel, meshLavaQuad.VBO, meshLavaQuad.IBO, mat));
			}

			ProjectileManager.Draw(device, effect);
			EntityManager.Draw(device, Main.CubeLitEffect);

			drawTime.Stop();
			ChunkDrawTime = drawTime.Elapsed.TotalSeconds;

		}

		public void DrawShadowmap(GraphicsDevice device, Effect effect)
        {
		}

		public void DrawUI(SpriteBatch batch)
		{
			player.DrawUI(batch);
		}

		public ChunkManager GetChunkManager()
		{
			return ChunkManager;
		}

		public void OnCubeUpdate(ChunkData updatingParent, CubePosition updating, int updatedId)
		{
			foreach (Entity entity in EntityManager.GetEntities())
			{
				//if (updatingParent.IsInChunkBounds(entity.Position))
					entity.OnCubeUpdated(updatingParent, updating, updatedId);
			}
		}

        #region Time
        public float GetTime()
        {
			return alive;
        }

		public void SetTime(float time)
		{
			alive = time;
		}

		public void AddTime(float time)
        {
			alive += time;
        }

		private float GetTimeOfDay(float dawnStartOffsetScale = 1f, float dawnEndOffsetScale = 1f, float duskStartOffsetScale = 1, float duskEndOffsetScale = 1)
		{
			//values here are in % of day cycle time;
			//dawn starts at the last 8% of the total cycle
			const float DAWN_START = 0.92f;
			//dawn ends after 16% of the total cycle (8% of the day cycle)
			const float DAWN_END = 0.16f;

			//Dusk starts at the last 8% of the day cycle.
			const float DUSK_START = 0.42f;
			//dusk ends after 16% of the night cycle.
			const float DUSK_END = 0.66f;

			float dawnStart = 1 - ((1 - DAWN_START) * dawnStartOffsetScale);
			float dawnEnd = DAWN_END * dawnEndOffsetScale;
			float duskStart = 0.5f - ((1 - DUSK_START - 0.5f) * duskStartOffsetScale);
			float duskEnd = ((DUSK_END - 0.5f) * duskEndOffsetScale) + 0.5f;

			float timeOfDayPercent = (alive % DAY_CYCLE_TIME) / DAY_CYCLE_TIME;

			//Night time
			if (timeOfDayPercent > duskEnd && timeOfDayPercent <= dawnStart)
				return 1;
            else if (timeOfDayPercent > duskStart && timeOfDayPercent <= duskEnd)
					return (timeOfDayPercent - duskStart) / (duskEnd - duskStart);
			else if ((timeOfDayPercent > dawnStart && timeOfDayPercent <= 1) || (timeOfDayPercent >= 0 && timeOfDayPercent <= dawnEnd))
            {
				float percent = 0;
				if (timeOfDayPercent > dawnStart)
					percent = (timeOfDayPercent - dawnStart) / ((timeOfDayPercent + dawnEnd) - dawnStart);
				else if (timeOfDayPercent <= dawnEnd)
					percent = (timeOfDayPercent + (1 - dawnStart)) / (dawnEnd + (1 - dawnStart));

				percent = MathHelper.Clamp(percent, 0, 1);

				return 1 - percent;
			}

			return 0;
		}

		public float GetDawnTime()
        {
			//dawn starts at the last 8% of the total cycle
			const float DAWN_START = 0.92f;
			//dawn ends after 16% of the total cycle (8% of the day cycle)
			const float DAWN_END = 0.16f;

			return 0;
		}

		public float GetDuskTime()
        {
			//Dusk starts at the last 8% of the day cycle.
			const float DUSK_START = 0.42f;
			//dusk ends after 16% of the night cycle.
			const float DUSK_END = 0.66f;

			float timeOfDayPercent = (alive % DAY_CYCLE_TIME) / DAY_CYCLE_TIME;

			if (timeOfDayPercent > DUSK_START && timeOfDayPercent <= DUSK_END)
				return (timeOfDayPercent - DUSK_START) / (DUSK_END - DUSK_START);

			return 0;
		}

		public bool IsDay()
        {
			return (alive % DAY_CYCLE_TIME) <= DAY_CYCLE_TIME / 2f;
        }

		public bool IsNight()
        {
			return (alive % DAY_CYCLE_TIME) > DAY_CYCLE_TIME / 2f;
        }

		public float GetTimeOfNight()
        {
			float timeOfDay = alive % DAY_CYCLE_TIME;

			if (!IsNight())
				return 0;
            else
            {
				float nightTime = timeOfDay - (DAY_CYCLE_TIME / 2f);

				float midnightTime = DAY_CYCLE_TIME * 0.25f;

				if (nightTime < midnightTime)
                {
					const float START = DAY_CYCLE_TIME / 2f;
					const float END = DAY_CYCLE_TIME * 0.75f;

					return (timeOfDay - START) / (END - START);
				}
                else
                {
					const float START = DAY_CYCLE_TIME * 0.75f;
					const float END = DAY_CYCLE_TIME;

					return 1 - ((timeOfDay - START) / (END - START));
                }

				//return nightTime / (DAY_CYCLE_TIME / 2f);
            }
        }
        #endregion

        public bool TryMineCube(CubePosition position, int level, int num, bool instant = false)
		{
			Chunk chunk = ChunkManager.GetChunk(position);

			MinedCube mined = new MinedCube()
			{
				position = position,
				chunk = chunk,
				progress = num,
				timer = 2
			};

			Cube cube = Main.Registry.CubeRegistry.Get(ChunkManager.GetRaw(position));

			if (cube != null && (level >= cube.MineLevelRequirement || instant))
			{
				if (instant)
				{
					mined.chunk.GetData().SetCube(position, 0);

					List<ItemInstance> items = new List<ItemInstance>();
					cube.GetDrops(items);

					foreach (ItemInstance item in items)
					{
						EntityItem ent = new EntityItem(position.InWorldSpace(null) + new Vector3(Cube.CUBE_SCALE / 2f), item);
						ent.Velocity = new Vector3(Main.random.NextFloat(-Cube.CUBE_SCALE * 2, Cube.CUBE_SCALE * 2), Cube.CUBE_SCALE, Main.random.NextFloat(-Cube.CUBE_SCALE * 2, Cube.CUBE_SCALE * 2));
						EntityManager.Add(ent);
					}

					cube.OnMined(player, position);

					return true;
				}

				if (miningCubes.ContainsKey(position))
				{
					mined.progress = miningCubes[position].progress + num;
					if (mined.progress >= cube.MineProgressToBreak)
					{
						miningCubes.Remove(position);
						mined.chunk.GetData().SetCube(position, 0);

						List<ItemInstance> items = new List<ItemInstance>();
						cube.GetDrops(items);

						foreach (ItemInstance item in items)
						{
							EntityItem ent = new EntityItem(position.InWorldSpace(null) + new Vector3(Cube.CUBE_SCALE / 2f), item);
							ent.Velocity = new Vector3(Main.random.NextFloat(-Cube.CUBE_SCALE * 2, Cube.CUBE_SCALE * 2), Cube.CUBE_SCALE, Main.random.NextFloat(-Cube.CUBE_SCALE * 2, Cube.CUBE_SCALE * 2));
							EntityManager.Add(ent);
						}

						cube.OnMined(player, position);

						return true;
					}
					else miningCubes[position] = mined;
				}
				else
				{
					if (mined.progress < cube.MineProgressToBreak)
						miningCubes.Add(position, mined);
					else
					{
						mined.chunk.GetData().SetCube(position, 0);

						List<ItemInstance> items = new List<ItemInstance>();
						cube.GetDrops(items);

						foreach (ItemInstance item in items)
						{
							EntityItem ent = new EntityItem(position.InWorldSpace(null) + new Vector3(Cube.CUBE_SCALE / 2f), item);
							ent.Velocity = new Vector3(Main.random.NextFloat(-Cube.CUBE_SCALE * 2, Cube.CUBE_SCALE * 2), Cube.CUBE_SCALE, Main.random.NextFloat(-Cube.CUBE_SCALE * 2, Cube.CUBE_SCALE * 2));
							EntityManager.Add(ent);
						}

						cube.OnMined(player, position);

						return true;
					} 
				}
			}

			return false;
		}

		public CubePosition GetFirstSolidDown(Vector3 start)
		{
			CubePosition startPos = CubePosition.FromWorldSpace(start);

			for (int y = 0; y < sizeInCubes; y++)
			{
				CubePosition pos = new CubePosition(startPos.X, startPos.Y - y, startPos.Z);
				if (ChunkManager.IsInWorldBounds(pos) && ChunkManager.GetRaw(pos) != 0)
					return pos;
			}

			return new CubePosition(-1, -1, -1);
		}

		public List<CubePosition> GetAdjacentsInWorld(CubePosition position)
		{
			CubePosition down = new CubePosition(position.X, position.Y - 1, position.Z);
			CubePosition up = new CubePosition(position.X, position.Y + 1, position.Z);
			CubePosition left = new CubePosition(position.X - 1, position.Y, position.Z);
			CubePosition right = new CubePosition(position.X + 1, position.Y, position.Z);
			CubePosition front = new CubePosition(position.X, position.Y, position.Z - 1);
			CubePosition back = new CubePosition(position.X, position.Y, position.Z + 1);

			List<CubePosition> positions = new List<CubePosition>();

			if (ChunkManager.IsInWorldBounds(down))
				positions.Add(down);
			if (ChunkManager.IsInWorldBounds(up))
				positions.Add(up);
			if (ChunkManager.IsInWorldBounds(left))
				positions.Add(left);
			if (ChunkManager.IsInWorldBounds(right))
				positions.Add(right);
			if (ChunkManager.IsInWorldBounds(front))
				positions.Add(front);
			if (ChunkManager.IsInWorldBounds(back))
				positions.Add(back);

			return positions;
		}

		public MeshHelper.CubeFace GetClearSides(CubePosition position)
		{
			if (position.Coord == CubePosition.CoordinateSpace.ChunkSpace)
				position = position.InCubeSpace(ChunkManager.GetChunk(position));

			if (ChunkManager.GetRaw(position) == 0)
				return MeshHelper.CubeFace.ALL;

			MeshHelper.CubeFace faces = MeshHelper.CubeFace.NONE;

			if (position.X == 0 || ChunkManager.GetCubeInstance(position.X - 1, position.Y, position.Z).cubeId == 0)
				faces |= MeshHelper.CubeFace.LEFT;
			if (position.X == sizeInCubes - 1 || ChunkManager.GetCubeInstance(position.X + 1, position.Y, position.Z).cubeId == 0)
				faces |= MeshHelper.CubeFace.RIGHT;

			if (position.Y == 0 || ChunkManager.GetCubeInstance(position.X, position.Y - 1, position.Z).cubeId == 0)
				faces |= MeshHelper.CubeFace.DOWN;
			if (position.Y == sizeInCubes - 1 || ChunkManager.GetCubeInstance(position.X, position.Y + 1, position.Z).cubeId == 0)
				faces |= MeshHelper.CubeFace.UP;

			if (position.Z == 0 || ChunkManager.GetCubeInstance(position.X, position.Y, position.Z - 1).cubeId == 0)
				faces |= MeshHelper.CubeFace.FRONT;
			if (position.Z == sizeInCubes - 1 || ChunkManager.GetCubeInstance(position.X, position.Y, position.Z + 1).cubeId == 0)
				faces |= MeshHelper.CubeFace.BACK;

			return faces;
		}

		public struct RaycastResult 
		{
			public Vector3 start;
			public Vector3 end;
			public Vector3 hit; //== end if hasHit == false
			public Vector3 normal;
			public bool hasHit;
		}

		public RaycastResult Raycast(Vector3 start, Vector3 end, Func<Vector3, bool> callback)
		{
			if (float.IsNaN(end.X) || float.IsNaN(end.Y) || float.IsNaN(end.Z))
				return new RaycastResult();

			RaycastResult result = new RaycastResult();

			const float ONE_CUBE = Cube.CUBE_SCALE;

			result.start = start;
			result.end = end;

			float x1 = start.X / ONE_CUBE;
			float y1 = start.Y / ONE_CUBE;
			float z1 = start.Z / ONE_CUBE;
			float x2 = end.X / ONE_CUBE;
			float y2 = end.Y / ONE_CUBE;
			float z2 = end.Z / ONE_CUBE;

			int i = (int)x1;
			int j = (int)y1;
			int k = (int)z1;

			int iend = (int)x2;
			int jend = (int)y2;
			int kend = (int)z2;

			int di = ((x1 < x2) ? 1 : ((x1 > x2) ? -1 : 0));
			int dj = ((y1 < y2) ? 1 : ((y1 > y2) ? -1 : 0));
			int dk = ((z1 < z2) ? 1 : ((z1 > z2) ? -1 : 0));

			float deltatx = 1.0f / Math.Abs(x2 - x1);
			float deltaty = 1.0f / Math.Abs(y2 - y1);
			float deltatz = 1.0f / Math.Abs(z2 - z1);

			float minx = (int)x1, maxx = minx + 1;
			float tx = ((x1 > x2) ? (x1 - minx) : (maxx - x1)) * deltatx;
			float miny = (int)y1, maxy = miny + 1;
			float ty = ((y1 > y2) ? (y1 - miny) : (maxy - y1)) * deltaty;
			float minz = (int)z1, maxz = minz + 1;
			float tz = ((z1 > z2) ? (z1 - minz) : (maxz - z1)) * deltatz;

			Vector3 hitPos = new Vector3(x1 * ONE_CUBE, y1 * ONE_CUBE, z1 * ONE_CUBE);

			while (true)
			{
				if (callback(hitPos))
				{
					result.hasHit = true;
					result.hit = hitPos;
					return result;
				}

				if (tx <= ty && tx <= tz)
				{
					if (i == iend)
					{
						result.hit = result.end;
						break;
					}
					tx += deltatx;
					i += di;

					if (di == 1) hitPos.X += ONE_CUBE;
					if (di == -1) hitPos.X -= ONE_CUBE;

					result.normal = new Vector3(-di, 0, 0);
				}
				else if (ty <= tz)
				{
					if (j == jend)
					{
						result.hit = result.end;
						break;
					}
					ty += deltaty;
					j += dj;

					if (dj == 1) hitPos.Y += ONE_CUBE;
					if (dj == -1) hitPos.Y -= ONE_CUBE;

					result.normal = new Vector3(0, -dj, 0);
				}
				else
				{
					if (k == kend)
					{
						result.hit = result.end;
						break;
					}
					tz += deltatz;
					k += dk;

					if (dk == 1) hitPos.Z += ONE_CUBE;
					if (dk == -1) hitPos.Z -= ONE_CUBE;

					result.normal = new Vector3(0, 0, -dk);
				}
			}

			return result;
		}

		public RaycastResult RaycastVector(Vector3 start, Vector3 direction, float distance, Func<Vector3, bool> callback)
		{
			if (float.IsNaN(direction.X) || float.IsNaN(direction.Y) || float.IsNaN(direction.Z))
				return new RaycastResult();

			return Raycast(start, start + Vector3.Normalize(direction) * distance, callback);
		}
	}
}
