using BrUtility;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Entities.Renderers;

namespace ViMG.Items
{
    public class ItemHeart : Item
    {
        public ItemHeart() : base("heart", StaticMaterials.Items, new RectangleF(128, 43, 16, 21))
        {

        }

        public override void AccumulateStats(Player player, Inventory inventory, int index, ref Player.AccumulatedStats stats, ref SetBonus.SetBonusInstance bonus)
        {
            base.AccumulateStats(player, inventory, index, ref stats, ref bonus);


        }
    }
}
