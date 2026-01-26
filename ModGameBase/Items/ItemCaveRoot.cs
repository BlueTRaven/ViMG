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
using ViMG.Buffs;
using ViMG.Rendering;

namespace ViMG.Items
{
    public class ItemCaveRoot : Item
    {
        private static Buffs.Buff.BuffInstance buff = new Buffs.Buff.BuffInstance(GlobalState.Registry.BuffRegistry.Get("well_fed"), 30f);

        public ItemCaveRoot() : base("food_root1")
        {
            Client = new ClientItem(this, new RectangleF(128, 96, 16, 16));

            name = "Cave Root Tuber";
            description = "The tuber of a Cave Root. Hardy and nutrituous, but bitter tasting.\n" +
                "Grants Well Fed for 30 seconds.";
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
