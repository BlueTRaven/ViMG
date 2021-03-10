using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Cubes;
using ViMG.Items;

namespace ViMG.Recipes
{
	public abstract class Recipe
	{
		public readonly CubeCatalyst Catalyst;
		public readonly ItemInstance[] Layout;
		public readonly ItemInstance[] Outputs;

		public Recipe(CubeCatalyst catalyst, ItemInstance[] layout, ItemInstance[] outputs)
		{
			this.Layout = layout;
			this.Catalyst = catalyst;
			this.Outputs = outputs;
		}

		public abstract bool Matches(Inventory inventory);
	}
}
