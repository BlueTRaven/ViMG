using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViMG.Entities
{
    public interface IProjectileEffects
    {
        void OnProjectileDeath(World world, int projectile);
    }
}
