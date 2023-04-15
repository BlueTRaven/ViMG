using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Entities.Renderers;

namespace ViMG.Items
{
    public class ItemPinkPepper : Item
    {
        private static Buffs.Buff.BuffInstance[] buffs = new Buffs.Buff.BuffInstance[] 
        {
            new(Main.Registry.BuffRegistry.Get("well_fed"), 30f),
            new(Main.Registry.BuffRegistry.Get("fire_resist"), 3f * 30f),
        };

        public ItemPinkPepper() : base("food_pink_pepper", StaticMaterials.Items, new RectangleF(176, 80, 16, 16))
        {
            name = "Pink Pepper";
            description = "A sweet-then-bitter tasting pepper. It appears to be native to this island...\n" +
                "Grants Well Fed for 30 seconds and resistance to On Fire for 3 minutes.";
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
