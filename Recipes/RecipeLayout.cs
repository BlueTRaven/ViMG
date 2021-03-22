using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Cubes;
using ViMG.Items;

namespace ViMG.Recipes
{
	public class RecipeLayout : Recipe
	{
		public RecipeLayout(IRecipeCatalyst catalyst, ItemInstance[] layout, ItemInstance[] outputs) : base(catalyst, layout, outputs)
		{
		}

		public override bool Matches(Inventory inventory)
		{
			// If the inventory lacks any items or does not have enough of a certain item, return false
			for (int i = 0; i < Layout.Length; i++)
			{
				if (inventory.Get(i).item != Layout[i].item || inventory.Get(i).num < Layout[i].num)
					return false;
			}

			return true;
		}
	}
}
