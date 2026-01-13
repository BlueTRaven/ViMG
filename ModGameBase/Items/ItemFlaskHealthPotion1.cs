using BrUtility;
using Engine.Items;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Rendering;

namespace ViMG.Items
{
    public class ItemFlaskHealthPotion1 : Item
	{
		public ItemFlaskHealthPotion1() : base("flask_healthpotion1")
		{
            Client = new ClientItem(this, new RectangleF(16, 96, 16, 16));

            name = "Health Potion 1";
			description = "A health potion. It smells surprisingly nice.";
		}

        public override bool RightClick(Player player, Inventory inventory, int index, Vector3 facing, out ActionStats actionStats)
        {
			player.Heal(10);

			inventory.Remove(index, 1);

			return base.RightClick(player, inventory, index, facing, out actionStats);
        }
    }
}
