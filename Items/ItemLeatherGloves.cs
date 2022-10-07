using BrUtility;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViMG.Items
{
    public class ItemLeatherGloves : Item
    {
        public ItemLeatherGloves() : base("leather_gloves", Main.assetsManager.GetAsset<Texture2D>("swrod"), new RectangleF(48, 48, 16, 16))
        {
            name = "Leather Gloves";
            description = "A pair of sturdy leather gloves.\n" +
                "+3 defense\n" +
                "Hitting an enemy increases your attack speed by 10% for 3 seconds. This effect cannot occur more than once every 10 seconds.";

            Tags.Add("accessory");
        }

        public override void AccumulateStats(Player player, Inventory inventory, int index, ref Player.AccumulatedStats stats, ref SetBonus.SetBonusInstance bonus)
        {
            base.AccumulateStats(player, inventory, index, ref stats, ref bonus);

            stats.DefenseFlat += 3;
        }

        public override void OnDealDamage(Player player, Inventory inventory, int index, IHitboxOwner hit)
        {
            base.OnDealDamage(player, inventory, index, hit);

            player.GetBuffManager().AddBuffUnique(new Buffs.BuffLeatherGlove(player));
        }
    }
}
