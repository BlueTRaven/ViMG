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
		public ItemCopperIngot() : base("ingot_copper")
		{
            name = "Copper Ingot";
			description = "A refined chunk of copper ore. Can be made into a variety of shapes and tools.";
		}

        public override ClientItem ClientInit()
        {
            return new ClientItem(this, new RectangleF(112, 0, 16, 16));
        }
	}
}
