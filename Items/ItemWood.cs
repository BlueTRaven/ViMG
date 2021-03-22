using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace ViMG.Items
{
	public class ItemWood : Item
	{
		public ItemWood() : base("wood", Main.assetsManager.GetAsset<Texture2D>("swrod"), new RectangleF(0, 32, 16, 16))
		{
			name = "Wood Log";
			description = "A log of wood. ...kinda looks like bacon, doesn't it? No, you can't eat it.";
		}
	}
}
