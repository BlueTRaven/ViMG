using BrUtility;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace ViMG.Items
{
	public class ItemTinIngot : Item
	{
		public ItemTinIngot() : base("tin_ingot", Main.assetsManager.GetAsset<Texture2D>("swrod"), new RectangleF(96, 0, 16, 16))
		{
			name = "Tin Ingot";
			description = "A refined chunk of tin ore. Can be made into a variety of shapes and tools.";
		}
	}
}
