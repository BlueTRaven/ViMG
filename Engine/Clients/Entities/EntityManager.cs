using Engine.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Entities;

namespace Engine.Clients.Entities
{
    public class EntityManager
    {
        private struct EntityHolder
        {
            public int id;
            public int generation;
            public bool active;
            public string entityType;
            public BasicState state;

            public static EntityHolder DEFAULT = new()
            {
                id = -1,
                generation = -1,
                active = false,
                state = new(),
            };
        }

        private EntityHolder[] entities;

        public int MaxEnts => entities.Length;

        public EntityManager()
        {
            entities = new EntityHolder[ViMG.Entities.EntityManager.EntMax];
            Array.Fill(entities, EntityHolder.DEFAULT);
        }

        public void NewFrame(EntityManager prev)
        {
            for (int i = 0; i < prev.entities.Length; i++)
            {
                entities[i] = prev.entities[i];
            }
        }

        public BasicState GetById(int id)
        {
            if (id < 0) return new BasicState();
            return entities[id].state;
        }

        public ViMG.Entities.EntityManager.EntityReference GetReference(int id)
        {
            return new ViMG.Entities.EntityManager.EntityReference
            {
                id = entities[id].id,
                generation = entities[id].generation,
            };
        }

        public string? GetTypeById(int id)
        {
            if (id == -1) return null;
            return entities[id].entityType;
        }

        public void Set(ViMG.Entities.EntityManager.EntityReference reference, string type, BasicState state) 
        {
            entities[reference.id] = new()
            {
                id = reference.id,
                generation = reference.generation,
                state = state,
                active = true,
                entityType = type,
            };
        }

        public void Remove(ViMG.Entities.EntityManager.EntityReference reference)
        {
            entities[reference.id] = new EntityHolder
            {
                id = reference.id,
                generation = reference.generation,
                active = false,
            };
        }
    }
}
