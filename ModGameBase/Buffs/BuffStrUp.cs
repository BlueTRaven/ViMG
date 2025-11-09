using BrUtility;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViMG.Buffs
{
    public class BuffStrUp : Buff
    {
        public BuffStrUp() : base("str_up", 999f, 0, Main.assetsManager.GetAsset<Texture2D>("skill"), new RectangleF(32, 160, 16, 16))
        {
            Name = "Strength Up";
            Description = "You feel stronger.";
        }

        public override void Update(double deltaTime, IBuffManager manager, Player player, ref BuffInstance buffInstance, ref Player.AccumulatedStats stats)
        {
            base.Update(deltaTime, manager, player, ref buffInstance, ref stats);

            stats.MeleeAtkScale += 0.1f * buffInstance.stack;
        }

        public override void Update(double deltaTime, IBuffManager manager, ref BuffInstance buffInstance, ref Stats stats)
        {
            base.Update(deltaTime, manager, ref buffInstance, ref stats);

            stats.Damage += buffInstance.stack;
        }
    }
}
