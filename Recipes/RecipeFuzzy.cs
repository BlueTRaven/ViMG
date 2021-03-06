using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Cubes;
using ViMG.Items;

namespace ViMG.Recipes
{
	public class RecipeFuzzy : Recipe
	{
		public RecipeFuzzy(CubeCatalyst catalyst, ItemLayout layout) : base(catalyst, layout)
		{
		}

		public override bool Matches(Inventory inventory)
		{
			for (int i = 0; i < Layout.items.Length; i++)
			{
				if (!ItemMatches(inventory, i))
					return false;
			}

			return true;
		}

		private bool ItemMatches(Inventory inventory, int index)
		{
			ref ItemInstance item = ref Layout.items[index];
			int req = item.num;

			for (int i = 0; i < inventory.NumSlots; i++)
			{
				if (inventory.Get(i).item == item.item)
				{
					req -= inventory.Get(i).num;

					if (req <= 0)
						return true;
				}
			}

			return req <= 0;
		}
	}
}
