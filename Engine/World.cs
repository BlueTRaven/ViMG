using BepuPhysics;
using BepuPhysics.Constraints;
using BepuUtilities.Memory;
using BrUtility;
using Engine.Clients;
using Engine.Common;
using Engine.Items;
using Engine.Networking.Messages;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
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
        [ConsoleCommandVar("sv_sync_time", "Amount of time between state syncs. Default = 1 / 20")]
        public static float SyncTime = 1.0f / 20.0f;

        public readonly string LoadedFolderName;
		public readonly int Layer;

		public const float GRAVITY = -9.8f / 20f * Cube.CUBE_SCALE;
		public const float DAY_CYCLE_TIME = 60f * 10f;

		public readonly int sizeInChunks;
		public readonly int sizeInCubes;

		public ChunkManager ChunkManager;

		public Skybox Skybox;
		public float WeatherSkyboxAlpha;
		public Color WeatherSkyboxColor;

		public const int MAX_PLAYERS = 4;
		public Player?[] player = new Player[4];
		// TODO: get rid of localPlayerIndex
		// Client will be handled with the dumb client, therefore the server will have no concept of a "local player"
		public int localPlayerIndex;

		public int DrawDistanceHoriz = 6;   //radius in chunks that we should be able to see
		public int DrawDistanceVert = 6;
		public int DrawRadius = 6;

		public float TimeScale = 1f;

		public HitboxManager HitboxManager = new HitboxManager(32);
		public ProjectileManager ProjectileManager;
		public EntityManager EntityManager;
		public InventoryManager InventoryManager;
		public LightManager2 LightManager2;
		//public LightManager? LightManager;
		public PassiveSpawnerManager PassiveSpawnerManager;
		public WorldInfoIO.WorldInfo WorldInfo;
		public ChunkLoadManager ChunkLoadManager;
		public PhysicsInfo PhysicsInfo;
		public ChatManager ChatManager;
		public MenuDialogue MenuDialogue;
		//public DialogueManager DialogueManager;
		public CubeBreakProgressTracker CubeProgressTracker;

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

        private double lastSyncTime;
		private double lastAutosaveTime;

		public List<Player> PlayerRespawnedEvent = new List<Player>();
		private float randomUpdatesTimer;

		private int nextLayer;
		private Task<World> nextWorld;

        private float alive;

        public World(WorldPrototype prototype, ChunkLoadManager chunkLoadManager, 
			WorldInfoIO winfoIO, EntityManagerIO entityIO, ChunkManagerIO chunkIO, int worldSize)
		{
            using var zone = TracyImpl.Tracy.BeginZone();

            this.Layer = prototype.Layer;
			this.LoadedFolderName = prototype.WorldName;

			ChunkManager = prototype.ChunkManager;
			EntityManager = prototype.EntityManager;
			InventoryManager = prototype.InventoryManager;
			WorldInfo = prototype.WorldInfo;
			Skybox = prototype.Skybox;
			Logic = prototype.Logic;
			PhysicsInfo = prototype.PhysicsInfo;

			HousingManager = prototype.HousingManager;

			//DialogueManager = new DialogueManager();

			CubeProgressTracker = new CubeBreakProgressTracker();

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

            LightManager2 = new LightManager2();
        }

        public void InitMeshes(GraphicsDevice device)
        {
            ChatManager = new ChatManager(new Vector2(8, Options.CurrentWindowResolution.Y - 256));
            MenuDialogue = new MenuDialogue(Main.gameStateManager);

            //LightManager = new LightManager(device);
		}

		public void FinishLoading(GraphicsDevice device)
        {
            using var zone = TracyImpl.Tracy.BeginZone();

            //The player reference will not be set up after loading. We need to do that ourselves.
			foreach (Player p in EntityManager.GetAll<Player>())
			{
				player[p.playerIndex] = p;
			}
			localPlayerIndex = Main.gameStateManager.TheIsland.netManagerClient?.whoAmI ?? 0;
			// -1 means singleplayer
			if (localPlayerIndex == -1) localPlayerIndex = 0;

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

		public void Update(double deltaTime)
		{
			LightManager2.Reset();

            //EntIO.TestConsistency(GetLocalPlayer());
            using var zone = TracyImpl.Tracy.BeginZone();

			if (Main.Time - lastSyncTime > SyncTime)
			{
				EntityManager.UpdateNetwork();
				InventoryManager.UpdateNetwork(player);
				SyncProjectile.Instance.DoSend();
				SyncCubeAction.Instance.DoSend();
				SyncWorldState.Instance.DoSend();
				lastSyncTime = Main.Time;
            }

			// Autosave every 5 minutes?
			if (Main.Time - lastAutosaveTime > 60 * 5)
			{
				
				Main.gameStateManager.TheIsland.Save(true);
				lastAutosaveTime = Main.Time;
			}

            deltaTime *= TimeScale;

            PhysicsInfo.Simulation.Timestep((float)deltaTime);

			alive += (float)deltaTime;

            CubeProgressTracker.Update(ChunkManager.CubeView, deltaTime);

            ChatManager.Update(deltaTime);
			//DialogueManager.Update(deltaTime);

			ChunkManager.Update(deltaTime, this);
			//ChunkManager.ProcessChunkQueue(this, 0);
			ChunkLoadManager.Update(deltaTime, this);

			ProjectileManager.Update(deltaTime);
			EntityManager.Update(deltaTime);

			// TODO: hacky
			// Players never automatically send major syncs, we always have to send those manually.
			// When a player is connected, one is sent for every entity already present on the server.
			// When a player respawns, it's a bit more complicated, and basically requires us to defer this until
			// after the Player has been ReallyAdded/init.
			// This is maybe just straight up bad. Maybe we should just allow players to automatically major sync?
			// This would fix two issues with one stone, removing the special case path for creating players when connecting
			// and this bullshit when a player respawns.
			foreach (Player player in PlayerRespawnedEvent)
			{
                if (Main.gameStateManager.netMode != GameStates.GameStateManager.NetworkingMode.Client)
				{
                    Player p = new Player(player);
                    EntityManager.ForceAdd(p);
                    this.player[player.playerIndex] = p;
				}
			}
			PlayerRespawnedEvent.Clear();

            SyncPlayerInputs.Instance.Apply(player);

			Logic.Update(this, deltaTime);

			if (Main.ENABLE_RANDOM_UPDATES)
			{
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
			}

			PassiveSpawnerManager?.Update(deltaTime, this);

			// TODO
			//TryLoadNextLayer();
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

            IMGUIConsole.Assert(Main.gameStateManager.netMode != GameStateManager.NetworkingMode.Client);

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

		public Player? GetClosestPlayer(Vector3 position)
		{
			Player? closestPlayer = null;
            float closestDistance = float.MaxValue;
            foreach (Player? player in player)
			{
				if (player != null)
				{
					var dist = (player.Position - position).Length();

                    if (dist < closestDistance)
					{
						closestDistance = dist;
						closestPlayer = player;
					}
                }
			}

			return closestPlayer;
		}

        public float DistanceFromPlayer(Player player, Vector3 position)
        {
            return (player.Position - position).Length();
        }

        // Gets the distance from the closest player
        public float DistanceFromPlayer(Vector3 position)
		{
			float closestDistance = float.MaxValue;

			foreach (Player? player in player)
			{
				if (player != null)
				{
					closestDistance = float.Min(closestDistance, (player.Position - position).Length());
				}
			}

			return closestDistance;
        }

		public static int NumChunksDrawn;
		public static double ChunkDrawTime;

		public void DrawDebug(GraphicsDevice device)
		{
			if (Main.Debug)
				HousingManager.DrawDebug(this, device);
		}

        public void Draw(GraphicsDevice device)
		{
            using var zone = TracyImpl.Tracy.BeginZone();

			Stopwatch drawTime = Stopwatch.StartNew();

			//foreach (var mined in miningCubes)
			//{
			//	Cube cube = ChunkManager.CubeView.GetCube(mined.Value.position).GetOrDefault(Main.Registry.CubeRegistry.Air);

			//	if (cube != Main.Registry.CubeRegistry.Air)
			//	{
			//		float percent = (float)mined.Value.progress / (float)cube.MineProgressToBreak;

			//		float stepped = ((int)(percent * 8f)) / 8f;

			//		RectangleF sourceRect = new RectangleF(128f * stepped, 0, 16, 16);

			//		RendererDeferred.DrawMaterial material = new RendererDeferred.DrawMaterial("mine");
			//		//Main.Renderer.AddOpaqueDraw(new Rendering.RendererDeferred.GBufferDraw(material, meshMiningCube,
			//		//	Matrix.CreateTranslation(mined.Value.position.InWorldSpace(mined.Value.chunk)), sourceRect));
			//	}
			//}

			//ProjectileManager.Draw(device);

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

			Cube cube = ChunkManager.CubeView.GetCube(position).GetOrDefault(Main.Registry.CubeRegistry.Air);

			if (cube != Main.Registry.CubeRegistry.Air && (level >= cube.MineLevelRequirement || instant))
			{
				if (instant)
				{
					DoMineCube(position, player);
					CubeProgressTracker.RemoveProgress(position);

					return true;
				}

				if (CubeProgressTracker.AddProgress(ChunkManager.CubeView, position, num))
				{
                    DoMineCube(position, player, Main.gameStateManager.netMode != GameStateManager.NetworkingMode.Client);

                    return true;
                }

				SyncCubeUpdate.Instance.SendCubeUpdate(position, player?.playerIndex ?? -1, CubeProgressTracker.GetProgress(position));
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

                //if (player != null && player.IsLocalPlayer && Main.gameStateManager.netMode == GameStateManager.NetworkingMode.Client)
                //{
                //    var action = new SyncCubeUpdateAuditRequest.AuditedCubeUpdate
                //    {
                //        position = player.PlaceAtPos,
                //        newId = id,
                //        oldId = oldId,
                //        player = (byte)player.playerIndex,
                //        time = Main.Time,
                //    };

                //    Main.gameStateManager.TheIsland.netManagerServer?.SendMessageToAll(SyncCubeUpdateAuditRequest.Instance, Main.gameStateManager.TheIsland.netManagerServer?.netManager, action);
                //}

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
			//LightManager.Dispose();
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
				if (IMGUIConsole.RequireParam(parameters, 0, "player_name"))
				{
					World world = gsIsland.GetWorld();
					var netPlayer = gsIsland.netManagerServer?.GetNetPlayerByName(parameters[0]) ?? new();
					Player? player = world.player.First(x => x?.playerIndex == netPlayer.playerId);
					if (player != null)
					{
						if (IMGUIConsole.RequireParam(parameters, 1, "item_name"))
						{
							Item item;
							if (int.TryParse(parameters[1], out int itemIndex))
								item = Main.Registry.ItemRegistry.Get(itemIndex);
							else item = Main.Registry.ItemRegistry.Get(parameters[1]);

							if (item != null)
							{
								int num = 1;
								if (parameters.Length >= 3)
								{
									num = int.Parse(parameters[2]);
								}

								int damage = 1;
								if (parameters.Length >= 4)
								{
									damage = int.Parse(parameters[3]);
								}

								world.InventoryManager.Get(player.inventory)?.Add(new ItemInstance(item, num, damage));
							}
							else
							{
								IMGUIConsole.LogLine("[error] Tried to give item with name " + parameters[1] + ", but an item by that name did not exist.");
							}
						}
					}
					else
					{
                        IMGUIConsole.LogLine("[error] Tried to get player with name " + parameters[0] + ", but a player by that name did not exist.");
                    }
                }
				else
				{
					IMGUIConsole.LogLine("[error] give can only be used from within the GameStateTheIsland state. Current state: " + Main.gameStateManager.GetCurrentGameState().ToString());
				}
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
