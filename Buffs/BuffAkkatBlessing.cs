using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViMG.Buffs
{
    public class BuffAkkatBlessing : Buff
    {
        public BuffAkkatBlessing() : base("akkat_blessing", 999, 0)
        {
            Name = "Akkat's Blessing";
            Description = "Blessed by Akkat, the God of Vitality.\n" +
                "'Blood is a blessed gift; by it alone you shall find redemption.'" +
                "+50% defense\n" +
                "+50% maximum hp\n" +
                "Cannot heal from ordinary sources. Gain HP by attacking enemies instead. (unimpemented)";

            Tags.Add("blessing");
            Tags.Add("vampiric_med");
            Tags.Add("noheal");
        }

        public override void Update(double deltaTime, ref BuffInstance buffInstance, ref Player.AccumulatedStats stats)
        {
            base.Update(deltaTime, ref buffInstance, ref stats);

            stats.DefenseScale += 0.5f;
            stats.HPScale += 0.5f;
        }
    }
}
