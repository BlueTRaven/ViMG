using BrUtility;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace ViMG.Items
{
	public class ItemBronzeIngot : Item
	{
		public ItemBronzeIngot() : base("ingot_bronze", Main.assetsManager.GetAsset<Texture2D>("swrod"), new RectangleF(112, 16, 16, 16))
		{
			name = "Bronze Ingot";
			description = "An ingot of bronze, made from alloying copper and tin. Can be made into a variety of shapes and tools. Welcome to the bronze age.";
		}
	}
}
