using BrUtility;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Items;

namespace ViMG.Cubes
{
	public class CubeTinOre : Cube
	{
		public CubeTinOre() : base("ore_tin", new RectangleF(32, 48, 16, 16), Color.White, 5)
		{
			if (Main.TRANSPARENT_ORES)
				Transparency = TransparencyValue.Transparent;
		}

		public override void GetDrops(List<ItemInstance> itemsToDrop)
		{
			base.GetDrops(itemsToDrop);

			itemsToDrop.Add(new ItemInstance(Main.Registry.ItemRegistry.Get("tin_chunk"), Main.random.Next(1, 4), 1));
		}
	}
}
