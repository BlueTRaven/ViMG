using BrUtility;
using Engine;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Entities;
using ViMG.GameStates;
using ViMG.Items;

namespace ViMG.Cubes
{
	public class CubeTree : Cube
	{
		public CubeTree() : base("tree", 2)
		{
			Transparency = TransparencyValue.Invisible;

            Client = new(this, new RectangleF(64, 0, 16, 16), Color.White);
        }

		public override void GetDrops(List<ItemInstance> itemsToDrop)
		{
			base.GetDrops(itemsToDrop);

			itemsToDrop.Add(new ItemInstance(GlobalState.Registry.ItemRegistry.Get("wood"), 1, 1));
		}
	}
}
