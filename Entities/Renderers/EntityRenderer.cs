using BrUtility;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;

namespace ViMG.Entities.Renderers
{
    public abstract class EntityRenderer : IRegisterable
    {
        private readonly string identifier;
        public string Identifier => identifier;

        private FastList<Type> renderableTypes = new FastList<Type>();

        public EntityRenderer(string identifier, GraphicsDevice device)
        {
            this.identifier = identifier;
        }

        public virtual void NewEntityManagerInitialized(EntityManager entityManager)
        {
            entityManager.OnEntityAdded += OnEntityAdded;
            entityManager.OnEntityRemoved += OnEntityRemoved;
        }

        public virtual void EntityManagerDisposed(EntityManager entityManager)
        {
            entityManager.OnEntityAdded -= OnEntityAdded;
            entityManager.OnEntityRemoved -= OnEntityRemoved;
        }

        private void OnEntityAdded(Entity entity)
        {
            Type[] types = GetRenderedTypes();

            for (int i = 0; i < types.Length; i++)
            {
                if (entity.GetType() == types[i])
                    OnEntityOfOurTypeAdded(i, entity);
            }
        }

        private void OnEntityRemoved(Entity entity)
        {
            Type[] types = GetRenderedTypes();

            for (int i = 0; i < types.Length; i++)
            {
                if (entity.GetType() == types[i])
                    OnEntityOfOurTypeRemoved(i, entity);
            }
        }

        protected virtual void OnEntityOfOurTypeAdded(int renderedTypeIndex, Entity entity)
        {

        }

        protected virtual void OnEntityOfOurTypeRemoved(int renderedTypeIndex, Entity entity)
        {

        }

        public abstract Type[] GetRenderedTypes();

        public abstract void Render(GraphicsDevice device, double deltaTime, EntityManager entityManager, int renderedTypeIndex);
    }
}
