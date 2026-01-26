using BrUtility;
using Engine;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Items;

namespace ViMG.Cubes
{
	public class CubeCopperOre : Cube
	{
		public CubeCopperOre() : base("ore_copper", 6)
		{
			if (Main.TRANSPARENT_ORES)
				Transparency = TransparencyValue.Transparent;
        }

        public override ClientCube ClientInit()
        {
            return new(this, new RectangleF(48, 48, 16, 16), Color.White);
        }

		public override void GetDrops(List<ItemInstance> itemsToDrop)
		{
			base.GetDrops(itemsToDrop);

			itemsToDrop.Add(new ItemInstance(GlobalState.Registry.ItemRegistry.Get("copper_chunk"), GlobalState.random.Next(1, 4), 1));
		}
	}
}
