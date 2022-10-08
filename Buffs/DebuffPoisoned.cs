using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViMG.Buffs
{
    public class DebuffPoisoned : Buff
    {
        public DebuffPoisoned() : base("poisoned", 999f * 60f, 0.5f)
        {
        }

        public override void Tick(double deltaTime, ref BuffInstance buffInstance, ref Stats stats)
        {
            base.Tick(deltaTime, ref buffInstance, ref stats);

            stats.HP -= 1;
            stats.TintColor = new Color(89, 125, 34);
        }
    }
}
