using BrUtility;
using Engine.Entities;
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
    public class ItemStonelily : Item
    {
        public ItemStonelily() : base("stone_lily")
        {
            name = "Stone Lily";
            description = "A lily made of solid stone. Despite its cold exterior, its beautiful appearance warms your heart.\n" +
                "+4 defense\n" +
                "+5% magic damage";

            Tags.Add("accessory");
        }

        protected override ClientItem ClientInit()
        {
            return new ClientItem(this, new RectangleF(0, 80, 16, 16));
        }

        public override void AccumulateStats(Player player, Inventory inventory, int index, ref PlayerAccumulatedStats stats, ref SetBonus.SetBonusInstance bonus)
        {
            base.AccumulateStats(player, inventory, index, ref stats, ref bonus);

            stats.DefenseFlat += 4;
            stats.MagicAtkScale += 0.05f;
        }
    }
}
