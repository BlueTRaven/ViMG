using BrUtility;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Items;

namespace ViMG.Cubes
{
	public class CubeAltarBrick : Cube
	{
		public CubeAltarBrick() : base("altar_brick", new RectangleF(80, 0, 16, 16), Color.White, 8)
		{
		}

		public override void GetDrops(List<ItemInstance> itemsToDrop)
		{
			base.GetDrops(itemsToDrop);

			itemsToDrop.Add(new ItemInstance(Main.Registry.ItemRegistry.Get("item_altar_brick"), 1, 1));

			if (Main.random.NextDouble() < 1.0 / 200.0)
				itemsToDrop.Add(new ItemInstance(Main.Registry.ItemRegistry.Get("altar_dust"), 1, 1));
		}
	}
}
