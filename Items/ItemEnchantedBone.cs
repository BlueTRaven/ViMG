using BrUtility;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace ViMG.Items
{
	public class ItemEnchantedBone : Item
	{
		public ItemEnchantedBone() : base("brittle_enchanted_bone", Main.assetsManager.GetAsset<Texture2D>("swrod"), new RectangleF(32, 112, 16, 16))
		{
			name = "Enchanted Brittle Bone";
			description = "A brittle bone that has been enchanted with glow dust.";
		}
	}
}
