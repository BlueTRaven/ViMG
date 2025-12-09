using BepuUtilities.Memory;
using Engine.Networking;
using Engine.Networking.Messages;
using LiteNetLib;
using LiteNetLib.Utils;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Schema;
using ViMG.IMGUIImpl;
using static ViMG.UIs.UI;

namespace ViMG.Entities
{
	public class EntityManager
	{
		[ConsoleCommandVar("ent_max", "Maximum numbere of entities the server can have active at once. Entities allocated in excess of this number will be immediately destroyed.\n" +
			"Changes to this variable require a restart.")]
		public static int EntMax = 4096;
		[ConsoleCommandVar("ent_sync_time", "Amount of time between entity state syncs. Default = 1 / 20")]
		public static float EntSyncTime = 1.0f / 4.0f; // 1.0f / 20.0f;

		public struct EntityReference : INetSerializable
		{
			public int id;
			public int generation;

            public void Deserialize(NetDataReader reader)
            {
				id = reader.GetInt();
				generation = reader.GetInt();
            }

            public void Serialize(NetDataWriter writer)
            {
				writer.Put(id);
				writer.Put(generation);
            }
        }

		private struct EntityHolder
		{
			public int id;
			public int generation;
			public bool active;
			public Entity? entity;

			public static EntityHolder DEFAULT = new()
			{
				id = -1,
				generation = -1,
				active = false,
				entity = null,
			};

			public void Reset()
			{
				generation = (generation + 1) % int.MaxValue;
				entity = null;
				active = false;
			}
		}

        public struct EntityIterator : IEnumerator<Entity>, IEnumerable<Entity>
        {
			public Entity Current => manager.ents[currentIndex].entity;

            object IEnumerator.Current => Current;

			private EntityManager manager;
			private int currentIndex;

			public EntityIterator(EntityManager manager)
			{
				this.manager = manager;
				currentIndex = -1;
			}

            public void Dispose()
            {
				currentIndex = -1;
				manager = null;
            }

            public bool MoveNext()
            {
				while (true) 
				{
					currentIndex += 1;

					if (currentIndex >= EntMax) return false;
					if (manager.ents[currentIndex].active) return true;
				}
            }

            public void Reset()
            {
				currentIndex = -1;
            }

