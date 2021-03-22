using BrUtility;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace ViMG.Items
{
	public class ItemBrittleBone : Item
	{
		public ItemBrittleBone() : base("brittle_bone", Main.assetsManager.GetAsset<Texture2D>("swrod"), new RectangleF(16, 112, 16, 16))
		{
			name = "Brittle Bone";
			description = "An ancient and brittle bone, so ancient it might dissolve in your hands. Drops from skeletons.";
		}
	}
}
