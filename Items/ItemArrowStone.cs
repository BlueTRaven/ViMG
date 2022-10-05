using BrUtility;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace ViMG.Items
{
	public class ItemArrowStone : Item
	{
		public ItemArrowStone() : base("arrow_stone", Main.assetsManager.GetAsset<Texture2D>("swrod"), new RectangleF(16, 48, 16, 16))
		{
			Tags.Add("ammo_arrow");
		}
	}
}
