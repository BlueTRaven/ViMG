using Engine.Common.Entities;
using Engine.Networking;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG;
using ViMG.Entities;
using ViMG.Entities.Renderers;

namespace Engine.Clients.Entities
{
    public class ClientEntityManager : IGetEntity
    {
        private struct EntityHolder
        {
            public int id;
            public int generation;
            public bool active;
            public string entityType;
            public int entityTypeId;
            public BasicState state;

            public static EntityHolder DEFAULT = new()
            {
                id = -1,
                generation = -1,
                active = false,
                entityTypeId = 0,
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

        public void NewFrame(ClientEntityManager prev, double deltaTime)
        {
            for (int i = 0; i < prev.entities.Length; i++)
            {
                entities[i] = prev.entities[i];
                entities[i].state.aliveTime += (float)deltaTime;
            }

            for (int i = 0; i < World.MAX_PLAYERS; i++)
            {
                players[i] = prev.players[i];
            }
        }

        public void Update(double deltaTime)
        {
            for (int i = 0; i < entities.Length; i++)
            {
                entities[i].state.aliveTime += (float)deltaTime;
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

        public void RemovePlayer(EntityManager.EntityReference reference)
        {
            var playerIndex = GetPlayerIndex(reference);
            Debug.Assert(playerIndex != -1);
            players[playerIndex] = PlayerHolder.DEFAULT;
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

        public EntityManager.EntityReference GetPlayerRef(int playerIndex)
        {
            return players[playerIndex].entity;
        }

        public EntityManager.EntityReference GetLocalPlayerRef()
        {
            for (int i = 0; i < players.Length; i++)
            {
                if (players[i].playerIndex == Main.gameStateManager.TheIsland.netManagerClient.whoAmI)
                {
                    return players[i].entity;
                }
            }

            return new();
        }

        public bool IsActive(ref readonly EntityManager.EntityReference reference)
        {
            return entities[reference.id].generation == reference.generation;
        }

        public BasicState GetByRef(ref readonly EntityManager.EntityReference reference)
        {
            if (entities[reference.id].generation != reference.generation) return new();
            else return entities[reference.id].state;
        }

        public ref BasicState GetByRefPtr(ref readonly EntityManager.EntityReference reference)
        {
            if (entities[reference.id].generation != reference.generation)
            {
                throw new Exception("Generation mismatch");
            }
            return ref entities[reference.id].state;
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

        public int GetTypeById(int id)
        {
            if (id == -1) return 0;
            return entities[id].entityTypeId;
        }

        public void Set(ViMG.Entities.EntityManager.EntityReference reference, string type, BasicState state) 
        {
            var oldGen = entities[reference.id].generation;
            entities[reference.id] = new()
            {
                id = reference.id,
                generation = reference.generation,
                state = state,
                active = true,
                entityType = type,
                entityTypeId = Main.Registry.EntityRegistry.Get(type).Id,
            };

            if (oldGen != reference.generation)
                entities[reference.id].state.aliveTime = 0;
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
