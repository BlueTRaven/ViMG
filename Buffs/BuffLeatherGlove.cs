using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViMG.Buffs
{
    public class BuffLeatherGlove : Buff
    {
        public BuffLeatherGlove() : base("leather_glove", 13, 0)
        {
            Name = "Wild Hands";
            Description = "Attack speed increased by 10%.";
        }

        public override void Update(double deltaTime, ref BuffInstance buffInstance, ref Player.AccumulatedStats stats)
        {
            base.Update(deltaTime, ref buffInstance, ref stats);

            //10 seconds = cd time
            if (buffInstance.duration > 10f)
                stats.MeleeSpdScale += 0.5f;
            else
            {
                Name = "Wild Hands (Cooldown)";
                Description = "Wild Hands cannot occur more than once every ten seconds.";
            }
        }
    }
}
