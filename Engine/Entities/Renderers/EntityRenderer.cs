using BrUtility;
using Engine.Networking;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
using ViMG.IMGUIImpl;

namespace ViMG.Entities.Renderers
{
    public abstract class EntityRenderer : IRegisterable
    {
        [ConsoleCommandVar("r_delay_render_ent", "Time in past to start interpolation from")]
        public static float DelayRenderEnt = 2.0f / 60.0f;

        private readonly string identifier;
        public string Identifier => identifier;

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
            int[] types = GetRenderedTypes();

            for (int i = 0; i < types.Length; i++)
            {
                if (Main.Registry.EntityRegistry.GetFromEntity(entity).Id == types[i])
                    OnEntityOfOurTypeAdded(i, entity);
            }
        }

        private void OnEntityRemoved(Entity entity)
        {
            int[] types = GetRenderedTypes();

            for (int i = 0; i < types.Length; i++)
            {
                if (Main.Registry.EntityRegistry.GetFromEntity(entity).Id == types[i])
                    OnEntityOfOurTypeRemoved(i, entity);
            }
        }

        protected virtual void OnEntityOfOurTypeAdded(int renderedTypeIndex, Entity entity)
        {

        }

        protected virtual void OnEntityOfOurTypeRemoved(int renderedTypeIndex, Entity entity)
        {

        }

        /// <summary>
        /// Get the EntityType ids that will be rendered
        /// </summary>
        /// <returns></returns>
        public abstract int[] GetRenderedTypes();

        public virtual void RenderClientEnt(GraphicsDevice device, double deltaTime, Engine.Clients.ClientStates client, int entityType) { }

        public virtual void RenderUI(GraphicsDevice device, SpriteBatch batch, double deltaTime, Engine.Clients.ClientStates client, int entityType) { }

        protected ref struct Iterator<T> where T : Entity
        {
            int current;
            List<Entity> entities;

            public Iterator(List<Entity> renderedEntities)
            {
                entities = renderedEntities;
            }

            public bool Next(out T ent)
            {
                if (current == entities.Count)
                {
                    ent = null;
                    return false;
                }
                else
                {
                    ent = entities[current] as T;
                    current++;
                    return true;
                }
            }
        }
    }
}
