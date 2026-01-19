using BepuUtilities.Memory;
using BrUtility;
using Engine.Common.Entities;
using Engine.Networking;
using Engine.Networking.Messages;
using LiteNetLib;
using LiteNetLib.Utils;
using Microsoft.Xna.Framework;
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

namespace ViMG.Entities
{
	public class EntityManager : IGetEntity
	{
		[ConsoleCommandVar("ent_max", "Maximum numbere of entities the server can have active at once. Entities allocated in excess of this number will be immediately destroyed.\n" +
			"Changes to this variable require a restart.")]
		public static int EntMax = 4096;

		[ConsoleCommandVar("ent_prev_copies", "Number of previous copies of an entity to keep (for interpolation. Includes current state). Default = 2.")]
		public static int EntPrev = 2;
		[ConsoleCommandVar("ent_prev_copies_srv", "Number of previous copies of an entity to keep (for interpolation and networking. Includes current state.) Default = 30")]
		public static int EntPrevSrv = 5;

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

			public BasicState[] prevState;

			public static EntityHolder DEFAULT = new()
			{
				id = -1,
				generation = -1,
				active = false,
				entity = null,
				prevState = null,
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
					if (manager.ents[currentIndex].active && manager.ents[currentIndex].entity.Enabled) return true;
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

		public Engine.Common.Entities.CubeTrackers MeshCubeTrackers;

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
		private Dictionary<ChunkPosition, CubeTrackers> cubeTrackers = new Dictionary<ChunkPosition, CubeTrackers>();

		private World world;

		public int GetUniqueId()
		{
			if (freeList.Count == 0) return -1;
			int last = freeList.Last();
			freeList.RemoveAt(freeList.Count - 1);

			return last;
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

			MeshCubeTrackers = new Engine.Common.Entities.CubeTrackers();
		}

		public void Initialize(World world)
        {
			this.world = world;

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

		public void ForceAdd(Entity entity, ulong id, int forceGeneration = -1)
		{
			if (iteratingUpdate)
				throw new Exception("Cannot add while iterating");

			if (ents[id].active)
			{
				Debug.Assert(ents[id].entity is not Player);
				Console.WriteLine("Unload {0}:{1} to make room for {2}", ents[id].entity.ToString(), id, entity.ToString());
				ForceUnload(ents[id].entity);
			}
			else freeList.Remove((int)id);
            entity.SetId(id);

            ReallyAdd(entity);

			if (forceGeneration != -1)
				ents[id].generation = forceGeneration;
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

			Debug.Assert(!ents[entity.Id].active, "Entity with id already exists");
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

                    MeshCubeTrackers.Get(chunkPos).Add(position.InChunkSpace(), GetReference((int)entity.Id));
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

						MeshCubeTrackers.Get(chunkPos).Add(position.InChunkSpace(), GetReference((int)entity.Id));
                    }
                }
            }

            OnEntityAdded?.Invoke(entity);

			if (entity is ISyncBasicState basicState)
			{
                if (ents[entity.Id].prevState == null)
                {
                    ents[entity.Id].prevState = new BasicState[EntPrevSrv];
                }

                basicState.Get(out var state);
				ents[entity.Id].prevState[Main.Frame % EntPrevSrv] = state;
			}
        }

		public void Kill(Entity entity)
		{
			toDeleteLater.Add(entity);
			entity.OnKill();
		}

		public void Unload(Entity entity, bool delay = false)
        {
            if (entity.NetEntity && Main.gameStateManager.netMode == GameStates.GameStateManager.NetworkingMode.Client)
            {
                // Force entity to be disabled
                entity.NetEnable = false;
                return;
            }

            if (iteratingUpdate || delay)
				toDeleteLater.Add(entity);
			else ReallyUnload(entity);
        }

		public void ForceUnload(Entity entity)
		{
			ReallyUnload(entity);
		}

