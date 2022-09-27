using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViMG.Items
{
    public abstract class SetBonus
    {
        public struct SetBonusInstance
        {
            public SetBonus SetBonus;
            public int Count;
        }

        public virtual void AccumulateStats(Player player, ref Player.AccumulatedStats stats)
        {

        }
    }

    public class SetBonusMaterial : SetBonus
    {
        private readonly string material;
        private readonly Player.AccumulatedStats stats;

        public SetBonusMaterial(string material, Player.AccumulatedStats stats)
        {
            this.material = material;
            this.stats = stats;
        }

        public override void AccumulateStats(Player player, ref Player.AccumulatedStats stats)
        {
            base.AccumulateStats(player, ref stats);

            stats += this.stats;
        }
    }
}
