using BepuPhysics;
using BepuPhysics.Constraints;
using BepuUtilities.Memory;
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
using ViMG.Physics;
using ViMG.Spawners;
using ViMG.UIs;
using ViMG.WorldLogics;

namespace ViMG
{
	public class World
	{
		public readonly string LoadedFolderName;
		public readonly int Layer;

		public const float GRAVITY = -9.8f / 20f * Cube.CUBE_SCALE;
		public const float DAY_CYCLE_TIME = 60f * 10f;

		public readonly int sizeInChunks;
		public readonly int sizeInCubes;

		public ChunkManager ChunkManager;
		public ChunkGenerator ChunkGenerator;

		private static SimpleMesh<VertexPositionColor, int> meshWireframeCube;
		private static SimpleMesh<VertexPositionColor, int> meshWireframeUnscaled;
		private static SimpleMesh<VertexCube, int> meshMiningCube;

		private static SimpleMesh<VertexCube, int> skyboxMesh;
		private static bool meshesLoaded;
		public Skybox Skybox;

		public GameStateManager GameStateManager;
		public Player player;

		public int DrawDistanceHoriz = 6;   //radius in chunks that we should be able to see
		public int DrawDistanceVert = 6;
		public int DrawRadius = 6;

		public HitboxManager HitboxManager = new HitboxManager(32);
		public ProjectileManager ProjectileManager;
		public EntityManager EntityManager;
		public LightManager LightManager;
		public PassiveSpawnerManager PassiveSpawnerManager;
		public WorldInfoIO.WorldInfo WorldInfo;
		public ChunkLoadManager ChunkLoadManager;
		public PhysicsInfo PhysicsInfo;
		public ChatManager ChatManager;

		private WorldInfoIO worldInfoIO;
		private ChunkManagerIO chunkIO;
		private EntityManagerIO entIO;
		private WorldLogic logic;

		public HousingManager HousingManager;

		public Color SkyColor = new Color(94, 107, 154);

		public List<ChunkPosition> CulledChunkDrawPositions = new List<ChunkPosition>();
		private bool chunkDrawPositionsDirty = true;
		private ChunkPosition oldChunkPosition;
		private Vector3 oldCameraRotation;

		private struct MinedCube
		{
			public CubePosition position;
			public ChunkPosition chunk;
			public float timer;
			public int progress;	//goes up one per "mine"
		}

		private Dictionary<CubePosition, MinedCube> miningCubes = new Dictionary<CubePosition, MinedCube>();
		private List<CubePosition> miningRemove = new List<CubePosition>();
		private List<MinedCube> miningUpdate = new List<MinedCube>();

		private float randomUpdatesTimer;

		private int nextLayer;
		private Task<World> nextWorld;

		public World(GameStateManager gameStateManager, WorldPrototype prototype, ChunkLoadManager chunkLoadManager, 
			WorldInfoIO winfoIO, EntityManagerIO entityIO, ChunkManagerIO chunkIO, GraphicsDevice device, int worldSize)
		{
			this.Layer = prototype.Layer;
			this.LoadedFolderName = prototype.WorldName;

			this.GameStateManager = gameStateManager;

			ChunkManager = prototype.ChunkManager;
			EntityManager = prototype.EntityManager;
			WorldInfo = prototype.WorldInfo;
			Skybox = prototype.Skybox;
			logic = prototype.Logic;
			PhysicsInfo = prototype.PhysicsInfo;

			HousingManager = prototype.HousingManager;

			ChatManager = new ChatManager(new Vector2(8, Options.CurrentWindowResolution.Y - 256));

			this.ChunkLoadManager = chunkLoadManager;

            worldInfoIO = winfoIO;
			entIO = entityIO;
			this.chunkIO = chunkIO;

			//TEMP start in night time
			//alive = DAY_CYCLE_TIME * 0.65f;

			this.sizeInCubes = worldSize;

			sizeInChunks = (int)((float)worldSize / Chunk.CHUNK_SIZE);

			if (!meshesLoaded)
				CreateMeshes(device);

			ProjectileManager = new ProjectileManager(this, device);
			EntityManager.Initialize(this);
			LightManager = new LightManager(device);

			PassiveSpawnerManager = new PassiveSpawnerManager(EntityManager);

			Main.CubeLitEffect.Parameters["WorldSize"].SetValue(new Vector3(worldSize));
			Main.CubeLitEffect.Parameters["CubeSize"].SetValue(new Vector3(Cube.CUBE_SCALE));
			Main.CubeUnlitEffect.Parameters["WorldSize"].SetValue(new Vector3(worldSize));
			Main.CubeUnlitEffect.Parameters["CubeSize"].SetValue(new Vector3(Cube.CUBE_SCALE));
		}

