using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViMG.Buffs
{
    public interface IHasStats
    {
        Stats GetStats();

        void SetStats(Stats stats);
    }
}
