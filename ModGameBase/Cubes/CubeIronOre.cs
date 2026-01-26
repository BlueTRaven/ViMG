using BrUtility;
using Engine;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Items;

namespace ViMG.Cubes
{
	public class CubeIronOre : Cube
	{
		public CubeIronOre() : base("ore_iron", 8)
		{
			if (Main.TRANSPARENT_ORES)
				Transparency = TransparencyValue.Transparent;

			Client = new(this, new RectangleF(0, 48, 16, 16), Color.White);
		}

		public override void GetDrops(List<ItemInstance> itemsToDrop)
		{
			base.GetDrops(itemsToDrop);

			itemsToDrop.Add(new ItemInstance(GlobalState.Registry.ItemRegistry.Get("iron_chunk"), GlobalState.random.Next(1, 4), 1));
		}
	}
}
