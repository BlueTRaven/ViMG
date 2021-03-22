using BrUtility;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace ViMG.Items
{
	public class ItemInfusedBone : Item
	{
		public ItemInfusedBone() : base("brittle_infused_bone", Main.assetsManager.GetAsset<Texture2D>("swrod"), new RectangleF(48, 112, 16, 16))
		{
			name = "Infused Brittle Bone";
			description = "A brittle bone that has been infused with altar dust.";
		}
	}
}
