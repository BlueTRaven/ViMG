using Engine.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViMG.Buffs
{
    public class BuffIratVengeance : Buff
    {
        public BuffIratVengeance() : base("irat_vengeance", 999f, 0f)
        {
            Name = "Irat's Vengeance";
            Description = "Irat's fury has been incurred.\n" +
                "-10% defense\n" +
                "+50% attack damage";
        }

        public override void Update(double deltaTime, IBuffManager manager, Player player, ref BuffInstance buffInstance, ref PlayerAccumulatedStats stats)
        {
            base.Update(deltaTime, manager, player, ref buffInstance, ref stats);

            if (buffInstance.duration >= 10f)
            {
                stats.DefenseScale -= 0.1f;
                stats.MeleeAtkScale += 0.5f;
                stats.MagicAtkScale += 0.5f;
                stats.RangeAtkScale += 0.5f;
            }
            else
            {
                Name = "Irat's Vengeance (Cooldown)";
                Description = "Irat's Vengeance cannot occur more than once every ten seconds.";
            }
        }
    }
}
