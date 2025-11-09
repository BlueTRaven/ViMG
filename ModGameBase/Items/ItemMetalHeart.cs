using BrUtility;
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
        public ItemMetalHeart() : base("heart_metal", StaticMaterials.Items, new RectangleF(144, 43, 16, 21))
        {
            name = "Metal Heart";
            description = "An intricately carved block of solid steel in the shape of a heart. Makes you feel uneasy.\n" +
                "+10 max hp";
            
            Tags.Add("gear_heart");
        }

        public override void AccumulateStats(Player player, Inventory inventory, int index, ref Player.AccumulatedStats stats, ref SetBonus.SetBonusInstance bonus)
        {
            base.AccumulateStats(player, inventory, index, ref stats, ref bonus);

            stats.HPFlat += 10;
        }
    }
}
