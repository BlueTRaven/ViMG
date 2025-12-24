using Engine.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG;
using ViMG.Entities;
using ViMG.Entities.Renderers;

namespace Engine.Clients.Entities
{
    public class ClientEntityManager
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

        private struct PlayerHolder
        {
            public EntityManager.EntityReference entity;
            public bool active;
            public int playerIndex;
            public int playerUuid;

            public static PlayerHolder DEFAULT = new()
            {
                entity = new(),
                playerIndex = -1,
                playerUuid = -1,
                active = false,
            };
        }

        private EntityHolder[] entities;
        private PlayerHolder[] players;

        public int MaxEnts => entities.Length;

        public ClientEntityManager()
        {
            entities = new EntityHolder[ViMG.Entities.EntityManager.EntMax];
            Array.Fill(entities, EntityHolder.DEFAULT);

            players = new PlayerHolder[World.MAX_PLAYERS];
        }

        public void NewFrame(ClientEntityManager prev)
        {
            for (int i = 0; i < prev.entities.Length; i++)
            {
                entities[i] = prev.entities[i];
            }

            for (int i = 0; i < World.MAX_PLAYERS; i++)
            {
                players[i] = prev.players[i];
            }
        }

        public void AddPlayer(EntityManager.EntityReference reference, int playerUuid, int playerIndex)
        {
            players[playerIndex] = new PlayerHolder
            {
                active = true,
                entity = reference,
                playerUuid = playerUuid,
                playerIndex = playerIndex,
            };
        }

        public int GetPlayerIndex(EntityManager.EntityReference reference)
        {
            for (int i = 0; i < players.Length; i++)
            {
                if (players[i].entity.id == reference.id && players[i].entity.generation == reference.generation)
                {
                    return i;
                }
            }

            return -1;
        }

        public BasicState GetByRef(ref readonly EntityManager.EntityReference reference)
        {
            if (entities[reference.id].generation != reference.generation) return new();
            else return entities[reference.id].state;
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

            int playerIndex = GetPlayerIndex(reference);
            if (playerIndex != -1)
            {
                players[playerIndex] = PlayerHolder.DEFAULT;
            }
        }
    }
}
