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
		}

		public void Update(double deltaTime)
		{
			foreach (Entity entity in toAddLater)
			{
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

		public void Draw(GraphicsDevice device)
		{
			foreach (Entity entity in entities)
			{
				entity.Draw(device);
			}
		}
	}
}
