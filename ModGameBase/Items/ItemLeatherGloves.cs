using BrUtility;
using Engine;
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
    public class ItemLeatherGloves : Item
    {
        public ItemLeatherGloves() : base("leather_gloves")
        {
            name = "Leather Gloves";
            description = "A pair of sturdy leather gloves.\n" +
                "+3 defense\n" +
                "Hitting an enemy increases your attack speed by 10% for 3 seconds. This effect cannot occur more than once every 10 seconds.";

            Tags.Add("accessory");
        }

        protected override ClientItem ClientInit()
        {
            return new ClientItem(this, new RectangleF(48, 48, 16, 16));
        }

        public override void AccumulateStats(Player player, Inventory inventory, int index, ref Player.AccumulatedStats stats, ref SetBonus.SetBonusInstance bonus)
        {
            base.AccumulateStats(player, inventory, index, ref stats, ref bonus);

            stats.DefenseFlat += 3;
        }

        public override void OnDealDamage(Player player, Inventory inventory, int index, HitboxManager.Hitbox otherHitbox)
        {
            base.OnDealDamage(player, inventory, index, otherHitbox);

            player.GetBuffManager().AddBuff(new Buffs.Buff.BuffInstance(GlobalState.Registry.BuffRegistry.Get("leather_glove")));
        }
    }
}
