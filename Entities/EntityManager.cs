using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ViMG.Entities
{
	public class EntityManager
	{
		private bool iterating;

		private ulong lastEntityId;

		private List<Entity> entities = new List<Entity>();
		private Dictionary<Type, List<Entity>> entitiesByType = new Dictionary<Type, List<Entity>>();

		private List<Entity> toAddLater = new List<Entity>();
		private List<Entity> toDeleteLater = new List<Entity>();

		private Dictionary<CubePosition, ICubeTracker> cubeTrackers = new Dictionary<CubePosition, ICubeTracker>();

		private readonly World world;

		public ulong GetUniqueId()
		{
			return lastEntityId++;
		}

		public void SetUniqueIdSeed(ulong seed)
		{
			lastEntityId = seed;
		}

		public EntityManager(World world)
		{
			this.world = world;
		}

		public void ForceAdd(Entity entity, ulong id)
		{
			ReallyAdd(entity, (long)id, true);
		}

		public void AddTileEntity(ICubeTracker tracker)
		{
			if (cubeTrackers.ContainsKey(tracker.TrackedPosition))
				return;
			else
				Add(tracker as Entity);
		}

		public void Add(Entity entity)
		{
			if (iterating)
				toAddLater.Add(entity);
			else ReallyAdd(entity);
		}

		private void ReallyAdd(Entity entity, long id = -1, bool replaceCubeTracker = false)
		{
			if (entity is ICubeTracker tracker)
			{
				//If entity is already present, then replace it
				if (cubeTrackers.ContainsKey(tracker.TrackedPosition))
				{
					if (replaceCubeTracker)
					{
						Remove(cubeTrackers[tracker.TrackedPosition] as Entity);
						cubeTrackers[tracker.TrackedPosition] = tracker;
					}
					else return;	//don't add the entity.
				}
				else cubeTrackers.Add(tracker.TrackedPosition, tracker);
			}

			entities.Add(entity);
			if (!entitiesByType.ContainsKey(entity.GetType()))
				entitiesByType.Add(entity.GetType(), new List<Entity>());
			entitiesByType[entity.GetType()].Add(entity);

			if (id < 0)
				entity.SetId(GetUniqueId());
			else entity.SetId((ulong)id);

			entity.Initialize(world);
		}

		public void Remove(Entity entity)
		{
			toDeleteLater.Add(entity);
			entity.OnDelete();
		}

		public void Update(double deltaTime)
		{
			foreach (Entity entity in toAddLater)
			{
				ReallyAdd(entity);
			}

			toAddLater.Clear();

			iterating = true;

			foreach (Entity entity in entities)
			{
				entity.Update(deltaTime);
			}

			iterating = false;

			foreach (Entity entity in toDeleteLater)
			{
				entities.Remove(entity);

				if (entitiesByType.ContainsKey(entity.GetType()))
					entitiesByType[entity.GetType()].Remove(entity);

				if (entity is ICubeTracker tracker && cubeTrackers.ContainsKey(tracker.TrackedPosition))
					cubeTrackers.Remove(tracker.TrackedPosition);
			}

			toDeleteLater.Clear();
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

		public Optional<ICubeTracker> GetEntityTrackingPosition(CubePosition position)
		{
			if (cubeTrackers.ContainsKey(position))
				return new Optional<ICubeTracker>(cubeTrackers[position]);
			else return new Optional<ICubeTracker>();
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
