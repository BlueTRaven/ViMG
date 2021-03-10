using BrUtility;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Items;

namespace ViMG.Cubes
{
	public class CubeIronOre : Cube
	{
		public CubeIronOre() : base("ore_iron", new RectangleF(0, 48, 16, 16), Color.White, 8)
		{
		}

		public override void GetDrops(List<ItemInstance> itemsToDrop)
		{
			base.GetDrops(itemsToDrop);

			itemsToDrop.Add(new ItemInstance(Main.Registry.ItemRegistry.Get("iron_chunk"), Main.random.Next(1, 4), 1));
		}
	}
}
