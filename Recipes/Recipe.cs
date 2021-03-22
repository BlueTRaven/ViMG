using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Cubes;
using ViMG.Items;

namespace ViMG.Recipes
{
	public abstract class Recipe
	{
		public readonly IRecipeCatalyst Catalyst;
		public readonly ItemInstance[] Layout;
		public readonly ItemInstance[] Outputs;

		public readonly int Weight;

		public Recipe(IRecipeCatalyst catalyst, ItemInstance[] layout, ItemInstance[] outputs, int weight = 1)
		{
			this.Layout = layout;
			this.Catalyst = catalyst;
			this.Outputs = outputs;
			this.Weight = weight;
		}

		public abstract bool Matches(Inventory inventory);
	}
}
