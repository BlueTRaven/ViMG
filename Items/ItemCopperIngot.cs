using BrUtility;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace ViMG.Items
{
	public class ItemCopperIngot : Item
	{
		public ItemCopperIngot() : base("copper_ingot", Main.assetsManager.GetAsset<Texture2D>("swrod"), new RectangleF(112, 0, 16, 16))
		{
			name = "Copper Ingot";
			description = "A refined chunk of copper ore. Can be made into a variety of shapes and tools.";
		}
	}
}
