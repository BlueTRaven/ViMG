using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Entities;

namespace ViMG.Buffs
{
    public interface IBuffManager
    {
        void OnTakeDamage(Entity entity, int damage, HitboxManager.Hitbox hitbox);

        List<Buff.BuffInstance> GetBuffs();

        void AddBuff(Buff.BuffInstance buffInstance);

        void AddBuffs(Buff.BuffInstance[] buffs);

        bool HasBuff(Buff buff);

        bool HasBuff(string tag);

        List<Buff.BuffInstance> GetWithTag(string tag);
    }
}
