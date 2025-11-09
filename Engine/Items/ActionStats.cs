using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Items;

namespace ViMG.Items
{
    public record struct ActionStats
    {
        public float useTime;
        public float useAnimTime;
        public float preUseTime;

        public UseAnimationType animationType = UseAnimationType.Use;

        public ActionStats(Item.AttackStats attackStats)
        {
            useTime = attackStats.actionStats.useTime;
            useAnimTime = attackStats.actionStats.useAnimTime;

            preUseTime = attackStats.actionStats.preUseTime;
        }

        public ActionStats(float time)
        {
            useTime = time;
            useAnimTime = time;

            preUseTime = 0;
        }
    }
}
