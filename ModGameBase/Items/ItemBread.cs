using BrUtility;
using Engine;
using Engine.Items;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Rendering;

namespace ViMG.Items
{
    public class ItemBread : Item
    {
        private static Buffs.Buff.BuffInstance buff = new Buffs.Buff.BuffInstance(GlobalState.Registry.BuffRegistry.Get("well_fed"), 60f * 5f);

        public ItemBread() : base("food_bread1")
        {
            name = "Agaldam Bread";
            description = "A thick, dry, brick-like loaf of bread. If your teeth survive eating this, " +
                "it's said one slice provides enough nutrients for a single meal.\n" +
                "Grants Well Fed for 5 minutes.";
        }

        protected override ClientItem ClientInit()
        {
            return new ClientItem(this, new RectangleF(96, 96, 16, 16));
        }

        public override bool RightClick(Player player, Inventory inventory, int index, Vector3 facing, out ActionStats actionStats)
        {
            base.RightClick(player, inventory, index, facing, out actionStats);

            //5 minute buff
            player.GetBuffManager().AddBuff(buff);

            inventory.Remove(index, 1);

            return true;
        }
    }
}
