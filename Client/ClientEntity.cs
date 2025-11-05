using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Entities;

namespace ViMG.Client
{
    public abstract class ClientEntity
    {
        public Entity entity;

        public ClientEntity(Entity baseEntity)
        {

        }

        public virtual void Update()
        {

        }
    }
}
