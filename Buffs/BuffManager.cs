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
        private List<Buff.BuffInstance> buffs = new List<Buff.BuffInstance>();
        private HashSet<Type> buffTypes = new HashSet<Type>();

        public BuffManager(IHasStats stats)
        {
            this.stats = stats;
        }

        public void Update(double deltaTime)
        {
            for (int i = buffs.Count - 1; i >= 0; i--)
            {
                Buff.BuffInstance instance = buffs[i];

                Stats currentStats = stats.GetStats();

                instance.buff.Update(deltaTime, ref instance, ref currentStats);

                stats.SetStats(currentStats);

                if (instance.duration <= 0 || !instance.valid)
                {
                    buffs.RemoveAt(i);
                    buffTypes.Remove(instance.GetType());
                    i--;
                }
                else
                {
                    buffs[i] = instance;
                }
            }
        }

        public void AddBuffs(Buff.BuffInstance[] buffs)
        {
            for (int i = 0; i < buffs.Length; i++)
            {
                AddBuff(buffs[i]);
            }
        }

        public void AddBuff(Buff.BuffInstance buffInstance)
        {
            if (buffTypes.Contains(buffInstance.GetType()))
            {
                foreach (Buff.BuffInstance existingBuff in buffs)
                {
                    if (existingBuff.buff == buffInstance.buff)
                        existingBuff.buff.OnApplyOfSameType(buffInstance);
                }
            }
            else this.buffs.Add(buffInstance);
        }
    }
}
