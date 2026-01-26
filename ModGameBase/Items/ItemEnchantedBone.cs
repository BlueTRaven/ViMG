using BrUtility;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Rendering;

namespace ViMG.Items
{
    public class ItemEnchantedBone : Item
	{
		public ItemEnchantedBone() : base("brittle_enchanted_bone")
		{
            name = "Enchanted Brittle Bone";
			description = "A brittle bone that has been enchanted with glow dust.";
		}

        protected override ClientItem ClientInit()
        {
            return new ClientItem(this, new RectangleF(32, 112, 16, 16));
        }
	}
}
