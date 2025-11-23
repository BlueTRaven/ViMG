using BrUtility;
using Engine.Items;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Rendering;

namespace ViMG.Items
{
    public class ItemSkeletonHead : Item
    {
        public ItemSkeletonHead() : base("skeleton_head", new RectangleF(48, 64, 16, 16))
        {
            name = "Skeleton Head";
            description = "Unlike most skeletons on this strange island, this one doesn't appear to be alive.\n" +
                "+5% attack damage\n" +
                "+5% melee attack speed";

            Tags.Add("accessory");
        }

        public override void AccumulateStats(Player player, Inventory inventory, int index, ref Player.AccumulatedStats stats, ref SetBonus.SetBonusInstance bonus)
        {
            base.AccumulateStats(player, inventory, index, ref stats, ref bonus);

            stats.MeleeAtkScale += 0.05f;
            stats.RangeAtkScale += 0.05f;
            stats.MagicAtkScale += 0.05f;

            stats.MeleeSpdScale += 0.05f;
        }
    }
}
