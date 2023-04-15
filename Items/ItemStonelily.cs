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
    public class ItemStonelily : Item
    {
        public ItemStonelily() : base("stone_lily", StaticMaterials.Items, new RectangleF(0, 80, 16, 16))
        {
            name = "Stone Lily";
            description = "A lily made of solid stone. Despite its cold exterior, its beautiful appearance warms your heart.\n" +
                "+4 defense\n" +
                "+5% magic damage";

            Tags.Add("accessory");
        }

        public override void AccumulateStats(Player player, Inventory inventory, int index, ref Player.AccumulatedStats stats, ref SetBonus.SetBonusInstance bonus)
        {
            base.AccumulateStats(player, inventory, index, ref stats, ref bonus);

            stats.DefenseFlat += 4;
            stats.MagicAtkScale += 0.05f;
        }
    }
}
