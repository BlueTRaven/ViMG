using BrUtility;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Entities.Renderers;

namespace ViMG.Items
{
	public class ItemInfusedBone : Item
	{
		public ItemInfusedBone() : base("brittle_infused_bone", StaticMaterials.Items, new RectangleF(48, 112, 16, 16))
		{
			name = "Infused Brittle Bone";
			description = "A brittle bone that has been infused with a strange red energy.";
		}
	}
}
