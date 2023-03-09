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

		private Dictionary<CubePosition, ICubeTracker> cubeTrackers = new Dictionary<CubePosition, ICubeTracker>();
		private Dictionary<CubePosition, IMultiCubeTracker> multiCubeTrackers = new Dictionary<CubePosition, IMultiCubeTracker>();

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
        }

		public void ForceAdd(Entity entity, ulong id)
		{
			if (iteratingUpdate)
				throw new Exception("Cannot add while iterating");

			ReallyAdd(entity, (long)id);
		}

		public void AddTileEntity(ICubeTracker tracker)
		{
			if (cubeTrackers.ContainsKey(tracker.TrackedPosition))
				return;
			else
				Add(tracker as Entity);
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
				//If entity is already present, then replace it
				if (cubeTrackers.ContainsKey(tracker.TrackedPosition))
					return;	//don't add the entity.
				else cubeTrackers.Add(tracker.TrackedPosition, tracker);
			}

			if (entity is IMultiCubeTracker multiTracker)
			{
				foreach (CubePosition position in multiTracker.TrackedPositions)
				{
					if (multiCubeTrackers.ContainsKey(position))
						return;
					else multiCubeTrackers.Add(position, multiTracker);
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

			//queue all entities in chunk to be unloaded
			foreach (Entity entity in entities)
            {
				if (ChunkPosition.WorldSpaceChunk(entity.Position) == pos && !toDeleteLater.Contains(entity))
					Unload(entity, true);
			}

			//Now remove them, and whatever else was in the queue...
			foreach (Entity entity in toDeleteLater)
			{
				ReallyRemove(entity);
			}

			toDeleteLater.Clear();

			//...and toAddLater, since we don't want to unload the chunk, then spawn it.
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

			if (entity is ICubeTracker tracker && cubeTrackers.ContainsKey(tracker.TrackedPosition))
				cubeTrackers.Remove(tracker.TrackedPosition);

			if (entity is IMultiCubeTracker multiTracker)
			{
				foreach (CubePosition pos in multiTracker.TrackedPositions)
				{
					if (multiCubeTrackers.ContainsKey(pos))
						multiCubeTrackers.Remove(pos);
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
			if (cubeTrackers.ContainsKey(position))
				return new Optional<Entity>(cubeTrackers[position] as Entity);
			else if (multiCubeTrackers.ContainsKey(position))
				return new Optional<Entity>(multiCubeTrackers[position] as Entity);
			else return new Optional<Entity>();
		}

		public void Draw(GraphicsDevice device, Effect effect)
		{
			foreach (Entity entity in entities)
			{
				if (entity.AlwaysRender || Main.camera.FrustumContains(entity.Position))
					entity.Draw(device, effect);
			}
		}
    }
}
