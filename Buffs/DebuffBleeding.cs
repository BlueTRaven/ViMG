using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViMG.Buffs
{
    public class DebuffBleeding : Buff
    {
        public DebuffBleeding() : base("bleeding", 999f * 60f, 3.5f)
        {
        }

        public override void Tick(double deltaTime, IBuffManager manager, ref BuffInstance buffInstance, ref Stats stats)
        {
            base.Tick(deltaTime, manager, ref buffInstance, ref stats);

            stats.HP -= 1;
            stats.TintColor = Color.DarkRed;
        }
    }
}