            public IEnumerator<Entity> GetEnumerator()
            {
				return this;
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        public event Action<Entity> OnEntityAdded;
		public event Action<Entity> OnEntityRemoved;

		private bool iteratingUpdate;

		private EntityHolder[] ents;
		// NOTE:
		// On the client side, things might have to be a bit different.
		// We will basically never create an entity with a new id. The only scenario in which this will happen is if we're creating
		// a client-side only entity.
		// We might want to maintain a separate free list specifically for that, and this one will remain empty on the client side.
		private List<int> freeList = new List<int>();

		private Dictionary<Type, List<Entity>> entitiesByType = new Dictionary<Type, List<Entity>>();

		private List<Entity> toAddLater = new List<Entity>();
		private HashSet<Entity> toDeleteLater = new HashSet<Entity>();

		private class CubeTrackers
		{
			public ICubeTracker[] cubeTrackers;
			public IMultiCubeTracker[] multiCubeTrackers;

			public ChunkPosition chunkPosition;

			public int count;

			public void Add(CubePosition chunkSpacePosition, Entity entity)
			{
				Util.ThreeDToOneD(new ValuePoint3D(chunkSpacePosition.X, chunkSpacePosition.Y, chunkSpacePosition.Z), new ValuePoint3D(Chunk.CHUNK_SIZE), out int i);

				if (entity is ICubeTracker tracker)
				{
					if (cubeTrackers == null)
						cubeTrackers = new ICubeTracker[Chunk.NUM_CUBES_IN_CHUNK];

					if (cubeTrackers[i] == null)
						cubeTrackers[i] = tracker;
                    else throw new Exception(string.Format("ICubeTracker {0} already present at position {1} while trying to place CubeTracker {2}", 
						cubeTrackers[i], chunkSpacePosition.InCubeSpace(chunkPosition), entity));
                }
				else if (entity is IMultiCubeTracker multiTracker)
				{
					if (multiCubeTrackers == null)
						multiCubeTrackers = new IMultiCubeTracker[Chunk.NUM_CUBES_IN_CHUNK];

					if (multiCubeTrackers[i] == null)
						multiCubeTrackers[i] = multiTracker;
                    else throw new Exception(string.Format("IMultiCubeTracker {0} already present at position {1} while trying to place CubeTracker {2}",
                        multiCubeTrackers[i], chunkSpacePosition.InCubeSpace(chunkPosition), entity));
                }

				count++;
			}

			public void Remove(CubePosition chunkSpacePosition)
			{
                Util.ThreeDToOneD(new ValuePoint3D(chunkSpacePosition.X, chunkSpacePosition.Y, chunkSpacePosition.Z), new ValuePoint3D(Chunk.CHUNK_SIZE), out int i);

                bool isSingle = cubeTrackers != null && cubeTrackers[i] != null;
				bool isMulti = multiCubeTrackers != null && multiCubeTrackers[i] != null;

				if (isSingle && isMulti || (cubeTrackers?[i] == null && multiCubeTrackers?[i] == null))
					throw new Exception("???");

                if (isSingle)
					cubeTrackers[i] = null;
				else if (isMulti)
					multiCubeTrackers[i] = null;

				count--;
            }

			public Entity Get(CubePosition chunkSpacePosition)
			{
                Util.ThreeDToOneD(new ValuePoint3D(chunkSpacePosition.X, chunkSpacePosition.Y, chunkSpacePosition.Z), new ValuePoint3D(Chunk.CHUNK_SIZE), out int i);

                bool isSingle = cubeTrackers != null && cubeTrackers[i] != null;
                bool isMulti = multiCubeTrackers != null && multiCubeTrackers[i] != null;

				if (isSingle)
					return cubeTrackers[i] as Entity;
				else if (isMulti)
					return multiCubeTrackers[i] as Entity;
				else return null;
            }
		}
		private int sizeInCubes;
		private Dictionary<ChunkPosition, CubeTrackers> cubeTrackers = new Dictionary<ChunkPosition, CubeTrackers>();

		private World world;

		private double lastSyncTime;

		public int GetUniqueId()
		{
			if (freeList.Count == 0) return -1;
			int last = freeList.Last();
			freeList.RemoveAt(freeList.Count - 1);

			return last;
			//return lastEntityId++;
		}

		[Obsolete]
		public void SetUniqueIdSeed(ulong seed)
		{
			//lastEntityId = seed;
		}

		public EntityManager()
		{
			ents = new EntityHolder[EntMax];
			Array.Fill(ents, EntityHolder.DEFAULT);

			// TODO: we might want to only generate this on server-side
			for (int i = EntMax - 1; i >= 0; i--)
			{
				freeList.Add(i);
			}

			Debug.Assert(freeList.First() == EntMax - 1);
		}

		public void Initialize(World world)
        {
			this.world = world;

			this.sizeInCubes = world.sizeInCubes;

			if (!Main.IsHeadless)
			{
				foreach (var r in Main.Registry.RendererRegistry.GetIterable())
				{
					if (r != null)
						r.NewEntityManagerInitialized(this);
				}
			}
        }

		public void Dispose()
		{
			if (!Main.IsHeadless)
			{
				foreach (var r in Main.Registry.RendererRegistry.GetIterable())
				{
					if (r != null)
						r.EntityManagerDisposed(this);
				}
			}

            UnloadAll();
		}

		public void ForceAdd(Entity entity, ulong id)
		{
			if (iteratingUpdate)
				throw new Exception("Cannot add while iterating");

			// TODO: might not have to do this on client side.
			if (ents[id].active) ForceUnload(ents[id].entity);
			else freeList.Remove((int)id);
            entity.SetId(id);

            ReallyAdd(entity);
		}

		public void ForceAdd(Entity entity)
		{
			if (iteratingUpdate)
				throw new Exception("Cannot add while iterating");

			int id = GetUniqueId();
			if (id == -1)
			{
				Console.WriteLine("Entity free list empty. Could not create entity {0}", entity);
				entity.OnUnload();
				return;
			}

            entity.SetId((ulong)id);

			ReallyAdd(entity);
        }

        public void Add(Entity entity, bool delayAdding = false)
		{
			// Shouldn't add entities if not server or singleplayer?
			// What about player entities...?
			//Debug.Assert(Main.gameStateManager.netMode != GameStates.GameStateManager.NetworkingMode.Client, "Created entity on client", "Tried to create entity {0} on client", entity.ToString());
			if (Main.gameStateManager.netMode == GameStates.GameStateManager.NetworkingMode.Client)
			{
				Console.WriteLine("Tried to create entity {0} on client", entity.ToString());

                return;
			}

            int id = GetUniqueId();
            if (id == -1)
            {
                Console.WriteLine("Entity free list empty. Could not create entity {0}", entity);
                entity.OnUnload();
                return;
            }
            entity.SetId((ulong)id);

            if (iteratingUpdate || delayAdding)
				toAddLater.Add(entity);
			else ReallyAdd(entity);
		}

		private void ReallyAdd(Entity entity)
		{
			if (iteratingUpdate)
				throw new Exception("Cannot add while iterating");

			Debug.Assert(!ents[entity.Id].active);
			ents[entity.Id] = new EntityHolder
			{
				active = true,
				entity = entity,
				generation = (ents[entity.Id].generation + 1) % int.MaxValue,
				id = (int)entity.Id,
			};

			if (!entitiesByType.ContainsKey(entity.GetType()))
				entitiesByType.Add(entity.GetType(), new List<Entity>());
			entitiesByType[entity.GetType()].Add(entity);

            entity.Initialize(world);
            if (!Main.IsHeadless)
            {
                entity.LoadContent(world);
            }

            if (entity is ICubeTracker tracker)
			{
				CubePosition position = tracker.TrackedPosition;

				ChunkPosition chunkPos = ChunkPosition.CubeChunk(position);

				if (cubeTrackers.ContainsKey(chunkPos))
					cubeTrackers[chunkPos].Add(position.InChunkSpace(chunkPos), entity);
				else
				{
					CubeTrackers ts = new CubeTrackers();
					ts.chunkPosition = chunkPos;
					ts.Add(position.InChunkSpace(chunkPos), entity);

					cubeTrackers.Add(chunkPos, ts);
				}
			}

			if (entity is IMultiCubeTracker multiTracker)
			{
				foreach (CubePosition position in multiTracker.TrackedPositions)
				{
					ChunkPosition chunkPos = ChunkPosition.CubeChunk(position);

					if (cubeTrackers.ContainsKey(chunkPos))
						cubeTrackers[chunkPos].Add(position.InChunkSpace(chunkPos), entity);
					else
					{
						CubeTrackers ts = new CubeTrackers();
						ts.chunkPosition = chunkPos;
						ts.Add(position.InChunkSpace(chunkPos), entity);

						cubeTrackers.Add(chunkPos, ts);
					}
				}
			}

            OnEntityAdded?.Invoke(entity);

			//if (Main.gameStateManager.netMode == GameStates.GameStateManager.NetworkingMode.Server)
			//{
			//	var entSerializableAttr = entity.GetType().GetCustomAttribute<EntitySerializableAttribute>();
			//	if (entSerializableAttr != null)
			//	{
			//		if ((entSerializableAttr.serializationType & EntitySerializableAttribute.SerializationType.Server) == EntitySerializableAttribute.SerializationType.Server)
			//		{
			//			EntityManagerIO.EntityData data = new(entity);
			//			Console.WriteLine("To sync create {0}", data.type);
			//			SyncEntityState.Instance.AddToSync(-1, GetReference(entity), data);
			//			// Send a full sync when entity is created. Disregard TimeSynced
			//			//var ent = new SyncBasicState.SyncEntity()
			//			//{
			//			//	entity = entity,
			//			//	firstCreation = true,
			//			//	type = SyncBasicState.SyncType.FullSync,
			//			//};
			//			//Main.Registry.MessageRegistry.SendMessageToAll(SyncBasicState.Instance, Main.gameStateManager.TheIsland.netManager.netManager, ent);
			//		}
			//	}
			//}
        }

		public void Remove(Entity entity)
		{
			toDeleteLater.Add(entity);
			entity.OnDelete();
		}

		public void Unload(Entity entity, bool delay = false)
        {
			if (iteratingUpdate || delay)
				toDeleteLater.Add(entity);
			else ReallyRemove(entity);
        }

		public void ForceUnload(Entity entity)
		{
			ReallyRemove(entity);
		}

		public void Unload(ChunkPosition pos)
        {
            //TODO: better method of determining which entities are in this chunk for unloading

			//Initial flush to remove entities that are already queued to be deleted.
			//This is so that we don't have to check for entities that are already queued when trying to unload.
            foreach (Entity entity in toDeleteLater)
            {
                ReallyRemove(entity);
            }

			toDeleteLater.Clear();

            //queue all entities in chunk to be unloaded

            for (int i = 0; i < EntMax; i++)
            {
                if (ents[i].active && (ents[i].entity is not Player || world.isDisposed)) // Players cannot be unloaded normally
				{
					if (ChunkPosition.WorldSpaceChunk(ents[i].entity.Position) == pos)
						Unload(ents[i].entity, true);
				}
			}

			//Another flush, to remove any entities that are newly added to the queue...
			//(aka any entity with a position inside the chunk.)
			foreach (Entity entity in toDeleteLater)
			{
				ReallyRemove(entity);
			}

			toDeleteLater.Clear();

			//Some entities may track a cube inside a given chunk while not being in the chunk themselves.
			//(For instance, at the time of writing, AncientAltar's y position is + 1.25 blocks above the tracked position. If this
			//is on a chunk boundary, then it isn't within the same chunk as the cube it's tracking!)
			if (cubeTrackers.ContainsKey(pos))
			{
				CubeTrackers tracker = cubeTrackers[pos];
				if (tracker.cubeTrackers != null)
				{
					for (int i = 0; i < Chunk.NUM_CUBES_IN_CHUNK; i++)
						if (tracker.cubeTrackers[i] != null)
							Unload(tracker.cubeTrackers[i] as Entity, true);
				}

				if (tracker.multiCubeTrackers != null)
				{
                    for (int i = 0; i < Chunk.NUM_CUBES_IN_CHUNK; i++)
                        if (tracker.multiCubeTrackers[i] != null)
                            Unload(tracker.multiCubeTrackers[i] as Entity, true);
                }

				//flush the queue again
				//we do these in separate flushes.
                foreach (Entity entity in toDeleteLater)
                {
                    ReallyRemove(entity);
                }
				
				toDeleteLater.Clear();
            }

			//...and flush toAddLater, since we don't want to unload the chunk, then spawn it.
			for (int i = toAddLater.Count - 1; i >= 0; i--)
            {
				Entity entity = toAddLater[i];

				if (ChunkPosition.WorldSpaceChunk(entity.Position) == pos)
					toAddLater.RemoveAt(i);
            }
		}

		public void UnloadAll()
		{
			//TODO: better method of determining which entities are in this chunk for unloading

			//queue all entities to be unloaded
			for (int i = 0; i < EntMax; i++)
			{
				if (ents[i].active && (ents[i].entity is not Player || world.isDisposed || world.isCreateWorldReloading)) // Players cannot be unloaded normally
				{
					if (!toDeleteLater.Contains(ents[i].entity))
						Unload(ents[i].entity, true);
				}
			}

			//Now remove them, and whatever else was in the queue...
			foreach (Entity entity in toDeleteLater)
			{
				ReallyRemove(entity);
			}

			toDeleteLater.Clear();

			//also clear toAddLater so we don't end up adding some entities after
			toAddLater.Clear();
		}

		public void Update(double deltaTime)
		{
            using var zone = TracyImpl.Tracy.BeginZone();

            AddLaterEntities();

			iteratingUpdate = true;

			for (int i = 0; i < EntMax; i++)
			{
				if (ents[i].active && !ents[i].entity.Dead)
				{
					try
					{
						ents[i].entity.Update(deltaTime);
					}
					catch (Exception e)
					{
						Console.WriteLine("Entity {0} (id {1}) caused an error during Update. It has been removed.\n{2}", ents[i].entity, ents[i].id, e.ToString());
					}
				}
			}

			iteratingUpdate = false;

			foreach (Entity entity in toDeleteLater)
			{
				ReallyRemove(entity);
			}

			toDeleteLater.Clear();

            if (Main.gameStateManager.netMode != GameStates.GameStateManager.NetworkingMode.Singleplayer)
                UpdateNetwork();
		}

		private void UpdateNetwork()
		{
			if (Main.gameStateManager.netMode == GameStates.GameStateManager.NetworkingMode.Server)
			{
				SyncEntityState.Instance.DoSync(this, world.player);
				//if (Main.Time - lastSyncTime > EntSyncTime)
				//{
				//	foreach (Player player in world.player)
				//	{
				//		if (player == null || !player.IsInitialized || player.IsLocalPlayer)
				//			continue;

				//		var peer = Main.gameStateManager.TheIsland.netManager.GetPeer(player.playerIndex);

				//		if (peer == null)
				//		{
				//			Console.WriteLine("Peer null");
				//			continue;
				//		}

				//		var iter = GetEntities();

				//		foreach (var ent in iter)
				//		{
				//			if (ent is Player)
				//				continue;

				//			var entSerializableAttr = ent.GetType().GetCustomAttribute<EntitySerializableAttribute>();
				//			if (entSerializableAttr != null)
				//			{
				//				if ((entSerializableAttr.serializationType & EntitySerializableAttribute.SerializationType.Server) == EntitySerializableAttribute.SerializationType.Server)
				//				{
				//					if (ent.DoesMajorSync && Main.Time - ent.TimeMajorSynced > ent.MajorSyncInterval)
				//					{
				//						var entData = new EntityManagerIO.EntityData(ent);
				//						SyncEntityState.Instance.AddToSync(player.playerIndex, GetReference(ent), entData);
				//					}
				//					else if (ent.DoesSync && ent is ISyncBasicState syncsBasicState)
				//						SyncEntityState.Instance.AddToSync(player.playerIndex, GetReference(ent), syncsBasicState);
				//				}
				//			}
				//		}

				//		Main.Registry.MessageRegistry.SendMessageToPeer(SyncEntityState.Instance, peer, player.playerIndex);
				//	}
					
				//	lastSyncTime = Main.Time;
				//	SyncEntityState.Instance.PostSend();
				//}
			}


			// Update entities (excluding player)
			//if (Main.gameStateManager.netMode == GameStates.GameStateManager.NetworkingMode.Server)
			//{
   //             for (int i = 0; i < EntMax; i++)
   //             {
   //                 if (ents[i].active && ents[i].entity is not Player && ents[i].entity.IsInitialized)
			//		{
			//			var entity = ents[i].entity;

   //                     var entSerializableAttr = entity.GetType().GetCustomAttribute<EntitySerializableAttribute>();
			//			if (entSerializableAttr != null)
			//			{
			//				if ((entSerializableAttr.serializationType & EntitySerializableAttribute.SerializationType.Server) == EntitySerializableAttribute.SerializationType.Server)
			//				{
			//					if (entity.DoesMajorSync && Main.Time - entity.TimeMajorSynced > entity.MajorSyncInterval)
			//					{
			//						var ent = new SyncBasicState.SyncEntity()
			//						{
			//							entity = entity,
			//							type = SyncBasicState.SyncType.FullSync,
			//						};
			//						Main.Registry.MessageRegistry.SendMessageToAll(SyncBasicState.Instance, Main.gameStateManager.TheIsland.netManager.netManager, ent);
			//					}
			//					else
			//					{
			//						if (entity.DoesSync && Main.Time - entity.TimeSynced > entity.SyncInterval)
			//						{
			//							var ent = new SyncBasicState.SyncEntity()
			//							{
			//								entity = entity,
			//								type = SyncBasicState.SyncType.BasicState,
			//							};
			//							Main.Registry.MessageRegistry.SendMessageToAll(SyncBasicState.Instance, Main.gameStateManager.TheIsland.netManager.netManager, ent);
			//						}
			//					}
			//				}
			//			}
			//		}
			//	}
			//}

			// Handle syncing players separately from normal entities.
			// This is mainly because of two factors:
			// Clients send their player back to the server (client authoratative over its own player)
			// and Servers will send player data to all clients but to the client whose player it represents
			var localPlayer = world.GetLocalPlayer();
			if (localPlayer != null && localPlayer.TimeInitialized != 0)
			{
				// Local player has all its inputs synced to all connections
				if (localPlayer.LeftClick.Changed() || localPlayer.RightClick.Changed() ||
                    localPlayer.MoveLeft.Changed() || localPlayer.MoveRight.Changed() ||
                    localPlayer.MoveForward.Changed() || localPlayer.MoveBack.Changed() ||
                    localPlayer.Jump.Changed() || localPlayer.Run.Changed() ||
                    localPlayer.MoveDown.Changed() || Main.camera.IsDirty || Main.Time - localPlayer.TimeSinceInputSynced > 0.25)
				{
					Main.Registry.MessageRegistry.SendMessageToAll(SyncPlayerInputs.Instance, Main.gameStateManager.TheIsland.netManager.netManager, null);
				}
			}

			if (Main.gameStateManager.netMode == GameStates.GameStateManager.NetworkingMode.Server)
			{
				// Sync players to other players.
				// SyncPlayerConnected only tells us that other players are connected.
				// We need to send entity serialization info continually.
				foreach (var player in world.player)
				{
					if (player != null && player.TimeInitialized != 0)
					{
						NetPeer peer = Main.gameStateManager.TheIsland.netManager.GetPeer(player.playerIndex);
						if (Main.Time - player.TimeMajorSynced > player.MajorSyncInterval)
						{
							var ent = new SyncBasicState.SyncEntity()
							{
								entity = player,
								type = SyncBasicState.SyncType.FullSync,
							};
							Main.Registry.MessageRegistry.SendMessageToAll(SyncBasicState.Instance, Main.gameStateManager.TheIsland.netManager.netManager, ent, peer);
						}
						else
						{
							if (Main.Time - player.TimeSynced > player.SyncInterval)
							{
								var ent = new SyncBasicState.SyncEntity()
								{
									entity = player,
									type = SyncBasicState.SyncType.BasicState,
								};
								Main.Registry.MessageRegistry.SendMessageToAll(SyncBasicState.Instance, Main.gameStateManager.TheIsland.netManager.netManager, ent, peer);
							}
						}
					}
				}
			}
		}

		public void AddLaterEntities()
        {
			foreach (Entity entity in toAddLater)
			{
				ReallyAdd(entity);
			}

			toAddLater.Clear();
		}

		private void ReallyRemove(Entity entity)
        {
			if (entity == null)
				return;

			if (iteratingUpdate)
				throw new Exception("Cannot remove entity while iterating");

			if (Main.gameStateManager.netMode == GameStates.GameStateManager.NetworkingMode.Server && entity is not Player)
			{
				if (entity is not Player)
				{
					var entSerializableAttr = entity.GetType().GetCustomAttribute<EntitySerializableAttribute>();
					if (entSerializableAttr != null)
					{
						if ((entSerializableAttr.serializationType & EntitySerializableAttribute.SerializationType.Server) == EntitySerializableAttribute.SerializationType.Server)
						{
							var ent = new SyncBasicState.SyncEntity()
							{
								entity = entity,
								type = SyncBasicState.SyncType.EntityUnloaded,
							};
							Main.Registry.MessageRegistry.SendMessageToAll(SyncBasicState.Instance, Main.gameStateManager.TheIsland.netManager.netManager, ent);
						}
					}
				}
			}

			entity.OnUnload();
			//entities.Remove(entity);
			ents[entity.Id].Reset();
			freeList.Add((int)entity.Id);

			if (entitiesByType.ContainsKey(entity.GetType()))
				entitiesByType[entity.GetType()].Remove(entity);

			//entitiesById.Remove(entity.Id);

			if (entity is ICubeTracker tracker)
            {
				CubePosition position = tracker.TrackedPosition;
				ChunkPosition chunkPos = ChunkPosition.CubeChunk(position);

				if (cubeTrackers.ContainsKey(chunkPos))
				{
					CubeTrackers ts = cubeTrackers[chunkPos];
					ts.Remove(position.InChunkSpace(chunkPos));

					if (ts.count <= 0)
						cubeTrackers.Remove(chunkPos);
				}
            }

			if (entity is IMultiCubeTracker multiTracker)
			{
				foreach (CubePosition position in multiTracker.TrackedPositions)
				{
                    ChunkPosition chunkPos = ChunkPosition.CubeChunk(position);

                    if (cubeTrackers.ContainsKey(chunkPos))
                    {
                        CubeTrackers ts = cubeTrackers[chunkPos];
                        ts.Remove(position.InChunkSpace(chunkPos));

                        if (ts.count <= 0)
                            cubeTrackers.Remove(chunkPos);
                    }
                }
			}

			OnEntityRemoved?.Invoke(entity);
		}

		public bool GetActive(int id) => ents[id].active;

		public Entity? GetById(ulong id)
		{
			return ents[(int)id].entity;
            //if (entitiesById.TryGetValue(id, out Entity ent))
            //    return ent;
            //else return null;
        }

		public Entity? GetByRef(ref readonly EntityReference reference)
		{
			if (ents[reference.id].generation == reference.generation)
				return ents[reference.id].entity;
			else return null;
		}

		public T? GetById<T>(ulong id) where T : Entity
		{
            return ents[(int)id].entity as T;
   //         if (entitiesById.TryGetValue(id, out Entity ent))
			//	return (T?)ent;
			//else return null;
		}

        public T? GetByRef<T>(ref readonly EntityReference reference) where T : Entity
        {
            if (ents[reference.id].generation == reference.generation)
                return ents[reference.id].entity as T;
            else return null;
        }

        public T GetFirst<T>() where T : Entity
        {
			var all = GetAll<T>();

			return all.FirstOrDefault() as T;
        }

		//TODO this feels like it would be slow.
		public void UpdateTrackedPositions<T>(T ent, IReadOnlyList<CubePosition> oldTrackedPositions) where T : Entity, IMultiCubeTracker
		{
            for (int i = 0; i < oldTrackedPositions.Count(); i++)
            {
                ChunkPosition cpos = ChunkPosition.CubeChunk(oldTrackedPositions.ElementAt(i));
                cubeTrackers[cpos].Remove(oldTrackedPositions.ElementAt(i).InChunkSpace(cpos));
            }

            var trackedPositions = ent.TrackedPositions;
            for (int i = 0; i < trackedPositions.Count(); i++)
            {
                ChunkPosition cpos = ChunkPosition.CubeChunk(trackedPositions.ElementAt(i));
                cubeTrackers[cpos].Add(trackedPositions.ElementAt(i).InChunkSpace(cpos), ent);
            }
		}

		private IReadOnlyList<Entity> emptyList = new List<Entity>();
		public IReadOnlyList<Entity> GetAll<T>() where T : Entity
		{
			if (entitiesByType.ContainsKey(typeof(T)))
				return entitiesByType[typeof(T)];
			else return emptyList;
		}

		public IReadOnlyList<Entity> GetAll(Type type)
		{
			if (entitiesByType.ContainsKey(type))
				return entitiesByType[type];
			else return emptyList;
		}

		public IEnumerable<Entity> GetEntities()
		{
			return new EntityIterator(this);
		}

		public Optional<Entity> GetEntityTrackingPosition(CubePosition position)
		{
			ChunkPosition chunkPos = ChunkPosition.CubeChunk(position);

			Entity ent = null;
			if (cubeTrackers.ContainsKey(chunkPos))
				ent = cubeTrackers[chunkPos].Get(position.InChunkSpace(chunkPos));

			return new Optional<Entity>(ent);
		}

		public void GetEntitiesTrackingPositions(Span<CubePosition> positions, Span<Entity> entities, int offset = 0, int count = -1)
		{
			if (count == -1)
				count = positions.Length;

			ChunkPosition previousChunkPos = new ChunkPosition();
			CubeTrackers ts = new CubeTrackers();

			for (int i = offset; i < offset + count; i++)
			{
				ChunkPosition chunkPos = ChunkPosition.CubeChunk(positions[i]);
				if (i == offset || chunkPos != previousChunkPos)
					ts = cubeTrackers[chunkPos];

				entities[i] = ts.Get(positions[i].InChunkSpace(chunkPos));
			}
		}

		private static CubeTrackers emptyTrackers = new CubeTrackers();
		public unsafe void GetEntityMeshingDatas(Span<CubePosition> positions, Span<BepuUtilities.Memory.Buffer<byte>> meshingDatas, BufferPool bufferPool,
			int offset = 0, int count = -1)
		{
            using var zone = TracyImpl.Tracy.BeginZone();

            
			{
				if (count == -1)
					count = positions.Length;

				ChunkPosition previousEmptyChunk = new ChunkPosition();
				ChunkPosition previousChunkPos = new ChunkPosition(-1, -1, -1);
				CubeTrackers ts = emptyTrackers;

                IMGUIConsole.Assert(offset >= 0 && offset + count <= positions.Length);

				for (int i = offset; i < offset + count; i++)
				{
					ChunkPosition chunkPos = ChunkPosition.CubeChunk(positions[i]);
					//hold onto the previous empty chunk to reduce number of lookups (since we have to look up the tracker in order to see if it's empty first, which is slow.)
					if (previousEmptyChunk == chunkPos)
						continue;

					if (i == offset || chunkPos != previousChunkPos)
					{
						if (cubeTrackers.ContainsKey(chunkPos))
						{
							ts = cubeTrackers[chunkPos];
							previousChunkPos = chunkPos;
						}
						else
						{
							previousEmptyChunk = chunkPos;
							meshingDatas[i] = default;
							continue;
						}
					}

					Entity ent = ts.Get(positions[i].InChunkSpace(chunkPos));

					if (ent != null && ent is ICubeTracker tracker)
					{
                        lock (bufferPool)
                            meshingDatas[i] = tracker.GetMeshingData(bufferPool);
					}
					else meshingDatas[i] = default;
				}
			}
        }

		public EntityReference GetReference(Entity entity)
		{
			return new EntityReference
			{
				generation = ents[entity.Id].generation,
				id = ents[entity.Id].id,
			};
		}

		public EntityReference GetReference(int id)
		{
			return new EntityReference
			{
				generation = ents[id].generation,
				id = id,
			};
		}

		public void Draw(GraphicsDevice device, Effect effect)
		{
			foreach (var r in Main.Registry.RendererRegistry.GetIterable())
			{
				if (r != null)
				{
					Type?[] renderedTypes = r.GetRenderedTypes();

                    for (int i = 0; i < renderedTypes.Length; i++)
					{
                        Type? renderedType = renderedTypes[i];
						if (renderedType != null)
						{
							if (entitiesByType.TryGetValue(renderedType, out var renderedEntities))
								r.Render(device, 0, this, i, renderedEntities);
						}
					}
				}
			}

			for (int i = 0; i < EntMax; i++) 
			{
				if (ents[i].active && (ents[i].entity.AlwaysRender || Main.camera.FrustumContains(ents[i].entity.Position)))
					ents[i].entity.Draw(device, effect);
			}
		}
    }
}
