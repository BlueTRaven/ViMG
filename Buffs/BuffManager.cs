using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViMG.Buffs
{
    public class BuffManager
    {
        private readonly IHasStats stats;
        private List<Buff> buffs = new List<Buff>();

        public BuffManager(IHasStats stats)
        {
            this.stats = stats;
        }

        public void Update(double deltaTime)
        {
            for (int i = buffs.Count - 1; i >= 0; i--)
            {
                Buff buff = buffs[i];

                Stats currentStats = stats.GetStats();

                buff.Update(deltaTime, ref currentStats);

                stats.SetStats(currentStats);

                if (buff.Duration <= 0)
                {
                    buffs.RemoveAt(i);
                    i--;
                }
            }
        }
    }
}
