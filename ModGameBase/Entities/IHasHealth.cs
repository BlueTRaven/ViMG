using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViMG.Entities
{
    public interface IHasHealth<T> where T : Entity, IHitboxOwner
    {
    }
}