		private void CreateMeshes(GraphicsDevice device)
        {
			meshWireframeUnscaled = MeshHelper.MakeCubeVertexPositionColor(device, Vector3.Zero, new Vector3(1), MeshHelper.CubeFace.ALL, Color.White, DrawHelper.WhitePixel);
			meshMiningCube = MeshHelper.MakeCubeVertexPositionColorTextureNormal(device, Vector3.Zero, Vector3.One * Cube.CUBE_SCALE, MeshHelper.CubeFace.ALL, Color.White, null);

			List<VertexCube> vertices = new List<VertexCube>();
			List<int> indices = new List<int>();

			Vector3 l_b_f = new Vector3(0, 0, 1);
			Vector3 r_b_f = new Vector3(1, 0, 1);
			Vector3 r_b_n = new Vector3(1, 0, 0);
			Vector3 l_b_n = new Vector3(0, 0, 0);

			Vector3 l_t_n = new Vector3(0, 1, 0);
			Vector3 r_t_n = new Vector3(1, 1, 0);
			Vector3 r_t_f = new Vector3(1, 1, 1);
			Vector3 l_t_f = new Vector3(0, 1, 1);

			const float SKYBOX_SIDE_SIZE = 1024f;
			const float SKYBOX_WIDTH = SKYBOX_SIDE_SIZE * 4f;
			const float SKYBOX_HEIGHT = SKYBOX_SIDE_SIZE * 2f;

			//front face
			int offset = vertices.Count;
			indices.Add(offset + 0);
			indices.Add(offset + 1);
			indices.Add(offset + 3);
			indices.Add(offset + 1);
			indices.Add(offset + 2);
			indices.Add(offset + 3);

			vertices.Add(new VertexCube(r_b_n, Color.White, new Vector2(SKYBOX_SIDE_SIZE / SKYBOX_WIDTH, SKYBOX_SIDE_SIZE / SKYBOX_HEIGHT), new Vector3(0, 0, 1)));
			vertices.Add(new VertexCube(l_b_n, Color.White, new Vector2(0, SKYBOX_SIDE_SIZE / SKYBOX_HEIGHT), new Vector3(0, 0, 1)));
			vertices.Add(new VertexCube(l_t_n, Color.White, new Vector2(0, 0), new Vector3(0, 0, 1)));
			vertices.Add(new VertexCube(r_t_n, Color.White, new Vector2(SKYBOX_SIDE_SIZE / SKYBOX_WIDTH, 0), new Vector3(0, 0, 1)));

			//right face
			offset = vertices.Count;
			indices.Add(offset + 0);
			indices.Add(offset + 1);
			indices.Add(offset + 3);
			indices.Add(offset + 1);
			indices.Add(offset + 2);
			indices.Add(offset + 3);

			vertices.Add(new VertexCube(r_b_f, Color.White, new Vector2(SKYBOX_SIDE_SIZE * 2f / SKYBOX_WIDTH, SKYBOX_SIDE_SIZE / SKYBOX_HEIGHT), new Vector3(-1, 0, 0)));
			vertices.Add(new VertexCube(r_b_n, Color.White, new Vector2(SKYBOX_SIDE_SIZE * 1f / SKYBOX_WIDTH, SKYBOX_SIDE_SIZE / SKYBOX_HEIGHT), new Vector3(-1, 0, 0)));
			vertices.Add(new VertexCube(r_t_n, Color.White, new Vector2(SKYBOX_SIDE_SIZE * 1f / SKYBOX_WIDTH, 0), new Vector3(-1, 0, 0)));
			vertices.Add(new VertexCube(r_t_f, Color.White, new Vector2(SKYBOX_SIDE_SIZE * 2f / SKYBOX_WIDTH, 0), new Vector3(-1, 0, 0)));

			//back face
			offset = vertices.Count;
			indices.Add(offset + 0);
			indices.Add(offset + 1);
			indices.Add(offset + 3);
			indices.Add(offset + 1);
			indices.Add(offset + 2);
			indices.Add(offset + 3);

			vertices.Add(new VertexCube(l_b_f, Color.White, new Vector2(SKYBOX_SIDE_SIZE * 3f / SKYBOX_WIDTH, SKYBOX_SIDE_SIZE / SKYBOX_HEIGHT), new Vector3(0, 0, -1)));
			vertices.Add(new VertexCube(r_b_f, Color.White, new Vector2(SKYBOX_SIDE_SIZE * 2f / SKYBOX_WIDTH, SKYBOX_SIDE_SIZE / SKYBOX_HEIGHT), new Vector3(0, 0, -1)));
			vertices.Add(new VertexCube(r_t_f, Color.White, new Vector2(SKYBOX_SIDE_SIZE * 2f / SKYBOX_WIDTH, 0), new Vector3(0, 0, -1)));
			vertices.Add(new VertexCube(l_t_f, Color.White, new Vector2(SKYBOX_SIDE_SIZE * 3f / SKYBOX_WIDTH, 0), new Vector3(0, 0, -1)));

			//left face
			offset = vertices.Count;
			indices.Add(offset + 0);
			indices.Add(offset + 1);
			indices.Add(offset + 3);
			indices.Add(offset + 1);
			indices.Add(offset + 2);
			indices.Add(offset + 3);

			vertices.Add(new VertexCube(l_b_n, Color.White, new Vector2(SKYBOX_SIDE_SIZE * 4f / SKYBOX_WIDTH, SKYBOX_SIDE_SIZE / SKYBOX_HEIGHT), new Vector3(1, 0, 0)));
			vertices.Add(new VertexCube(l_b_f, Color.White, new Vector2(SKYBOX_SIDE_SIZE * 3f / SKYBOX_WIDTH, SKYBOX_SIDE_SIZE / SKYBOX_HEIGHT), new Vector3(1, 0, 0)));
			vertices.Add(new VertexCube(l_t_f, Color.White, new Vector2(SKYBOX_SIDE_SIZE * 3f / SKYBOX_WIDTH, 0), new Vector3(1, 0, 0)));
			vertices.Add(new VertexCube(l_t_n, Color.White, new Vector2(SKYBOX_SIDE_SIZE * 4f / SKYBOX_WIDTH, 0), new Vector3(1, 0, 0)));

			//top face
			offset = vertices.Count;
			indices.Add(offset + 0);
			indices.Add(offset + 1);
			indices.Add(offset + 3);
			indices.Add(offset + 1);
			indices.Add(offset + 2);
			indices.Add(offset + 3);

			vertices.Add(new VertexCube(l_t_f, Color.White, new Vector2(SKYBOX_SIDE_SIZE / SKYBOX_WIDTH, SKYBOX_SIDE_SIZE * 2f / SKYBOX_HEIGHT), new Vector3(0, -1, 0)));
			vertices.Add(new VertexCube(r_t_f, Color.White, new Vector2(0, SKYBOX_SIDE_SIZE * 2f / SKYBOX_HEIGHT), new Vector3(0, -1, 0)));
			vertices.Add(new VertexCube(r_t_n, Color.White, new Vector2(0, SKYBOX_SIDE_SIZE * 1f / SKYBOX_HEIGHT), new Vector3(0, -1, 0)));
			vertices.Add(new VertexCube(l_t_n, Color.White, new Vector2(SKYBOX_SIDE_SIZE / SKYBOX_WIDTH, SKYBOX_SIDE_SIZE * 1f / SKYBOX_HEIGHT), new Vector3(0, -1, 0)));


			//bottom face
			offset = vertices.Count;
			indices.Add(offset + 0);
			indices.Add(offset + 1);
			indices.Add(offset + 3);
			indices.Add(offset + 1);
			indices.Add(offset + 2);
			indices.Add(offset + 3);

			vertices.Add(new VertexCube(r_b_f, Color.White, new Vector2(SKYBOX_SIDE_SIZE * 2 / SKYBOX_WIDTH, SKYBOX_SIDE_SIZE * 2f / SKYBOX_HEIGHT), new Vector3(0, 1, 0)));
			vertices.Add(new VertexCube(l_b_f, Color.White, new Vector2(SKYBOX_SIDE_SIZE * 1 / SKYBOX_WIDTH, SKYBOX_SIDE_SIZE * 2f / SKYBOX_HEIGHT), new Vector3(0, 1, 0)));
			vertices.Add(new VertexCube(l_b_n, Color.White, new Vector2(SKYBOX_SIDE_SIZE * 1 / SKYBOX_WIDTH, SKYBOX_SIDE_SIZE * 1f / SKYBOX_HEIGHT), new Vector3(0, 1, 0)));
			vertices.Add(new VertexCube(r_b_n, Color.White, new Vector2(SKYBOX_SIDE_SIZE * 2 / SKYBOX_WIDTH, SKYBOX_SIDE_SIZE * 1f / SKYBOX_HEIGHT), new Vector3(0, 1, 0)));

			skyboxMesh = new SimpleMesh<VertexCube, int>(device, vertices, indices, DrawHelper.WhitePixel);

			meshesLoaded = true;
		}

