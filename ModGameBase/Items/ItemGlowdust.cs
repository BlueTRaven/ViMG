using BrUtility;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Rendering;

namespace ViMG.Items
{
    public class ItemGlowdust : Item
	{
		public ItemGlowdust() : base("glowdust", StaticMaterials.Items, new RectangleF(16, 16, 16, 16))
		{
			name = "Glowdust";
			description = "A strange glowing dust that sticks to your fingers.";
		}
	}
}
