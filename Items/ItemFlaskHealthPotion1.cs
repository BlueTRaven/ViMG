using BrUtility;
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
	}
}
