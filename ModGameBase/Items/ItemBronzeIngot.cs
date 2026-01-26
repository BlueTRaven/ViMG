using BrUtility;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Rendering;

namespace ViMG.Items
{
    public class ItemBronzeIngot : Item
	{
		public ItemBronzeIngot() : base("ingot_bronze")
		{
            name = "Bronze Ingot";
			description = "An ingot of bronze, made from alloying copper and tin. Can be made into a variety of shapes and tools. Welcome to the bronze age.";
		}

        protected override ClientItem ClientInit()
        {
            return new ClientItem(this, new RectangleF(112, 16, 16, 16));
        }
	}
}
