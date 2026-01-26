using BrUtility;
using Engine;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Items;

namespace ViMG.Cubes
{
	public class CubeBrittleBone : Cube
	{
		public CubeBrittleBone() : base("brittle_bone_block", 6)
		{
			Transparency = TransparencyValue.Transparent;

            Client = new(this, new CubeFacingLayout(new RectangleF(80, 32, 16, 16), new RectangleF(96, 32, 16, 16), new RectangleF(96, 32, 16, 16)),
            Color.White);
        }

		public override void GetDrops(List<ItemInstance> itemsToDrop)
		{
			base.GetDrops(itemsToDrop);

			itemsToDrop.Add(new ItemInstance(GlobalState.Registry.ItemRegistry.Get("brittle_bone"), GlobalState.random.Next(1, 4), 1));
		}
	}
}
