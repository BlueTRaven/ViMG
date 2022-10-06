using BrUtility;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViMG.Items
{
    public class ItemMetalHeart : Item
    {
        public ItemMetalHeart() : base("metal_heart", Main.assetsManager.GetAsset<Texture2D>("swrod"), new RectangleF(64, 64, 16, 16))
        {
            name = "Metal Heart";
            description = "A block of solid steel in the shape of a heart. Makes you feel uneasy.\n" +
                "-3 defense\n" +
                "+10% max hp\n" +
                "+3% health regen (unimplemented)";

            Tags.Add("accessory");
        }

        public override void AccumulateStats(Player player, Inventory inventory, int index, ref Player.AccumulatedStats stats, ref SetBonus.SetBonusInstance bonus)
        {
            base.AccumulateStats(player, inventory, index, ref stats, ref bonus);

            stats.DefenseFlat -= 3; //-3 defense,
            stats.HPScale += 0.1f;  //+10% max hp,
            //stats.HPRegen += 0.03f;   //+3% health regen
        }
    }
}
