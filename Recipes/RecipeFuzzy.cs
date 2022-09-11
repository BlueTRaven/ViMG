using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Cubes;
using ViMG.Items;

namespace ViMG.Recipes
{
	public class RecipeFuzzy : Recipe
	{
		//Note that recipes with more items in the layout are weighted higher than other weights.
		//This is so that we don't end up in situations like, for instance, where a wood block is crafted with 2 wood items,
		//and a wood bundle is crafted with 4 wood items and 1 string, but since the wood recipe is technically matched and is matched first,
		//it goes with that recipe.
		public RecipeFuzzy(IRecipeCatalyst catalyst, ItemInstance[] layout, ItemInstance[] outputs, int weight = 1) : 
			base(catalyst, layout, outputs, weight == 1 ? layout.Length : weight)
		{
		}

		public override bool Matches(Inventory inventory)
		{
			for (int i = 0; i < Layout.Length; i++)
			{
				if (!ItemMatches(inventory, i))
					return false;
			}

			return true;
		}

		private bool ItemMatches(Inventory inventory, int index)
		{
			ref ItemInstance item = ref Layout[index];
			int req = item.num;

			for (int i = 0; i < inventory.NumSlots; i++)
			{
				if (inventory.Get(i).item == item.item)
				{
					req -= inventory.Get(i).num;

					//If req is less than the number of that item in the inventory, it matches
					if (req <= 0)
						return true;
				}
			}

			return req <= 0;
		}
	}
}
