using BrUtility;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace ViMG.Items
{
	public class ItemIronIngot : Item
	{
		public ItemIronIngot() : base("iron_ingot", Main.assetsManager.GetAsset<Texture2D>("swrod"), new RectangleF(80, 0, 16, 16))
		{
			name = "Iron Ingot";
			description = "A refined chunk of iron ore. Can be made into a variety of shapes and tools.";
		}
	}
}
