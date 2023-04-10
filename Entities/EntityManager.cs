using BepuUtilities.Memory;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ViMG.Entities
{
	public class EntityManager
	{
		public event Action<Entity> OnEntityAdded;
		public event Action<Entity> OnEntityRemoved;

		private bool iteratingUpdate;

		private ulong lastEntityId;

		private List<Entity> entities = new List<Entity>();
		private Dictionary<Type, List<Entity>> entitiesByType = new Dictionary<Type, List<Entity>>();

		private List<Entity> toAddLater = new List<Entity>();
		private HashSet<Entity> toDeleteLater = new HashSet<Entity>();

		private class CubeTrackers
		{
			public ICubeTracker[] cubeTrackers;
			public IMultiCubeTracker[] multiCubeTrackers;

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
                    else throw new Exception("???");
                }
				else if (entity is IMultiCubeTracker multiTracker)
				{
					if (multiCubeTrackers == null)
						multiCubeTrackers = new IMultiCubeTracker[Chunk.NUM_CUBES_IN_CHUNK];

					if (multiCubeTrackers[i] == null)
						multiCubeTrackers[i] = multiTracker;
					else throw new Exception("???");
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

		public ulong GetUniqueId()
		{
			return lastEntityId++;
		}

		public void SetUniqueIdSeed(ulong seed)
		{
			lastEntityId = seed;
		}

		public EntityManager()
		{
		}

		public void Initialize(World world)
        {
			this.world = world;

			this.sizeInCubes = world.sizeInCubes;

            foreach (var r in Main.Registry.RendererRegistry.GetIterable())
            {
				if (r != null)
					r.NewEntityManagerInitialized(this);
            }
        }

		public void Dispose()
		{
            foreach (var r in Main.Registry.RendererRegistry.GetIterable())
            {
				if (r != null)
					r.EntityManagerDisposed(this);
            }

            UnloadAll();
		}

		public void ForceAdd(Entity entity, ulong id)
		{
			if (iteratingUpdate)
				throw new Exception("Cannot add while iterating");

			ReallyAdd(entity, (long)id);
		}

		public void Add(Entity entity, bool delayAdding = false)
		{
			if (iteratingUpdate || delayAdding)
				toAddLater.Add(entity);
			else ReallyAdd(entity);
		}

		private void ReallyAdd(Entity entity, long id = -1)
		{
			if (iteratingUpdate)
				throw new Exception("Cannot add while iterating");

			if (entity is ICubeTracker tracker)
			{
                CubePosition position = tracker.TrackedPosition;

				ChunkPosition chunkPos = ChunkPosition.CubeChunk(position);

				if (cubeTrackers.ContainsKey(chunkPos))
					cubeTrackers[chunkPos].Add(position.InChunkSpace(chunkPos), entity);
				else
				{
					CubeTrackers ts = new CubeTrackers();
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
                        ts.Add(position.InChunkSpace(chunkPos), entity);

                        cubeTrackers.Add(chunkPos, ts);
                    }
				}
			}

			entities.Add(entity);
			if (!entitiesByType.ContainsKey(entity.GetType()))
				entitiesByType.Add(entity.GetType(), new List<Entity>());
			entitiesByType[entity.GetType()].Add(entity);

			if (id < 0)
				entity.SetId(GetUniqueId());
			else entity.SetId((ulong)id);

			entity.Initialize(world);

			OnEntityAdded?.Invoke(entity);
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
            foreach (Entity entity in entities)
            {
				if (ChunkPosition.WorldSpaceChunk(entity.Position) == pos)
					Unload(entity, true);
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
			foreach (Entity entity in entities)
			{
				if (!toDeleteLater.Contains(entity))
					Unload(entity, true);
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
			AddLaterEntities();

			iteratingUpdate = true;

			foreach (Entity entity in entities)
			{
				if (!entity.Dead)
					entity.Update(deltaTime);
			}

			iteratingUpdate = false;

			foreach (Entity entity in toDeleteLater)
			{
				ReallyRemove(entity);
			}

			toDeleteLater.Clear();
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
			if (iteratingUpdate)
				throw new Exception("Cannot remove entity while iterating");

			entity.OnUnload();
			entities.Remove(entity);

			if (entitiesByType.ContainsKey(entity.GetType()))
				entitiesByType[entity.GetType()].Remove(entity);

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

		public T GetFirst<T>() where T : Entity
        {
			var all = GetAll<T>();

			return all.FirstOrDefault() as T;
        }

		private IReadOnlyList<Entity> emptyList = new List<Entity>();
		public IReadOnlyList<Entity> GetAll<T>() where T : Entity
		{
			if (entitiesByType.ContainsKey(typeof(T)))
				return entitiesByType[typeof(T)];
			else return emptyList;
		}

		public IReadOnlyList<Entity> GetEntities()
		{
			return entities;
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

		public void GetEntityMeshingDatas(Span<CubePosition> positions, Span<BepuUtilities.Memory.Buffer<byte>> meshingDatas, BufferPool bufferPool,
			int offset = 0, int count = -1)
		{
			lock (bufferPool)
			{
				if (count == -1)
					count = positions.Length;

				ChunkPosition previousEmptyChunk = new ChunkPosition();
				ChunkPosition previousChunkPos = new ChunkPosition(-1, -1, -1);
				CubeTrackers ts = new CubeTrackers();

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
						meshingDatas[i] = tracker.GetMeshingData(bufferPool);
					else meshingDatas[i] = default;
				}
			}
        }

		public void Draw(GraphicsDevice device, Effect effect)
		{
			foreach (var r in Main.Registry.RendererRegistry.GetIterable())
			{
				if (r != null && entitiesByType.ContainsKey(r.GetRenderedType()))
					r.Render(device, 0, this);
			}

			foreach (Entity entity in entities)
			{
				if (entity.AlwaysRender || Main.camera.FrustumContains(entity.Position))
					entity.Draw(device, effect);
			}
		}
    }
}
