using BrUtility;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Rendering;

namespace ViMG.Items
{
    public class ItemBrittleBone : Item
	{
		public ItemBrittleBone() : base("brittle_bone")
		{
            Client = new ClientItem(this, new RectangleF(166, 112, 16, 16));

            name = "Brittle Bone";
			description = "An ancient and brittle bone, so ancient it might dissolve in your hands. Drops from skeletons.";
		}
	}
}
