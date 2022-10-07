using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViMG.Buffs
{
    public class BuffLeatherGlove : Buff
    {
        public BuffLeatherGlove(Player player) : base(player, 13, 1)
        {
            Name = "Wild Hands";
            Description = "Attack speed increased by 10%.";
        }

        public override void Update(double deltaTime, ref Player.AccumulatedStats stats)
        {
            base.Update(deltaTime, ref stats);

            //stats.MeleeSpdScale += 0.1f; //+10%

            //10 seconds = cd time
            if (duration > 10f)
                stats.MeleeSpdScale += 0.5f;
            else
            {
                Name = "Wild Hands (Cooldown)";
                Description = "Wild Hands cannot occur more than once every ten seconds.";
            }
        }
    }
}
