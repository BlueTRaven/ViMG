using BrUtility;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace ViMG.Items
{
	public class ItemGlowdust : Item
	{
		public ItemGlowdust() : base("glowdust", Main.assetsManager.GetAsset<Texture2D>("swrod"), new RectangleF(16, 16, 16, 16))
		{
			Name = "Glowdust";
			Description = "A strange glowing dust that sticks to your fingers.";
		}
	}
}
