using BrUtility;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Items;

namespace ViMG.Cubes
{
	public class CubeCopperOre : Cube
	{
		public CubeCopperOre() : base("ore_copper", new RectangleF(48, 48, 16, 16), Color.White, 6)
		{
			if (Main.TRANSPARENT_ORES)
				Transparency = TransparencyValue.Transparent;
		}

		public override void GetDrops(List<ItemInstance> itemsToDrop)
		{
			base.GetDrops(itemsToDrop);

			itemsToDrop.Add(new ItemInstance(Main.Registry.ItemRegistry.Get("copper_chunk"), Main.random.Next(1, 4), 1));
		}
	}
}
