using BrUtility;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Rendering;

namespace ViMG.Items
{
    public class ItemInfusedBone : Item
	{
		public ItemInfusedBone() : base("brittle_infused_bone")
		{
            name = "Infused Brittle Bone";
			description = "A brittle bone that has been infused with a strange red energy.";
		}

        public override ClientItem ClientInit()
        {
            return new ClientItem(this, new RectangleF(48, 112, 16, 16));
        }
	}
}
