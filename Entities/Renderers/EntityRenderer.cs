using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViMG.Entities.Renderers
{
    public abstract class EntityRenderer : IRegisterable
    {
        private readonly string identifier;
        public string Identifier => identifier;

        public EntityRenderer(string identifier, GraphicsDevice device)
        {
            this.identifier = identifier;
        }

        public virtual void NewEntityManagerInitialized(EntityManager entityManager)
        {
            entityManager.OnEntityAdded += OnEntityAdded;
        }

        public virtual void EntityManagerDisposed(EntityManager entityManager)
        {
            entityManager.OnEntityRemoved += OnEntityRemoved;
        }

        private void OnEntityAdded(Entity entity)
        {
            if (entity.GetType() == GetRenderedType())
            {
                OnEntityOfOurTypeAdded(entity);
            }
        }

        private void OnEntityRemoved(Entity entity)
        {
            if (entity.GetType() == GetRenderedType())
            {
                OnEntityOfOurTypeRemoved(entity);
            }
        }

        protected virtual void OnEntityOfOurTypeAdded(Entity entity)
        {

        }

        protected virtual void OnEntityOfOurTypeRemoved(Entity entity)
        {

        }

        public abstract Type GetRenderedType();

        public abstract void Render(GraphicsDevice device, double deltaTime, EntityManager entityManager);
    }
}
