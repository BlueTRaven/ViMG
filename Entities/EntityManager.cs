using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ViMG.Entities
{
	public class EntityManager
	{
		private List<Entity> entities = new List<Entity>();
		private Dictionary<Type, List<Entity>> entitiesByType = new Dictionary<Type, List<Entity>>();

		private List<Entity> toAddLater = new List<Entity>();
		private List<Entity> toDeleteLater = new List<Entity>();

		private Dictionary<CubePosition, ICubeTracker> cubeTrackers = new Dictionary<CubePosition, ICubeTracker>();

		private readonly World world;

		public EntityManager(World world)
		{
			this.world = world;
		}

		public void Add(Entity entity)
		{
			toAddLater.Add(entity);
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
				if (entity is ICubeTracker tracker)
				{
					//HACK: if trackers already contains the entity, don't add a new one.
					//This happens because if a detail phase chunk generation cascades to an adjacent chunk, it calls 
					//PostChunkInit for every cube in that chunk every time any cube is set. This means lots of PostChunkInit calls!
					//	(Note: the system no longer does this and now calls PostChunkInit once for every chunk it cascades to.)
					//The system should be modified so that PostChunkInit is only ever called once for a given chunk after setting cubes in it.
					//This will probably be an issue still when multiple chunks write to the same chunk.
					if (!cubeTrackers.ContainsKey(tracker.TrackedPosition))
						cubeTrackers.Add(tracker.TrackedPosition, tracker);
					else continue;
				}

				entities.Add(entity);
				if (!entitiesByType.ContainsKey(entity.GetType()))
					entitiesByType.Add(entity.GetType(), new List<Entity>());
				entitiesByType[entity.GetType()].Add(entity);

				entity.Initialize(world);
			}

			toAddLater.Clear();

			foreach (Entity entity in entities)
			{
				entity.Update(deltaTime);
			}

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

		public IReadOnlyList<Entity> GetAll<T>() where T : Entity
		{
			if (entitiesByType.ContainsKey(typeof(T)))
				return entitiesByType[typeof(T)];
			else return null;
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

		public void Draw(GraphicsDevice device)
		{
			foreach (Entity entity in entities)
			{
				if (entity.AlwaysRender || Main.camera.FrustumContains(entity.Position))
					entity.Draw(device);
			}
		}
	}
}
