using BrUtility;
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
    public class ItemPotato : Item
    {
        private static Buffs.Buff.BuffInstance[] buffs = new Buffs.Buff.BuffInstance[] 
        {
            new(Main.Registry.BuffRegistry.Get("well_fed"), 30f),
            new(Main.Registry.BuffRegistry.Get("str_up"), 3f * 60f, 5)
        };

        public ItemPotato() : base("food_potato", StaticMaterials.Items, new RectangleF(192, 80, 16, 16))
        {
            name = "Potato";
            description = "A hearty potato.\n" +
                "Grants Well Fed for 30 seconds and 5% Strength Up for 3 minutes.";
        }

        public override bool RightClick(Player player, Inventory inventory, int index, Vector3 facing, out Player.ActionStats actionStats)
        {
            base.RightClick(player, inventory, index, facing, out actionStats);

            player.GetBuffManager().AddBuffs(buffs);

            inventory.Remove(index, 1);

            return true;
        }
    }
}
