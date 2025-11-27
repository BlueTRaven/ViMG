using BepuPhysics;
using BepuPhysics.Constraints;
using BepuUtilities.Memory;
using BrUtility;
using Engine.Networking.Messages;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SharpDX.MediaFoundation;

//using SimplexNoise;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ViMG.Cubes;
using ViMG.Entities;
using ViMG.GameStates;
using ViMG.Generation;
using ViMG.IMGUIImpl;
using ViMG.Items;
using ViMG.Physics;
using ViMG.Rendering;
using ViMG.Spawners;
using ViMG.UIs;
using ViMG.VertexDeclarations;
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

		// TODO reimplement
		private static VerySimpleMesh? meshMiningCube = null;
		private static VerySimpleMesh? skyboxMesh = null;
		public Skybox Skybox;
		public float WeatherSkyboxAlpha;
		public Color WeatherSkyboxColor;

		public const int MAX_PLAYERS = 4;
		public Player?[] player = new Player[4];
		public int localPlayerIndex;

		public int DrawDistanceHoriz = 6;   //radius in chunks that we should be able to see
		public int DrawDistanceVert = 6;
		public int DrawRadius = 6;

		public float TimeScale = 1f;

		public HitboxManager HitboxManager = new HitboxManager(32);
		public ProjectileManager ProjectileManager;
		public EntityManager EntityManager;
		public LightManager? LightManager;
		public PassiveSpawnerManager PassiveSpawnerManager;
		public WorldInfoIO.WorldInfo WorldInfo;
		public ChunkLoadManager ChunkLoadManager;
		public PhysicsInfo PhysicsInfo;
		public ChatManager ChatManager;
		public MenuDialogue MenuDialogue;
		//public DialogueManager DialogueManager;

		private WorldInfoIO worldInfoIO;
		public ChunkManagerIO ChunkIO;
		public EntityManagerIO EntIO;
		public WorldLogic Logic;

		public HousingManager HousingManager;

		public Color SkyColor = new Color(94, 107, 154);

		public List<ChunkPosition> CulledChunkDrawPositions = new List<ChunkPosition>();
		private bool chunkDrawPositionsDirty = true;
		private ChunkPosition oldChunkPosition;
		private Vector3 oldCameraRotation;

        public bool isDisposed;
		// TODO HACK
		public bool isCreateWorldReloading;

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

		public World(WorldPrototype prototype, ChunkLoadManager chunkLoadManager, 
			WorldInfoIO winfoIO, EntityManagerIO entityIO, ChunkManagerIO chunkIO, int worldSize)
		{
            using var zone = TracyImpl.Tracy.BeginZone();

            this.Layer = prototype.Layer;
			this.LoadedFolderName = prototype.WorldName;

			ChunkManager = prototype.ChunkManager;
			EntityManager = prototype.EntityManager;
			WorldInfo = prototype.WorldInfo;
			Skybox = prototype.Skybox;
			Logic = prototype.Logic;
			PhysicsInfo = prototype.PhysicsInfo;

			HousingManager = prototype.HousingManager;

			//DialogueManager = new DialogueManager();

			this.ChunkLoadManager = chunkLoadManager;

            worldInfoIO = winfoIO;
			EntIO = entityIO;
			this.ChunkIO = chunkIO;

			//TEMP start in night time
			//alive = DAY_CYCLE_TIME * 0.65f;

			this.sizeInCubes = worldSize;

			sizeInChunks = (int)((float)worldSize / Chunk.CHUNK_SIZE);

			ProjectileManager = new ProjectileManager(this);
			EntityManager.Initialize(this);
			
			if (Main.gameStateManager.netMode != GameStateManager.NetworkingMode.Client)
				PassiveSpawnerManager = new PassiveSpawnerManager(EntityManager);
		}

		public void InitMeshes(GraphicsDevice device)
        {
            ChatManager = new ChatManager(new Vector2(8, Options.CurrentWindowResolution.Y - 256));
            MenuDialogue = new MenuDialogue(Main.gameStateManager);

            ProjectileManager.InitMeshes(device);
            LightManager = new LightManager(device);

			//meshMiningCube = MeshHelper.MakeCubeVertexPositionColorTextureNormal(device, Vector3.Zero, Vector3.One * Cube.CUBE_SCALE, MeshHelper.CubeFace.ALL, Color.White, null);

			if (skyboxMesh == null)
			{
				FastList<VertexCube> vertices = new FastList<VertexCube>();
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
				int offset = vertices.Length;
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
				offset = vertices.Length;
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
				offset = vertices.Length;
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
				offset = vertices.Length;
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
				offset = vertices.Length;
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
				offset = vertices.Length;
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

				skyboxMesh = VerySimpleMesh.Transparent(device, ChunkRenderMesher.VertexAttributes.Transparent(vertices, indices)); //MeshHelper.MakeSimplerMesh(device, vertices.ToVertexTransparentPass(), indices);
			}
		}

		public void FinishLoading(GraphicsDevice device)
        {
            using var zone = TracyImpl.Tracy.BeginZone();

            //The player reference will not be set up after loading. We need to do that ourselves.
			foreach (Player p in EntityManager.GetAll<Player>())
			{
				player[p.playerIndex] = p;
			}

			// TODO: do we need this?
			//if (player.All(x => x == null))
			//{
			//	//If we didn't manage to find the player using the new method, fall back to the old method.
			//	//This deserializes the player manually then loads the chunks around them.
			//	//This relies on reading metadata while deserializing so I'm not a huge fan of it and will probably get rid of it later.
			//	//TODO obsolete/deprecated
			//	EntIO.DeserializePlayerChunk();
   //             foreach (Player p in EntityManager.GetAll<Player>())
   //             {
   //                 player[p.playerIndex] = p;
   //             }

			//	if (GetLocalPlayer() != null)
			//	{
			//		ChunkLoadManager.LoadAroundTarget(this);
			//		ChunkLoadManager.FlushLoadQueue(this);
			//	}
			//}

			if (GetLocalPlayer() != null)
				Main.camera.Position = GetLocalPlayer().Position;

			Logic.FinishLoading(this, device);
		}

		public void UnfixedUpdate()
		{
		}

		private float alive;

		public void Update(double deltaTime)
		{
			//EntIO.TestConsistency(GetLocalPlayer());
            using var zone = TracyImpl.Tracy.BeginZone();

			//if (Main.Frame % 240 == 0)
			//{
			//	Console.WriteLine("Frame {0} Time {1}", Main.Frame, Main.Time);
			//}

            deltaTime *= TimeScale;

            PhysicsInfo.Simulation.Timestep((float)deltaTime);

			alive += (float)deltaTime;

			ChatManager.Update(deltaTime);
			//DialogueManager.Update(deltaTime);

			ChunkManager.Update(deltaTime, this, ChunkLoadManager);
			//ChunkManager.ProcessChunkQueue(this, 0);
			ChunkLoadManager.Update(deltaTime, this);

			ProjectileManager.Update(deltaTime);
			EntityManager.Update(deltaTime);

			SyncPlayerInputs.Instance.Apply(player);
			SyncBasicState.Instance.Apply(EntityManager, EntIO);
			SyncCubeUpdate.Instance.Apply(ChunkManager, player);
			SyncCubeUpdateAuditRequest.Instance.Apply(ChunkManager, player);
			SyncInventoryUpdate.Instance.Apply(EntityManager);
			SyncInventoryUpdateAuditRequest.Instance.Apply(EntityManager);

			if (Main.gameStateManager.netMode == GameStateManager.NetworkingMode.Server && Main.Time - timeSyncTime > 1)
			{
				Main.Registry.MessageRegistry.SendMessageToAll(SyncWorldState.Instance, Main.gameStateManager.TheIsland.netManager.netManager, null);
				timeSyncTime = Main.Time;
			}

			Logic.Update(this, deltaTime);

			//TODO: remove allocation somehow
			//Perhaps an expanding array
			//Span<Cube> miningCubesUnwrapped = new Cube[miningCubes.Count];

			using (var zoneUpdateMiningCubes = TracyImpl.Tracy.BeginZone()) 
			{
				foreach (var mined in miningCubes)
				{
					MinedCube mc = mined.Value;

					if (ChunkLoadManager.IsLoaded(mc.chunk))
					{
						Cube cube = ChunkManager.CubeView.GetCube(mc.position).GetOrDefault(Main.Registry.CubeRegistry.Air);

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
			}

            using (var zoneRandomUpdates = TracyImpl.Tracy.BeginZone())
			{
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

						ChunkManager.CubeView.GetIds(rups, rupis);

						for (int i = 0; i < Main.RANDOM_UPDATES_PER_CHUNK; i++)
						{
							Cube cube = Main.Registry.CubeRegistry.GetOrDefault(rupis[i], Main.Registry.CubeRegistry.Air);

							if (cube != Main.Registry.CubeRegistry.Air)
								cube.OnRandomUpdate(this, ChunkManager, rups[i]);
						}
					}
				}
				else randomUpdatesTimer -= (float)deltaTime;
			}

			PassiveSpawnerManager?.Update(deltaTime, this);

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
            using var zone = TracyImpl.Tracy.BeginZone();

            if (Logic.AllowsLoadingNextLayer(this) && nextWorld == null)
			{
				if (player[localPlayerIndex].Position.Y < Cube.CUBE_SCALE * Chunk.CHUNK_SIZE * 3)
					nextLayer = Layer + 1;
				else if (player[localPlayerIndex].Position.Y >= Cube.CUBE_SCALE * sizeInCubes - (Chunk.CHUNK_SIZE * 3 * Cube.CUBE_SCALE))
					nextLayer = Layer - 1;
				else nextLayer = Layer;

				if (nextLayer != Layer)
					nextWorld = Main.gameStateManager.TheIsland.BeginLoadLayer(LoadedFolderName, nextLayer);
			}

			//if in the middle 22 chunks (> 0-5 chunks && < 32-27 chunks), unload the loaded world.
			if (player[localPlayerIndex] != null)
			{
				if (player[localPlayerIndex].Position.Y > Cube.CUBE_SCALE * Chunk.CHUNK_SIZE * 5 &&
                player[localPlayerIndex].Position.Y <= sizeInChunks * Chunk.CHUNK_SIZE * Cube.CUBE_SCALE - (Chunk.CHUNK_SIZE * Cube.CUBE_SCALE * 5) && nextWorld != null)
				{
					if (nextWorld.IsCompleted)
					{
						nextWorld.Result.Dispose();
						nextWorld = null;
					}
				}

				if (nextWorld != null && player[localPlayerIndex].Position.Y < Cube.CUBE_SCALE * 4 ||
                    player[localPlayerIndex].Position.Y >= sizeInChunks * Chunk.CHUNK_SIZE * Cube.CUBE_SCALE - (4 * Cube.CUBE_SCALE))
				{
					GameStateTheIsland.LoadMessage = "Waiting for world to finish loading...";
					if (!nextWorld.IsCompleted)
						nextWorld.Wait();

					GameStateTheIsland.LoadMessage = "Moving to new world...";
					World loadedWorld = nextWorld.Result;

					if (nextLayer == Layer + 1)
					{
                        player[localPlayerIndex].Position.Y = player[localPlayerIndex].Position.Y + Cube.CUBE_SCALE * (512 - Chunk.CHUNK_SIZE);

						ProfilingHelper.Start("Copying Layer");
						for (int x = 0; x < sizeInCubes; x++)
						{
							for (int z = 0; z < sizeInCubes; z++)
							{
								for (int y = 0; y < Chunk.CHUNK_SIZE; y++)
								{
									Cube cube = ChunkManager.CubeView.GetCube(new CubePosition(x, y, z)).GetOrDefault(Main.Registry.CubeRegistry.Air);

									loadedWorld.ChunkManager.CubeView.SetCube(new CubePosition(x, sizeInCubes - Chunk.CHUNK_SIZE + y, z), cube.Id);
								}
							}
						}
						ProfilingHelper.End("Done");
					}
					else if (nextLayer == Layer - 1)
                        player[localPlayerIndex].Position.Y = player[localPlayerIndex].Position.Y - Cube.CUBE_SCALE * (512 - Chunk.CHUNK_SIZE);

					EntityManager.Unload(player[localPlayerIndex]);
                    player[localPlayerIndex].world = loadedWorld;
					loadedWorld.EntityManager.Add(player[localPlayerIndex]);
					loadedWorld.player = player;

					WorldInfo.playerLayers[localPlayerIndex] = loadedWorld.Layer;
					WorldInfo.playerPositions[localPlayerIndex] = loadedWorld.player[localPlayerIndex].Position;

					loadedWorld.ChunkLoadManager.LoadAroundTarget(loadedWorld);

					//Finally, tell the ChunkLoadManager to actually load the things.
					//(We have to tell it this manually as it queues things up to load, and we want it to finish loading instead of load things in the background
					//as it normally does.)
					loadedWorld.ChunkLoadManager.FlushLoadQueue(this);

                    Main.gameStateManager.TheIsland.SetWorld(loadedWorld);

					GameStateTheIsland.LoadMessage = "Saving...";
					//Player has been moved to nextWorld, therefore we need to save some parts of the current world to tell the world that it's gone.
					//Note that we don't save chunks because they shouldn't be modified by any operation here.
					EntIO.Save(LoadedFolderName);
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
		}

		public void SaveWorld()
        {
            using var zone = TracyImpl.Tracy.BeginZone();

			Debug.Assert(Main.gameStateManager.netMode != GameStateManager.NetworkingMode.Client);

            Main.SessionInformation.LastLoadedSave = LoadedFolderName;
			Main.SessionIO.Save();

			//Flush the load queue so we don't end up not saving chunks that are currently loading in.
			//This is probably unnecessary (why would data in newly loaded chunks change ever?) but it's best to be on the safe side.
			ChunkLoadManager.FlushLoadQueue(this);
			//Serialize all the chunks that are currently loaded
			//chunkIO.Serialize(ChunkLoadManager.GetLoaded());
			EntIO.Serialize(ChunkLoadManager.GetLoaded());

			//Save serialized data to disk
			ChunkIO.Save(LoadedFolderName);
			EntIO.Save(LoadedFolderName);

			//Deduplicate/decache serialized entity data
			EntIO.DecacheCurrentlySerialized();

			for (int i = 0; i < MAX_PLAYERS; i++)
			{
				WorldInfo.playerPositions[i] = player[i]?.Position ?? WorldInfo.spawnPosition;
				WorldInfo.playerLayers[i] = Layer;
			}
				
			worldInfoIO.Save(LoadedFolderName, WorldInfo);
		}

		public Player? GetLocalPlayer()
		{
			if (localPlayerIndex >= 0 && localPlayerIndex < MAX_PLAYERS)
			{
				return player[localPlayerIndex];
			}
			else return null;
		}

		private static List<int> validIndices = new List<int>();
		/// <summary>
		/// Gets a random player.
		/// </summary>
		/// <returns>A random player, or null if there were no active players.</returns>
		public Player? GetRandomPlayer()
		{
			validIndices.Clear();
			for (int i = 0; i < MAX_PLAYERS; i++)
			{
				if (player[i] != null)
				{
					validIndices.Add(i);
				}
			}

			if (validIndices.Count > 0)
			{
				return player[validIndices[Main.random.Next(validIndices.Count)]];
			}
			else return null;
		}

		//Gets a list of all chunks that should be rendered by the main camera.
		public List<ChunkPosition> GetChunkDrawPositions()
        {
			return CulledChunkDrawPositions;
        }

		public static int NumChunksDrawn;
		public static double ChunkDrawTime;
        private double timeSyncTime;

        public void Draw(GraphicsDevice device)
		{
            using var zone = TracyImpl.Tracy.BeginZone();

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

				RendererDeferred.DrawMaterial cubesMaterial = StaticMaterials.Cubes;
				if (GetLocalPlayer()?.GetBuffManager().HasBuff("emissive_ores") ?? false)
					cubesMaterial = StaticMaterials.CubesWithEmissiveOres;

                VerySimpleMesh mesh = ChunkManager.ChunkMesher.RenderMesher.GetMesh(pos, Cubes.Cube.RenderPass.Opaque);
				if (mesh.IBO != null)
				{
					Main.Renderer.AddOpaqueDraw(new RendererDeferred.GBufferDraw(cubesMaterial, mesh, transform));
				}
                //if (mesh.VBO != null)
                //{
                //    Main.Renderer.AddOpaqueDraw(new RendererDeferred.GBufferDraw(cubesMaterial, mesh.VBO, mesh.IBO,
                //        transform, null));
                //}

                mesh = ChunkManager.ChunkMesher.RenderMesher.GetMesh(pos, Cubes.Cube.RenderPass.Transparent);
                if (mesh.IBO != null)
                {
                    Vector3 minBounds = Main.camera.Position - pos.InWorldSpace();
                    Vector3 maxBounds = Main.camera.Position - minBounds + new Vector3(Chunk.CHUNK_SIZE * Cube.CUBE_SCALE);

                    Vector3 min = new Vector3(Math.Min(minBounds.X, maxBounds.X), Math.Min(minBounds.Y, maxBounds.Y), Math.Min(minBounds.Z, maxBounds.Z));
                    //Vector3 max = new Vector3(Math.Max(minBounds.X, maxBounds.X), Math.Max(minBounds.Y, maxBounds.Y), Math.Max(minBounds.Z, maxBounds.Z));

                    Main.Renderer.AddTransparentDraw(new RendererDeferred.TransparentDraw((int)min.Length(), cubesMaterial, mesh, transform));
                }
                //if (mesh.VBO != null)
                //{
                //    Vector3 minBounds = Main.camera.Position - pos.InWorldSpace();
                //    Vector3 maxBounds = Main.camera.Position - minBounds + new Vector3(Chunk.CHUNK_SIZE * Cube.CUBE_SCALE);

                //    Vector3 min = new Vector3(Math.Min(minBounds.X, maxBounds.X), Math.Min(minBounds.Y, maxBounds.Y), Math.Min(minBounds.Z, maxBounds.Z));
                //    //Vector3 max = new Vector3(Math.Max(minBounds.X, maxBounds.X), Math.Max(minBounds.Y, maxBounds.Y), Math.Max(minBounds.Z, maxBounds.Z));

                //    Main.Renderer.AddTransparentDraw(new Rendering.RendererDeferred.TransparentDraw((int)min.Length(),
                //        cubesMaterial, mesh.VBO, mesh.IBO, transform));
                //}

				if (Main.Renderer.EffectEmptyEnabled)
				{
					mesh = ChunkManager.ChunkMesher.RenderMesher.GetMesh(pos, Cubes.Cube.RenderPass.Air);
                    if (mesh.IBO != null)
                    {
                        Vector3 minBounds = Main.camera.Position - pos.InWorldSpace();
                        Vector3 maxBounds = Main.camera.Position - minBounds + new Vector3(Chunk.CHUNK_SIZE * Cube.CUBE_SCALE);

                        Vector3 min = new Vector3(Math.Min(minBounds.X, maxBounds.X), Math.Min(minBounds.Y, maxBounds.Y), Math.Min(minBounds.Z, maxBounds.Z));

                        Main.Renderer.DrawsEmptyPass.Add(new Rendering.RendererDeferred.TransparentDraw((int)min.Length(),
                            StaticMaterials.Cubes, mesh, transform));
                    }
     //               if (mesh.VBO != null)
					//{
					//	Vector3 minBounds = Main.camera.Position - pos.InWorldSpace();
					//	Vector3 maxBounds = Main.camera.Position - minBounds + new Vector3(Chunk.CHUNK_SIZE * Cube.CUBE_SCALE);

					//	Vector3 min = new Vector3(Math.Min(minBounds.X, maxBounds.X), Math.Min(minBounds.Y, maxBounds.Y), Math.Min(minBounds.Z, maxBounds.Z));

					//	Main.Renderer.DrawsEmptyPass.Add(new Rendering.RendererDeferred.TransparentDraw((int)min.Length(), 
					//		StaticMaterials.Cubes, mesh.VBO, mesh.IBO, transform));
					//}
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
						new RendererDeferred.DrawMaterial(Skybox.Night),
                        skyboxMesh.Value,
                        Matrix.CreateTranslation(new Vector3(-0.5f)) *
						Matrix.CreateFromYawPitchRoll(y, p, 0) *
						Matrix.CreateTranslation(Main.camera.Position),
						null, Color.White));
				}

				if (alphaDay > 0)
				{
					Main.Renderer.EffectRadialFog.Parameters["ColorInterpolate"].SetValue(new Vector3(0, 1, 1 - alphaDay));

					Main.Renderer.DrawsSkyboxPass.Add(new Rendering.RendererDeferred.TransparentDraw(1000,
						new RendererDeferred.DrawMaterial(Skybox.Day),
                        skyboxMesh.Value,
                        Matrix.CreateTranslation(new Vector3(-0.5f)) *
						Matrix.CreateTranslation(Main.camera.Position),
						null, Color.White * alphaDay));
				}

				if (WeatherSkyboxAlpha > 0)
				{
					Main.Renderer.DrawsSkyboxPass.Add(new Rendering.RendererDeferred.TransparentDraw()
					{
						SortValue = 100,
						Material = new Rendering.RendererDeferred.DrawMaterial(Skybox.Weather),
						TintColor = WeatherSkyboxColor.ToVector4() * WeatherSkyboxAlpha,
						Transform = Matrix.CreateTranslation(new Vector3(-0.5f)) *
							Matrix.CreateTranslation(Main.camera.Position),
						Mesh = skyboxMesh.Value,
					});
				}

				if (Main.Debug)
					HitboxManager.DrawDebug(device);

				if (Main.Debug)
					HousingManager.DrawDebug(this, device);
			}

			foreach (var mined in miningCubes)
			{
				Cube cube = ChunkManager.CubeView.GetCube(mined.Value.position).GetOrDefault(Main.Registry.CubeRegistry.Air);

				if (cube != Main.Registry.CubeRegistry.Air)
				{
					float percent = (float)mined.Value.progress / (float)cube.MineProgressToBreak;

					float stepped = ((int)(percent * 8f)) / 8f;

					RectangleF sourceRect = new RectangleF(128f * stepped, 0, 16, 16);

					RendererDeferred.DrawMaterial material = new RendererDeferred.DrawMaterial("mine");
					//Main.Renderer.AddOpaqueDraw(new Rendering.RendererDeferred.GBufferDraw(material, meshMiningCube,
					//	Matrix.CreateTranslation(mined.Value.position.InWorldSpace(mined.Value.chunk)), sourceRect));
				}
			}

			ProjectileManager.Draw(device);
			EntityManager.Draw(device, null);

			Logic.Draw(this, device);

			drawTime.Stop();
			ChunkDrawTime = drawTime.Elapsed.TotalSeconds;
		}

		public void DrawUI(SpriteBatch batch)
		{
            using var zone = TracyImpl.Tracy.BeginZone();

			GetLocalPlayer()?.DrawUI(batch);

			ChatManager.Draw(batch);
			//DialogueManager.Draw(batch);
		}

		public void OnCubeUpdate(CubePosition updating, ushort updatedId)
		{
            using var zone = TracyImpl.Tracy.BeginZone();

            Logic.OnCubeUpdated(updating, updatedId);

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

		public float GetTimeOfDay(float dawnStartOffsetScale = 1f, float dawnEndOffsetScale = 1f, float duskStartOffsetScale = 1, float duskEndOffsetScale = 1, float timeOffset = 0)
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

			float timeOfDayPercent = ((alive + timeOffset) % DAY_CYCLE_TIME) / DAY_CYCLE_TIME;

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

        public bool TryMineCube(Player? player, CubePosition position, int level, int num, bool instant = false)
		{
            using var zone = TracyImpl.Tracy.BeginZone();

            //debug mode mines instantly
            if (player != null && player.state == Player.State.Noclip)
				instant = true;

			MinedCube mined = new MinedCube()
			{
				position = position,
				chunk = ChunkPosition.CubeChunk(position),
				progress = num,
				timer = 2
			};

			Cube cube = ChunkManager.CubeView.GetCube(position).GetOrDefault(Main.Registry.CubeRegistry.Air);

			if (cube != Main.Registry.CubeRegistry.Air && (level >= cube.MineLevelRequirement || instant))
			{
				if (instant)
				{
					DoMineCube(position, player);

					return true;
				}

				bool doRemove = false;
				if (miningCubes.TryGetValue(position, out var currentMined))
				{
					doRemove = true;
					mined = currentMined with
					{
						progress = currentMined.progress + mined.progress
					};
					miningCubes[position] = mined;
				}
				else
				{
					if (mined.progress < cube.MineProgressToBreak)
						miningCubes.TryAdd(position, mined);
				}

                if (mined.progress >= cube.MineProgressToBreak)
                {
                    if (doRemove)
						miningCubes.Remove(position);

					// Client doesn't get to actually break blocks. Server does it for them
                    DoMineCube(position, player, Main.gameStateManager.netMode != GameStateManager.NetworkingMode.Client);
                    if (player != null && player.IsLocalPlayer && Main.gameStateManager.netMode == GameStateManager.NetworkingMode.Client)
					{
                        var action = new SyncCubeUpdateAuditRequest.AuditedCubeUpdate
						{
							position = position,
							newId = 0,
							oldId = cube.Id,
							player = (byte)localPlayerIndex,
							time = Main.Time,
						};

                        Main.Registry.MessageRegistry.SendMessageToAll(SyncCubeUpdateAuditRequest.Instance, Main.gameStateManager.TheIsland.netManager.netManager, action);
					}

                    return true;
                }
			}

			return false;
		}

		private void DoMineCube(CubePosition position, Player player, bool doDrops = true)
		{
            Cube cube = ChunkManager.CubeView.GetCube(position).GetOrDefault(Main.Registry.CubeRegistry.Air);

            ChunkManager.CubeView.SetCube(position, 0, player);

			if (doDrops)
			{
				List<ItemInstance> items = new List<ItemInstance>();
				cube.GetDrops(items);

				foreach (ItemInstance item in items)
				{
					EntityItem ent = new EntityItem(position.InWorldSpace() + new Vector3(Cube.CUBE_SCALE / 2f),
						new Vector3(Main.random.NextFloat(-Cube.CUBE_SCALE * 5, Cube.CUBE_SCALE * 5), Cube.CUBE_SCALE * 6.4f,
							Main.random.NextFloat(-Cube.CUBE_SCALE * 5, Cube.CUBE_SCALE * 5)), item);

					EntityManager.Add(ent);
				}
			}

            cube.OnMined(player, position);
        }

		public bool PlaceCube(Player? player, CubePosition position, ushort id)
		{
            if (player.world.ChunkLoadManager.IsLoaded(ChunkPosition.CubeChunk(player.PlaceAtPos)))
            {
				ushort oldId = ChunkManager.CubeView.GetId(position);
                ChunkManager.CubeView.SetCube(player.PlaceAtPos, id, player);
                Cube cube = Main.Registry.CubeRegistry.Get(id);
                cube.OnPlayerPlaced(player, player.PlaceAtPos);

                if (player != null && player.IsLocalPlayer && Main.gameStateManager.netMode == GameStateManager.NetworkingMode.Client)
                {
                    var action = new SyncCubeUpdateAuditRequest.AuditedCubeUpdate
                    {
                        position = player.PlaceAtPos,
                        newId = id,
                        oldId = oldId,
                        player = (byte)player.playerIndex,
                        time = Main.Time,
                    };

                    Main.Registry.MessageRegistry.SendMessageToAll(SyncCubeUpdateAuditRequest.Instance, Main.gameStateManager.TheIsland.netManager.netManager, action);
                }

                return true;
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
			isDisposed = true;
			ChunkLoadManager.Dispose();
			LightManager.Dispose();
			Logic.Dispose();

			PhysicsInfo.Simulation.Dispose();
			PhysicsInfo.Properties.Dispose();
			PhysicsInfo.GlobalBufferPool.Clear();
        }

		[ConsoleCommand("set_time", "Sets the world's time. Param 0: time to set to, between 0 and 600 (wraps around), 0 being dawn, 300 being dusk. " +
			"Alternatively, Param 0 can be \"dawn\", \"noon\", \"dusk\", or \"midnight\", for those respective times.")]
		public static void SetTime(string[] parameters)
		{
			if (Main.gameStateManager.GetCurrentGameState() is GameStateTheIsland gsIsland)
			{
				if (IMGUIConsole.RequireParam(parameters, 0, "time"))
				{
					if (int.TryParse(parameters[0], out int timeSetTo))
						gsIsland.GetWorld().alive = timeSetTo;
					else
					{
						if (parameters[0] == "dawn")
							gsIsland.GetWorld().alive = DAY_CYCLE_TIME / 4 * 0;
						else if (parameters[0] == "noon")
							gsIsland.GetWorld().alive = DAY_CYCLE_TIME / 4 * 1;
						else if (parameters[0] == "dusk")
							gsIsland.GetWorld().alive = DAY_CYCLE_TIME / 4 * 2;
						else if (parameters[0] == "midnight")
							gsIsland.GetWorld().alive = DAY_CYCLE_TIME / 4 * 3;
						else IMGUIConsole.LogLine("[error] Time not recognized.");
                    }
				}
			}
            else
            {
                IMGUIConsole.LogLine("[error] give can only be used from within the GameStateTheIsland state. Current state: " + Main.gameStateManager.GetCurrentGameState().ToString());
            }
        }

		[ConsoleCommand("give", "Gives the player an item.")]
		public static void GiveItem(string[] parameters)
		{
			if (Main.gameStateManager.GetCurrentGameState() is GameStateTheIsland gsIsland)
			{
				if (IMGUIConsole.RequireParam(parameters, 0, "item_name"))
				{
					Item item;
					if (int.TryParse(parameters[0], out int itemIndex))
						item = Main.Registry.ItemRegistry.Get(itemIndex);
					else item = Main.Registry.ItemRegistry.Get(parameters[0]);

					if (item != null)
					{
						int num = 1;
						if (parameters.Length >= 2)
						{
							num = int.Parse(parameters[1]);
						}

						int damage = 0;
						if (parameters.Length >= 3)
						{
							damage = int.Parse(parameters[2]);
						}

						World world = gsIsland.GetWorld();
						Player player = world.EntityManager.GetFirst<Player>();

						player.GetInventory().Add(new ItemInstance(item, num, damage));
					}
					else
					{
						IMGUIConsole.LogLine("[error] Tried to give item with name " + parameters[0] + ", but an item by that name did not exist.");
					}
				}
			}
			else 
			{
                IMGUIConsole.LogLine("[error] give can only be used from within the GameStateTheIsland state. Current state: " + Main.gameStateManager.GetCurrentGameState().ToString());
			}
		}

		[ConsoleCommand("list_entities", "Lists all entities. Supply 'spawnable' to parameter 0 to list only entities that are spawnable.")]
		public static void ListEntities(string[] parameters)
		{
			bool listParameterless = false;
			if (parameters != null && parameters.Length > 0 && parameters[0] == "spawnable")
			{
				listParameterless = true;
			}

			foreach (Type entType in Utility.GetTypes<Entity>())
			{
				if (listParameterless && entType.GetConstructor(Type.EmptyTypes) != null)
                    IMGUIConsole.LogLine(entType.Name);
				else 
					IMGUIConsole.LogLine(entType.Name);
			}
		}

		[ConsoleCommand("spawn", "Spawns an entity. Can be spawned on self or at the player's looking position.")]
		public static void SpawnEntity(string[] parameters)
		{
			if (Main.gameStateManager.GetCurrentGameState() is GameStateTheIsland gsIsland)
			{
				if (IMGUIConsole.RequireParam(parameters, 0, "location", ["self", "ray"]))
				{
					string location = parameters[0];

					IMGUIConsole.RequireParam(parameters, 1, "entity");

					string entityName = parameters[1];

					Type entityType = Utility.GetType(Assembly.GetExecutingAssembly().GetName().Name, entityName);

					if (entityType == null)
					{
                        IMGUIConsole.LogLine("[error] Entity " + entityName + " does not exist!");
						return;
                    }
				
					if (entityType.GetConstructor(Type.EmptyTypes) == null)
					{
						IMGUIConsole.LogLine("[error] Entity " + entityName + " exists, but has no parameterless constructor, and cannot be spawned.");
						return;
					}

					var created = Activator.CreateInstance(entityType);

					if (created != null && created is Entity ent)
					{
						if (location == "self")
						{
							ent.Position = gsIsland.GetWorld().EntityManager.GetFirst<Player>().Position;
						}
						else if (location == "ray")
						{
							CubePosition lookAt = gsIsland.GetWorld().EntityManager.GetFirst<Player>().LookAtPos;

							ent.Position = (lookAt + new CubePosition(0, 1, 0)).InWorldSpace();
						}

						gsIsland.GetWorld().EntityManager.Add(ent);
					}
				}
			}
			else
			{
				IMGUIConsole.LogLine("[error] spawn_entity can only be used from within the GameStateTheIsland state. Current state: " + Main.gameStateManager.GetCurrentGameState().ToString());
			}
		}
	}
}
