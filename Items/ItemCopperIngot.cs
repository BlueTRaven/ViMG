using BrUtility;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Rendering;

namespace ViMG.Items
{
    public class ItemCopperIngot : Item
	{
		public ItemCopperIngot() : base("ingot_copper", StaticMaterials.Items, new RectangleF(112, 0, 16, 16))
		{
			name = "Copper Ingot";
			description = "A refined chunk of copper ore. Can be made into a variety of shapes and tools.";
		}
	}
}
