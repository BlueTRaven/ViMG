using Engine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Entities;

namespace ViMG.Buffs
{
    public class BuffIratBlessing : Buff
    {
        public BuffIratBlessing() : base("irat_blessing", 999f, 0)
        {
            Name = "Irat's Blessing";
            Description = "Blessed by Irat, the Goddess of Vengeance.";

            Tags.Add("blessing");
        }

        public override void OnTakeDamage(Entity entity, IBuffManager buffManager, int damage, HitboxManager.Hitbox hitbox)
        {
            base.OnTakeDamage(entity, buffManager, damage, hitbox);

            buffManager.AddBuff(new BuffInstance(GlobalState.Registry.BuffRegistry.Get("irat_vengeance"), 15f));
        }
    }
}
