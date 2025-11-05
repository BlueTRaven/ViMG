using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Entities;

namespace ViMG.Client
{
    public class ClientEntityManager
    {
        private List<ClientEntity> entities = new List<ClientEntity>();

        public ClientEntityManager(EntityManager entityManager)
        {
            entityManager.OnEntityAdded += OnEntityAdded;
            entityManager.OnEntityRemoved += OnEntityRemoved;
        }

        private void OnEntityAdded(Entity entity)
        {
            Activator.CreateInstance(Main.Registry.ClientEntityRegistry.Get<Player>().entityType, entity);
        }

        private void OnEntityRemoved(Entity entity)
        {
            entities.RemoveAll(x => x.entity == entity);
        }
    }
}
