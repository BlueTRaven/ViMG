using BrUtility;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Items;

namespace ViMG.Cubes
{
	public class CubeDirt : Cube
	{
		public CubeDirt() : base("dirt", 2)
		{
			Name = "Dirt";

			Client = new ClientCube(this, new RectangleF(0, 0, 16, 16), Color.White);
		}

		public override void GetDrops(List<ItemInstance> itemsToDrop)
		{
			base.GetDrops(itemsToDrop);

			itemsToDrop.Add(new ItemInstance(Main.Registry.ItemRegistry.Get("item_dirt"), 1, 1));
		}
	}
}
