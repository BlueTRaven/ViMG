using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViMG.Buffs
{
    public class BuffShimuBlessing : Buff
    {
        public BuffShimuBlessing() : base("shimu_blessing", 999f, 0f)
        {
            Name = "Shimu's Blessing";
            Description = "Blessed by Shimu, the Goddess of the Mines.\n" +
                "+20% Mining Speed\n" +
                "Ores are highlighted";

            Tags.Add("blessing");
            Tags.Add("emissive_ores");
        }

        public override void Update(double deltaTime, ref BuffInstance buffInstance, ref Player.AccumulatedStats stats)
        {
            base.Update(deltaTime, ref buffInstance, ref stats);

            stats.MiningScale += 0.2f;
        }
    }
}
