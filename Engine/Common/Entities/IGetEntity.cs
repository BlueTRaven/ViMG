using Engine.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Entities;

namespace Engine.Common.Entities
{
    public interface IGetEntity
    {
        BasicState GetByRef(ref readonly EntityManager.EntityReference reference);
    }
}
