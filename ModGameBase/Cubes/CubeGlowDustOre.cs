using BrUtility;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Items;

namespace ViMG.Cubes
{
	public class CubeGlowDustOre : Cube
	{
		public CubeGlowDustOre() : base("ore_glowdust", 7)
		{
			if (Main.TRANSPARENT_ORES)
				Transparency = TransparencyValue.Transparent;

            Client = new(this, new RectangleF(16, 48, 16, 16), Color.White);
        }

		public override void GetDrops(List<ItemInstance> itemsToDrop)
		{
			base.GetDrops(itemsToDrop);

			int num = Main.random.Next(3, 6);
			
			for (int i = 0; i < num; i++)
			{
				itemsToDrop.Add(new ItemInstance(Main.Registry.ItemRegistry.Get("glowdust"), 1, 1));
			}
		}
	}
}
