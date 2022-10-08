using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViMG.Buffs
{
    public class BuffManagerPlayer
    {
        private readonly Player player;
        private List<Buff.BuffInstance> buffs = new List<Buff.BuffInstance>();
        private HashSet<Type> buffTypes = new HashSet<Type>();

        public BuffManagerPlayer(Player player)
        {
            this.player = player;
        }

        public void Update(double deltaTime, ref Player.AccumulatedStats stats)
        {
            for (int i = buffs.Count - 1; i >= 0; i--)
            {
                Buff.BuffInstance instance = buffs[i];

                instance.buff.Update(deltaTime, ref instance, ref stats);

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

        public void AddBuff(Buff.BuffInstance buffInstance)
        {
            if (buffTypes.Contains(buffInstance.buff.GetType()))
            {
                foreach (Buff.BuffInstance existingBuff in buffs)
                {
                    if (existingBuff.buff == buffInstance.buff)
                        existingBuff.buff.OnApplyOfSameType(buffInstance);
                }
            }
            else
            {
                this.buffs.Add(buffInstance);
                this.buffTypes.Add(buffInstance.buff.GetType());
            }
        }
    }
}
