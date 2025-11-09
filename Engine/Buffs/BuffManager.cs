using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Entities;

namespace ViMG.Buffs
{
    public class BuffManager : IBuffManager
    {
        private readonly IHasStats stats;
        private List<Buff.BuffInstance> buffs = new List<Buff.BuffInstance>();
        private HashSet<Buff> buffTypes = new HashSet<Buff>();
        private Dictionary<string, List<Buff.BuffInstance>> buffTags = new Dictionary<string, List<Buff.BuffInstance>>();

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

                instance.buff.Update(deltaTime, this, ref instance, ref currentStats);

                stats.SetStats(currentStats);

                if (instance.duration <= 0 || !instance.valid)
                {
                    buffs.RemoveAt(i);
                    buffTypes.Remove(instance.buff);

                    foreach (string tag in instance.buff.Tags)
                    {
                        buffTags[tag].Remove(instance);
                    }

                    i--;
                }
                else
                {
                    buffs[i] = instance;
                }
            }
        }

        public void OnTakeDamage(Entity entity, int damage, HitboxManager.Hitbox hitbox)
        {
            buffs.ForEach(x => x.buff.OnTakeDamage(entity, this, damage, hitbox));
        }

        public List<Buff.BuffInstance> GetBuffs()
        {
            return buffs;
        }

        public void AddBuffs(Buff.BuffInstance[] buffs)
        {
            if (buffs != null)
            {
                for (int i = 0; i < buffs.Length; i++)
                {
                    AddBuff(buffs[i]);
                }
            }
        }

        public void AddBuff(Buff.BuffInstance buffInstance)
        {
            if (buffTypes.Contains(buffInstance.buff))
            {
                foreach (Buff.BuffInstance existingBuff in buffs)
                {
                    if (existingBuff.buff == buffInstance.buff)
                        existingBuff.buff.OnApplyOfSameType(existingBuff);
                }
            }
            else
            {
                this.buffs.Add(buffInstance);
                this.buffTypes.Add(buffInstance.buff);
                foreach (string tag in buffInstance.buff.Tags)
                {
                    if (!buffTags.ContainsKey(tag))
                        buffTags.Add(tag, new List<Buff.BuffInstance>());

                    buffTags[tag].Add(buffInstance);
                }
            }
        }

        public bool HasBuff(Buff buff)
        {
            return buffTypes.Contains(buff);
        }

        public bool HasBuff(string tag)
        {
            return buffTags.ContainsKey(tag);
        }

        public List<Buff.BuffInstance> GetWithTag(string tag)
        {
            return buffTags.ContainsKey(tag) ? buffTags[tag] : null;
        }
    }
}