		// Unloads all entities in a chunk.
		// TODO: O(n)
		public void UnloadInChunk(ChunkPosition pos)
        {
            Debug.Assert(!iteratingUpdate, "Cannot unload a chunk while iterating");

			//TODO: better method of determining which entities are in this chunk for unloading

			AddLaterEntities();

			//Initial flush to remove entities that are already queued to be deleted.
			//This is so that we don't have to check for entities that are already queued when trying to unload.
            foreach (Entity entity in toDeleteLater)
            {
				Unload(entity);
            }

			toDeleteLater.Clear();

            //queue all entities in chunk to be unloaded

            for (int i = 0; i < EntMax; i++)
            {
                //&& (ents[i].entity is not Player || world.isDisposed)// Players cannot be unloaded normally
                if (ents[i].active) 
				{
					if (ents[i].entity.Position == Vector3.Zero) continue;
					if (ChunkPosition.WorldSpaceChunk(ents[i].entity.Position) == pos)
						Unload(ents[i].entity, true);
				}
			}

			//Another flush, to remove any entities that are newly added to the queue...
			//(aka any entity with a position inside the chunk.)
			foreach (Entity entity in toDeleteLater)
			{
                Unload(entity);
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
                    Unload(entity);
                }

                toDeleteLater.Clear();
            }
		}

		// Unloads all entities (including entities queued for unloading).
		// NOTE: throws an exception if used during iteration.
		public void UnloadAll()
		{
            Debug.Assert(!iteratingUpdate, "Cannot remove entities while iterating");

            //queue all entities to be unloaded
            for (int i = 0; i < EntMax; i++)
			{
				if (ents[i].active)
				{
					if (!toDeleteLater.Contains(ents[i].entity))
						Unload(ents[i].entity, true);
				}
			}

			//Now remove them, and whatever else was in the queue...
			foreach (Entity entity in toDeleteLater)
			{
                Unload(entity, false);
            }

            toDeleteLater.Clear();

			//also clear toAddLater so we don't end up adding some entities after
			toAddLater.Clear();
		}

        public void AddLaterEntities()
        {
            foreach (Entity entity in toAddLater)
            {
                ReallyAdd(entity);
            }

            toAddLater.Clear();
        }

        private void ReallyUnload(Entity entity)
        {
            if (entity == null)
                return;

			//if ((entity is Player && !world.isDisposed && !world.isCreateWorldReloading))
			//	return;

			Console.WriteLine("Unload {0}", entity.ToString());

            Debug.Assert(!iteratingUpdate, "Cannot remove entity while iterating");

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

                    MeshCubeTrackers.Get(chunkPos).Remove(position.InChunkSpace());
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

                        MeshCubeTrackers.Get(chunkPos).Remove(position.InChunkSpace());
                    }
                }
            }

            OnEntityRemoved?.Invoke(entity);
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
						if (ents[i].entity.Enabled && (ents[i].entity.NetEnable || !ents[i].entity.NetEntity))
							ents[i].entity.Update(deltaTime);
					}
					catch (Exception e)
					{
						Console.WriteLine("Entity {0} (id {1}) caused an error during Update. It has been removed.\n{2}", ents[i].entity, ents[i].id, e.ToString());
						Unload(ents[i].entity);
					}
				}
			}

			for (int i = 0; i < EntMax; i++)
			{
				if (ents[i].active && !ents[i].entity.Dead)
				{
					if (ents[i].entity.CanBeDisabled && !(ents[i].entity is ICubeTracker || ents[i].entity is IMultiCubeTracker))
					{
						var localPlayer = world.GetLocalPlayer();

						if (localPlayer != null)
						{
							var dist = world.DistanceFromPlayer(localPlayer, ents[i].entity.Position);
							if (dist >= ents[i].entity.DisableDistance)
							{
								ents[i].entity.Enabled = false;
								if (ents[i].entity.DestroyOnDisabled)
								{
									Unload(ents[i].entity);
								}
							}
						}
					}
				}
			}

			iteratingUpdate = false;

			foreach (Entity entity in toDeleteLater)
			{
				Unload(entity);
			}

			toDeleteLater.Clear();
		}

		public void UpdateNetwork()
		{
            for (int i = 0; i < EntMax; i++)
            {
                bool clear = false;

                if (ents[i].active && !ents[i].entity.Dead)
                {
                    if (ents[i].entity is ISyncBasicState basicState)
                    {
                        basicState.Get(out var state);

                        ents[i].prevState[SyncEntityState.Instance.serverSequence % EntPrevSrv] = state;
                    }
                    else clear = true;
                }
                else clear = true;

                if (clear)
                {
                    if (ents[i].prevState != null)
                        ents[i].prevState[SyncEntityState.Instance.serverSequence % EntPrevSrv] = new BasicState();
                }
            }

            SyncEntityState.Instance.DoSync(this, world.player);
		}

		public int GetPrevIndexTime(float time) 
		{
			return (int)(time * (float)Main.FIXED_FPS);
		}

		public BasicState GetPrevState(int id, int prev)
		{
            // negative numbers would be in the future, big nono
            Debug.Assert(prev >= 0 && prev < EntPrevSrv);

			int which = SyncEntityState.Instance.serverSequence - prev;
			which = ((which % EntPrevSrv) + EntPrevSrv) % EntPrevSrv;

            return ents[id].prevState?[which] ?? new();
		}

		public BasicState GetPrevStateAbs(int id, int frame)
		{
			var diff = SyncEntityState.Instance.serverSequence - frame;

			// If we overflowed, just return no state
			if (diff >= EntPrevSrv) return new();

			return GetPrevState(id, diff);
		}

		public bool GetActive(int id) => ents[id].active;

		public Entity? GetById(ulong id)
		{
			return ents[(int)id].entity;
        }

		public Entity? GetByRefServer(ref readonly EntityReference reference)
		{
			if (ents[reference.id].generation == reference.generation)
				return ents[reference.id].entity;
			else return null;
		}

		public T? GetById<T>(ulong id) where T : Entity
		{
            return ents[(int)id].entity as T;
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
			CubeTrackers ts = null!;

			for (int i = offset; i < offset + count; i++)
			{
				ChunkPosition chunkPos = ChunkPosition.CubeChunk(positions[i]);
				if (ts == null || i == offset || chunkPos != previousChunkPos)
					ts = cubeTrackers[chunkPos];

				entities[i] = ts.Get(positions[i].InChunkSpace(chunkPos));
			}
		}

		public void GetAllTrackersForChunk(ChunkPosition position, FastList<ICubeTracker> cubeTrackers, FastList<IMultiCubeTracker> multiCubeTrackers)
		{
			if (this.cubeTrackers.TryGetValue(position, out CubeTrackers? cubeTracker))
			{
				foreach (var t in cubeTracker.cubeTrackers.AsSpan())
				{
					if (t != null) cubeTrackers.Add(t);
				}
				foreach (var t in cubeTracker.multiCubeTrackers.AsSpan())
				{
					if (t != null) multiCubeTrackers.Add(t);
				}
			}
			else
			{
				cubeTrackers = FastList<ICubeTracker>.EMPTY;
				multiCubeTrackers = FastList<IMultiCubeTracker>.EMPTY;
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
			//foreach (var r in Main.Registry.RendererRegistry.GetIterable())
			//{
			//	if (r != null)
			//	{
			//		int[] renderedTypes = r.GetRenderedTypes();

   //                 for (int i = 0; i < renderedTypes.Length; i++)
			//		{
   //                     int renderedType = renderedTypes[i];
			//			if (entitiesByType.TryGetValue(renderedType, out var renderedEntities))
			//				r.Render(device, 0, this, i, renderedEntities);
			//		}
			//	}
			//}

			//for (int i = 0; i < EntMax; i++) 
			//{
			//	if (ents[i].active && (ents[i].entity.AlwaysRender || Main.camera.FrustumContains(ents[i].entity.Position)))
			//		ents[i].entity.Draw(device, effect);
			//}
		}

		[ConsoleCommand("killall", "killall <ent type name> [force] - kills all entities of type. If force (optional) is true, unloads instead of killing.")]
		public static void KillAll(string[] parameters)
		{
			IMGUIConsole.RequireParam(parameters, 0, "ent_type_name");

			string entTypeName = parameters[0];
			Type type = Utility.GetType(entTypeName);

			if (type == null)
			{
				IMGUIConsole.LogLine("[error] Could not find entity type with name " + entTypeName);
				return;
			}

            var entManager = Main.gameStateManager.TheIsland.GetWorld().EntityManager;
            var ents = entManager.GetAll(type);
			foreach (var ent in ents)
				entManager.Unload(ent);
		}

        [ConsoleCommand("killallbutplayer", "killallbutplayer - kills all entities but player entities")]
        public static void KillAllExceptPlayer(string[] parameters)
		{
            var entManager = Main.gameStateManager.TheIsland.GetWorld().EntityManager;

			foreach (var ent in entManager.GetEntities())
			{
				if (ent is not Player)
				{
					entManager.Unload(ent);
				}
			}
        }

		public BasicState GetByRef(ref readonly EntityReference reference)
		{
			if (ents[reference.id].generation != reference.generation) return new();
			else return GetPrevState(reference.id, 0); ;
		}

        public List<int> GetFreeList()
        {
			return freeList;
        }
    }
}
