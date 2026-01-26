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
    public class ItemFeatherRelic : Item
    {
        public ItemFeatherRelic() : base("dj_feather_relic")
        {
            name = "Feather Relic";
            description = "An ancient stone relic bearing the symbol of a feather. It feels as light as the symbol placed upon it would be.\n" +
                "Allows you to jump an additional time. Press <Spacebar> while in the air to use it.";

            Tags.Add("gear_dj");
        }

        protected override ClientItem ClientInit()
        {
            return new ClientItem(this, new RectangleF(112, 48, 16, 16));
        }

        public override void AccumulateStats(Player player, Inventory inventory, int index, ref Player.AccumulatedStats stats, ref SetBonus.SetBonusInstance bonus)
        {
            base.AccumulateStats(player, inventory, index, ref stats, ref bonus);

            stats.AddJumpEffect(DefaultJumpEffect.Instance);
            stats.JumpNum++;
        }
    }
}