		public void FinishLoading(GraphicsDevice device)
        {
			if (!skyboxMesh.Uploaded)
				skyboxMesh.Upload(device);

			//The player reference will not be set up after loading. We need to do that ourselves.
			//TODO multiplayer
			//Don't know how we'll handle this in multiplayer, but suffice to say this won't work.
			player = EntityManager.GetFirst<Player>();

			if (player == null)
			{
				//If we didn't manage to find the player using the new method, fall back to the old method.
				//This deserializes the player manually then loads the chunks around them.
				//This relies on reading metadata while deserializing so I'm not a huge fan of it and will probably get rid of it later.
				//TODO obsolete/deprecated
				entIO.DeserializePlayerChunk();
				player = EntityManager.GetFirst<Player>();

				if (player != null)
				{
					ChunkLoadManager.UpdateLoadTarget(player.Position);
					ChunkLoadManager.LoadAroundTarget(this);
					ChunkLoadManager.FlushLoadQueue(this);
				}
			}

			if (player != null)
				Main.camera.Position = player.Position;

			logic.FinishLoading(device);
		}

		public void UnfixedUpdate()
		{
		}

		private float alive;

		public void Update(double deltaTime)
		{
			PhysicsInfo.Simulation.Timestep((float)deltaTime);

			ChunkLoadManager.UpdateLoadTarget(player.Position);

			alive += (float)deltaTime;

			ChatManager.Update(deltaTime);

			ChunkManager.Update(deltaTime, this, ChunkLoadManager);
			//ChunkManager.ProcessChunkQueue(this, 0);
			ChunkLoadManager.Update(deltaTime, this);

			ProjectileManager.Update(deltaTime);
			EntityManager.Update(deltaTime);

			logic.Update(this, deltaTime);

			//TODO: remove allocation somehow
			//Perhaps an expanding array
			//Span<Cube> miningCubesUnwrapped = new Cube[miningCubes.Count];

			foreach (var mined in miningCubes)
			{
				MinedCube mc = mined.Value;

				if (ChunkLoadManager.IsLoaded(mc.chunk))
				{
					Cube cube = ChunkManager.ThreadedView.GetCube(mc.position).GetOrDefault(Main.Registry.CubeRegistry.Air);

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

			Span<CubePosition> rups = stackalloc CubePosition[Main.RANDOM_UPDATES_PER_CHUNK];
			Span<ushort> rupis = stackalloc ushort[Main.RANDOM_UPDATES_PER_CHUNK];

			//perform random updates
			//There is RANDOM_UPDATES_PER_CHUNK updates per chunk per RANDOM_UPDATES_TIME.
			if (randomUpdatesTimer <= 0)
			{
				randomUpdatesTimer += Main.RANDOM_UPDATES_TIME;
				foreach (ChunkPosition loadedPosition in ChunkLoadManager.GetLoaded())
				{
					for (int i = 0; i < Main.RANDOM_UPDATES_PER_CHUNK; i++)
					{
						int num = Main.random.Next(0, Chunk.NUM_CUBES_IN_CHUNK);
						Util.OneDToThreeD(num, new ValuePoint3D(Chunk.CHUNK_SIZE, Chunk.CHUNK_SIZE, Chunk.CHUNK_SIZE), out ValuePoint3D pi);
						CubePosition randomUpdatePos = new CubePosition(pi.x, pi.y, pi.z, CubePosition.CoordinateSpace.ChunkSpace).InCubeSpace(loadedPosition);

						rups[i] = randomUpdatePos;
					}

					ChunkManager.ThreadedView.GetIds(rups, rupis);

					for (int i = 0; i < Main.RANDOM_UPDATES_PER_CHUNK; i++)
					{
						Cube cube = Main.Registry.CubeRegistry.GetOrDefault(rupis[i], Main.Registry.CubeRegistry.Air);

						if (cube != Main.Registry.CubeRegistry.Air)
							cube.OnRandomUpdate(this, ChunkManager, rups[i]);
					}
				}
			}
			else randomUpdatesTimer -= (float)deltaTime;

			PassiveSpawnerManager.Update(deltaTime, this);

			ChunkPosition camPos = ChunkPosition.WorldSpaceChunk(Main.camera.Position);

			if (chunkDrawPositionsDirty || Main.camera.IsDirty)
			{
				CulledChunkDrawPositions.Clear();

				for (int x = Math.Max(0, camPos.X - DrawDistanceHoriz); x <= Math.Min(sizeInChunks, camPos.X + DrawDistanceHoriz); x++)
				{
					for (int y = Math.Max(0, camPos.Y - DrawDistanceVert); y <= Math.Min(sizeInChunks, camPos.Y + DrawDistanceVert); y++)
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

			TryLoadNextLayer();
		}

		private void TryLoadNextLayer()
        {
			if (logic.AllowsLoadingNextLayer(this) && nextWorld == null)
			{
				if (player.Position.Y < Cube.CUBE_SCALE * Chunk.CHUNK_SIZE * 3)
					nextLayer = Layer + 1;
				else if (player.Position.Y >= Cube.CUBE_SCALE * sizeInCubes - (Chunk.CHUNK_SIZE * 3 * Cube.CUBE_SCALE))
					nextLayer = Layer - 1;
				else nextLayer = Layer;

				if (nextLayer != Layer)
					nextWorld = GameStateManager.TheIsland.BeginLoadLayer(LoadedFolderName, nextLayer);
			}

			//if in the middle 22 chunks (> 0-5 chunks && < 32-27 chunks), unload the loaded world.
			if (player.Position.Y > Cube.CUBE_SCALE * Chunk.CHUNK_SIZE * 5 &&
				player.Position.Y <= sizeInChunks * Chunk.CHUNK_SIZE * Cube.CUBE_SCALE - (Chunk.CHUNK_SIZE * Cube.CUBE_SCALE * 5) && nextWorld != null)
			{
				if (nextWorld.IsCompleted)
				{
					nextWorld.Result.Dispose();
					nextWorld = null;
				}
			}

			if (nextWorld != null && player.Position.Y < Cube.CUBE_SCALE * 4 ||
				player.Position.Y >= sizeInChunks * Chunk.CHUNK_SIZE * Cube.CUBE_SCALE - (4 * Cube.CUBE_SCALE))
			{
				GameStateManager.TheIsland.LoadMessage = "Waiting for world to finish loading...";
				if (!nextWorld.IsCompleted)
					nextWorld.Wait();

				GameStateManager.TheIsland.LoadMessage = "Moving to new world...";
				World loadedWorld = nextWorld.Result;

				if (nextLayer == Layer + 1)
				{
					player.Position.Y = player.Position.Y + Cube.CUBE_SCALE * (512 - Chunk.CHUNK_SIZE);

					ProfilingHelper.Start("Copying Layer");
					for (int x = 0; x < sizeInCubes; x++)
					{
						for (int z = 0; z < sizeInCubes; z++)
						{
							for (int y = 0; y < Chunk.CHUNK_SIZE; y++)
							{
								Cube cube = ChunkManager.InitializerView.GetCube(new CubePosition(x, y, z)).GetOrDefault(Main.Registry.CubeRegistry.Air);

								loadedWorld.ChunkManager.InitializerView.SetCube(new CubePosition(x, sizeInCubes - Chunk.CHUNK_SIZE + y, z), cube.Id);
							}
						}
					}
					ProfilingHelper.End("Done");
				}
				else if (nextLayer == Layer - 1)
					player.Position.Y = player.Position.Y - Cube.CUBE_SCALE * (512 - Chunk.CHUNK_SIZE);

				EntityManager.Unload(player);
				player.world = loadedWorld;
				loadedWorld.EntityManager.Add(player);
				loadedWorld.player = player;

				WorldInfo.playerLayer = loadedWorld.Layer;
				WorldInfo.playerPosition = loadedWorld.player.Position;

				loadedWorld.ChunkLoadManager.UpdateLoadTarget(player.Position);
				loadedWorld.ChunkLoadManager.LoadAroundTarget(loadedWorld);

				//Finally, tell the ChunkLoadManager to actually load the things.
				//(We have to tell it this manually as it queues things up to load, and we want it to finish loading instead of load things in the background
				//as it normally does.)
				loadedWorld.ChunkLoadManager.FlushLoadQueue(loadedWorld);

				GameStateManager.TheIsland.SetWorld(loadedWorld);

				GameStateManager.TheIsland.LoadMessage = "Saving...";
				//Player has been moved to nextWorld, therefore we need to save some parts of the current world to tell the world that it's gone.
				//Note that we don't save chunks because they shouldn't be modified by any operation here.
				entIO.Save(LoadedFolderName);   
				worldInfoIO.Save(LoadedFolderName, WorldInfo);

				//Then save the entire nextWorld. We save chunks here since we may have modified them.
				//This should also update worldInfo.
				loadedWorld.SaveWorld();

				//TODO: why are we disposing this when we haven't even exited the load boundary?
				//We should be reusing this so we can reload super fast
				//Dispose();

				loadedWorld.nextWorld = new Task<World>(() => { return this; });
				loadedWorld.nextWorld.Start();
				loadedWorld.nextLayer = Layer;
			}
		}

		public void SaveWorld()
        {
			Main.SessionInformation.LastLoadedSave = LoadedFolderName;
			Main.SessionIO.Save();

			//Flush the load queue so we don't end up not saving chunks that are currently loading in.
			//This is probably unnecessary (why would data in newly loaded chunks change ever?) but it's best to be on the safe side.
			ChunkLoadManager.FlushLoadQueue(this);
			//Serialize all the chunks that are currently loaded
			//chunkIO.Serialize(ChunkLoadManager.GetLoaded());
			entIO.Serialize(ChunkLoadManager.GetLoaded());

			//Save serialized data to disk
			chunkIO.Save(LoadedFolderName);
			entIO.Save(LoadedFolderName);

			//Deduplicate/decache serialized entity data
			entIO.DecacheCurrentlySerialized();

			if (player != null)
			{
				WorldInfo.playerPosition = player.Position;
				WorldInfo.playerLayer = Layer;
				worldInfoIO.Save(LoadedFolderName, WorldInfo);
			}
		}

		//Gets a list of all chunks that should be rendered by the main camera.
		public List<ChunkPosition> GetChunkDrawPositions()
        {
			return CulledChunkDrawPositions;
        }

		public static int NumChunksDrawn;
		public static double ChunkDrawTime;

		public void Draw(GraphicsDevice device)
		{
			NumChunksDrawn = 0;
			ChunkDrawTime = 0;

			Stopwatch drawTime = Stopwatch.StartNew();

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
				Matrix transform = Matrix.Identity; //ChunkManager.GetTransform(pos);

				Texture2D emissiveTexture = Main.assetsManager.GetAsset<Texture2D>("cubes_textures_emissive");
				if (player.GetBuffManager().HasBuff("emissive_ores"))
					emissiveTexture = Main.assetsManager.GetAsset<Texture2D>("cubes_textures_emissive_ores");

                (VertexBuffer VBO, IndexBuffer IBO) mesh = ChunkManager.GetMesh(pos, Cubes.Cube.RenderPass.Opaque);
                if (mesh.VBO != null)
                {
                    Main.Renderer.DrawsPassGBuffer.Add(new Rendering.RendererDeferred.GBufferDraw(Main.assetsManager.GetAsset<Texture2D>("cubes_textures"),
                        DrawHelper.BlackPixel, emissiveTexture, mesh.VBO, mesh.IBO,
                        transform, null));
                }

                mesh = ChunkManager.GetMesh(pos, Cubes.Cube.RenderPass.Transparent);
                if (mesh.VBO != null)
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
					if (mesh.VBO != null)
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
			}

			if (drawSkybox)
			{
				float alphaDay = 1 - GetTimeOfDay();
				float alphaNight = GetTimeOfNight();

				if (alphaDay < 1)
				{
					const float mp = (DAY_CYCLE_TIME * 1.5f);
					const float my = (DAY_CYCLE_TIME * 1.34f);
					float p = MathF.Sin(MathF.PI * 2 * ((alive % mp) / mp));
					float y = MathF.Sin(MathF.PI * 2 * ((alive % my) / my));

					Main.Renderer.DrawsSkyboxPass.Add(new Rendering.RendererDeferred.TransparentDraw(1001,
						Matrix.CreateTranslation(new Vector3(-0.5f)) *
						Matrix.CreateScale(DrawDistanceHoriz * 1.95f * (Chunk.CHUNK_SIZE * Cube.CUBE_SCALE)) *
						Matrix.CreateFromYawPitchRoll(y, p, 0) *
						Matrix.CreateTranslation(Main.camera.Position),
						Skybox.Night, DrawHelper.WhitePixel,
						skyboxMesh.VBO, skyboxMesh.IBO, null, Color.White));
				}

				if (alphaDay > 0)
				{
					Main.Renderer.EffectRadialFog.Parameters["ColorInterpolate"].SetValue(new Vector3(0, 1, 1 - alphaDay));

					Main.Renderer.DrawsSkyboxPass.Add(new Rendering.RendererDeferred.TransparentDraw(1000,
						Matrix.CreateTranslation(new Vector3(-0.5f)) *
						Matrix.CreateScale(DrawDistanceHoriz * 2 * (Chunk.CHUNK_SIZE * Cube.CUBE_SCALE)) *
						Matrix.CreateTranslation(Main.camera.Position),
						Skybox.Day, DrawHelper.BlackPixel,
						skyboxMesh.VBO, skyboxMesh.IBO, null, Color.White * alphaDay));
				}

				if (Main.Debug)
					HitboxManager.DrawDebug(device);

				if (Main.Debug)
					HousingManager.DrawDebug(this, device);
			}

			foreach (var mined in miningCubes)
			{
				Cube cube = ChunkManager.ThreadedView.GetCube(mined.Value.position).GetOrDefault(Main.Registry.CubeRegistry.Air);

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

			ProjectileManager.Draw(device);
			EntityManager.Draw(device, Main.CubeLitEffect);

			logic.Draw(this, device);

			drawTime.Stop();
			ChunkDrawTime = drawTime.Elapsed.TotalSeconds;
		}

		public void DrawUI(SpriteBatch batch)
		{
			player.DrawUI(batch);

			ChatManager.Draw(batch);
		}

		public void OnCubeUpdate(CubePosition updating, ushort updatedId)
		{
			logic.OnCubeUpdated(updating, updatedId);

			HousingManager.OnCubeUpdate(this, updating, updatedId);
			//TODO: this should be optimized. Right now we're updating literally every entity. We don't need to do this,
			//Just every entity that could respond to this cube. 
			//What constitutes an entity that could respond to this cube? I don't know exactly.
			//Right now this exists as it does pretty much only so that trees and skeletons can work.
			foreach (Entity entity in EntityManager.GetEntities())
			{
				entity.OnCubeUpdated(updating, updatedId);
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

		public float GetTimeOfDay(float dawnStartOffsetScale = 1f, float dawnEndOffsetScale = 1f, float duskStartOffsetScale = 1, float duskEndOffsetScale = 1)
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

		public float GetDuskTime()
        {
			//Dusk starts at the last 8% of the day cycle.
			const float DUSK_START = 0.42f;
			const float DUSK_END = 0.56f;

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
			MinedCube mined = new MinedCube()
			{
				position = position,
				chunk = ChunkPosition.CubeChunk(position),
				progress = num,
				timer = 2
			};

			Cube cube = ChunkManager.ThreadedView.GetCube(position).GetOrDefault(Main.Registry.CubeRegistry.Air);

			if (cube != Main.Registry.CubeRegistry.Air && (level >= cube.MineLevelRequirement || instant))
			{
				if (instant)
				{
					ChunkManager.ThreadedView.SetCube(position, 0);

					List<ItemInstance> items = new List<ItemInstance>();
					cube.GetDrops(items);

					foreach (ItemInstance item in items)
					{
						EntityItem ent = new EntityItem(position.InWorldSpace() + new Vector3(Cube.CUBE_SCALE / 2f),
							new Vector3(Main.random.NextFloat(-Cube.CUBE_SCALE * 5, Cube.CUBE_SCALE * 5), Cube.CUBE_SCALE * 6.4f,
								Main.random.NextFloat(-Cube.CUBE_SCALE * 5, Cube.CUBE_SCALE * 5)), item);
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
						ChunkManager.ThreadedView.SetCube(position, 0);

						List<ItemInstance> items = new List<ItemInstance>();
						cube.GetDrops(items);

						foreach (ItemInstance item in items)
						{
							EntityItem ent = new EntityItem(position.InWorldSpace() + new Vector3(Cube.CUBE_SCALE / 2f),
								new Vector3(Main.random.NextFloat(-Cube.CUBE_SCALE * 5, Cube.CUBE_SCALE * 5), Cube.CUBE_SCALE * 6.4f,
									Main.random.NextFloat(-Cube.CUBE_SCALE * 5, Cube.CUBE_SCALE * 5)), item);
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
						ChunkManager.ThreadedView.SetCube(position, 0);

						List<ItemInstance> items = new List<ItemInstance>();
						cube.GetDrops(items);

						foreach (ItemInstance item in items)
						{
							EntityItem ent = new EntityItem(position.InWorldSpace() + new Vector3(Cube.CUBE_SCALE / 2f),
								new Vector3(Main.random.NextFloat(-Cube.CUBE_SCALE * 5, Cube.CUBE_SCALE * 5), Cube.CUBE_SCALE * 6.4f,
									Main.random.NextFloat(-Cube.CUBE_SCALE * 5, Cube.CUBE_SCALE * 5)), item);
							EntityManager.Add(ent);
						}

						cube.OnMined(player, position);

						return true;
					} 
				}
			}

			return false;
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

		public void Dispose()
        {
			ChunkLoadManager.Dispose();
			LightManager.Dispose();
			logic.Dispose();

			PhysicsInfo.Simulation.Dispose();
			PhysicsInfo.Properties.Dispose();
			PhysicsInfo.GlobalBufferPool.Clear();
        }
	}
}
