using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViMG.Buffs
{
    public class BuffGidamuBlessing : Buff
    {
        public BuffGidamuBlessing() : base("gidamu_blessing", 999, 0)
        {
            Name = "Gidamu's Blessing";
            Description = "Blessed by Gidamu, the Goddess of Stone.\n" +
                "+50% defense\n" +
                "-50% move speed";

            Tags.Add("blessing");
        }

        public override void Update(double deltaTime, ref BuffInstance buffInstance, ref Player.AccumulatedStats stats)
        {
            base.Update(deltaTime, ref buffInstance, ref stats);

            stats.Speed *= 0.5f;
            stats.Acceleration *= 0.5f;
        }
    }
}
