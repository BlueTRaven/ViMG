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
    public class ItemMetalHeart : Item
    {
        public ItemMetalHeart() : base("heart_metal")
        {
            name = "Metal Heart";
            description = "An intricately carved block of solid steel in the shape of a heart. Makes you feel uneasy.\n" +
                "+10 max hp";
            
            Tags.Add("gear_heart");
        }

        protected override ClientItem ClientInit()
        {
            return new ClientItem(this, new RectangleF(144, 43, 16, 21));
        }

        public override void AccumulateStats(Player player, Inventory inventory, int index, ref PlayerAccumulatedStats stats, ref SetBonus.SetBonusInstance bonus)
        {
            base.AccumulateStats(player, inventory, index, ref stats, ref bonus);

            stats.HPFlat += 10;
        }
    }
}
