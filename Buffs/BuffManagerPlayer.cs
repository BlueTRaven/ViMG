using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Entities;

namespace ViMG.Buffs
{
    public class BuffManagerPlayer : IBuffManager
    {
        private readonly Player player;
        private List<Buff.BuffInstance> toAddLater = new List<Buff.BuffInstance>();
        private List<Buff.BuffInstance> buffs = new List<Buff.BuffInstance>();
        private HashSet<Type> buffTypes = new HashSet<Type>();
        private Dictionary<string, List<Buff.BuffInstance>> buffTags = new Dictionary<string, List<Buff.BuffInstance>>();

        public BuffManagerPlayer(Player player)
        {
            this.player = player;
        }

        public void Update(double deltaTime, ref Player.AccumulatedStats stats)
        {
            toAddLater.ForEach(x => ReallyAddBuff(x));
            toAddLater.Clear();

            for (int i = buffs.Count - 1; i >= 0; i--)
            {
                Buff.BuffInstance instance = buffs[i];

                instance.buff.Update(deltaTime, ref instance, ref stats);

                if (instance.duration <= 0 || !instance.valid)
                {
                    buffs.RemoveAt(i);
                    buffTypes.Remove(instance.buff.GetType());
                    
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

        public void AddBuffs(Buff.BuffInstance[] buffs)
        {
            for (int i = 0; i < buffs.Length; i++)
            {
                AddBuff(buffs[i]);
            }
        }

        private void ReallyAddBuff(Buff.BuffInstance buffInstance)
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
                foreach (string tag in buffInstance.buff.Tags)
                {
                    if (!buffTags.ContainsKey(tag))
                        buffTags.Add(tag, new List<Buff.BuffInstance>());

                    buffTags[tag].Add(buffInstance);
                }
            }
        }

        public void AddBuff(Buff.BuffInstance buffInstance)
        {
            toAddLater.Add(buffInstance);
        }

        public bool HasBuff(Buff buff)
        {
            return buffTypes.Contains(buff.GetType());
        }

        public bool HasBuff(string buffTag)
        {
            return buffTags.ContainsKey(buffTag) && buffTags[buffTag].Count > 0;
        }

        public List<Buff.BuffInstance> GetWithTag(string tag)
        {
            return buffTags.ContainsKey(tag) ? buffTags[tag] : null;
        }

        public void RemoveAllWithTag(string tag)
        {
            if (buffTags.ContainsKey(tag))
                //Don't remove directly but it'll remove before the next tick.
                buffTags[tag].ForEach(x => x.duration = 0);
        }
    }
}
