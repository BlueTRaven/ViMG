using Engine.Entities;
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

        public virtual void AccumulateStats(Player player, ref PlayerAccumulatedStats stats)
        {

        }

        public virtual string GetDescription()
        {
            return "";
        }
    }

    public class SetBonusMaterial : SetBonus
    {
        private readonly string material;
        private readonly PlayerAccumulatedStats stats;

        public SetBonusMaterial(string material, PlayerAccumulatedStats stats)
        {
            this.material = material;
            this.stats = stats;
        }

        public override void AccumulateStats(Player player, ref PlayerAccumulatedStats stats)
        {
            base.AccumulateStats(player, ref stats);

            stats += this.stats;
        }
    }
}
