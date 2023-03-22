using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace ViMG.Items
{
	public class ItemFlaskHealthPotion1 : Item
	{
		public ItemFlaskHealthPotion1() : base("flask_healthpotion1", Main.assetsManager.GetAsset<Texture2D>("swrod"), new RectangleF(16, 96, 16, 16))
		{
			name = "Health Potion 1";
			description = "A health potion. It smells surprisingly nice.";
		}

        public override bool RightClick(Player player, Inventory inventory, int index, Vector3 facing, out Player.ActionStats actionStats)
        {
			player.Heal(10);

			inventory.Remove(index, 1);

			return base.RightClick(player, inventory, index, facing, out actionStats);
        }
    }
}
