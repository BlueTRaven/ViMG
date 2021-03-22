using BrUtility;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace ViMG.Items
{
	public class ItemArrow : Item
	{
		public ItemArrow() : base("arrow", Main.assetsManager.GetAsset<Texture2D>("swrod"), new RectangleF(16, 48, 16, 16))
		{
		}
	}
}
