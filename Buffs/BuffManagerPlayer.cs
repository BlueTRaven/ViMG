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
        private List<Buff> buffs = new List<Buff>();
        private HashSet<Type> buffTypes = new HashSet<Type>();

        public BuffManagerPlayer(Player player)
        {
            this.player = player;
        }

        public void Update(double deltaTime, ref Player.AccumulatedStats stats)
        {
            for (int i = buffs.Count - 1; i >= 0; i--)
            {
                Buff buff = buffs[i];

                buff.Update(deltaTime, ref stats);

                if (buff.Duration <= 0)
                {
                    buffs.RemoveAt(i);
                    buffTypes.Remove(buff.GetType());
                    i--;
                }
            }
        }

        public void AddBuff(Buff buff)
        {
            if (buffTypes.Contains(buff.GetType()))
            {
                foreach (Buff existingBuff in buffs)
                {
                    if (existingBuff.GetType() == buff.GetType())
                        existingBuff.OnApplyOfSameType(buff);
                }
            }
            else this.buffs.Add(buff);
        }

        public void AddBuffUnique(Buff buff)
        {
            if (buffs.Find(x => x.GetType() == buff.GetType()) == null)
                AddBuff(buff);
        }
    }
}
