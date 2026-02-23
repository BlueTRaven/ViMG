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
    public class ItemOssifiedHeart : Item
    {
        public ItemOssifiedHeart() : base("heart_ossified")
        {
            name = "Ossified Heart";
            description = "Bone in the shape of a heart. Perhaps it was once a true heart, and disease turned it to bone.\n" +
                "+15 max hp";

            Tags.Add("gear_heart");
        }

        protected override ClientItem ClientInit()
        {
            return new ClientItem(this, new RectangleF(160, 43, 16, 21));
        }

        public override void AccumulateStats(Player player, Inventory inventory, int index, ref PlayerAccumulatedStats stats, ref SetBonus.SetBonusInstance bonus)
        {
            base.AccumulateStats(player, inventory, index, ref stats, ref bonus);

            stats.HPFlat += 15;
        }
    }
}
