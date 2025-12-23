using Engine.Clients.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Clients
{
    public class ClientWorld 
    {
        public ClientEntityManager entities;

        public ClientWorld()
        {
            entities = new ClientEntityManager();
        }

        public void NewFrame(ClientWorld prev)
        {
            entities.NewFrame(prev.entities);
        }
    }
}
