using BrUtility;
using Engine;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Items;

namespace ViMG.Cubes
{
	public class CubeAltarBrick : Cube
	{
		public CubeAltarBrick() : base("altar_brick", 8)
		{
            Client = new(this, new RectangleF(80, 0, 16, 16), Color.White);
        }

		public override void GetDrops(List<ItemInstance> itemsToDrop)
		{
			base.GetDrops(itemsToDrop);

			itemsToDrop.Add(new ItemInstance(GlobalState.Registry.ItemRegistry.Get("item_altar_brick"), 1, 1));

			if (GlobalState.random.NextDouble() < 1.0 / 200.0)
				itemsToDrop.Add(new ItemInstance(GlobalState.Registry.ItemRegistry.Get("altar_dust"), 1, 1));
		}
	}
}
