using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Entities;
using ViMG.Items;
using ViMG.Recipes;
using ViMG.UIs;

namespace ViMG.Cubes
{
	public class CubeAnvilIron : Cube
	{
		public CubeAnvilIron() : base("anvil_iron", new RectangleF(160, 0, 16, 16), Color.White, 6)
		{
		}

		public override void OnPlayerPlaced(Player player, CubePosition position)
		{
			base.OnPlayerPlaced(player, position);

			player.GetWorld().EntityManager.Add(new EntityAnvilIron(position));
		}

		public override void GetDrops(List<ItemInstance> itemsToDrop)
		{
			base.GetDrops(itemsToDrop);

			itemsToDrop.Add(new ItemInstance(Main.Registry.ItemRegistry.Get("item_anvil_iron"), 1, 1));
		}
	}
}
