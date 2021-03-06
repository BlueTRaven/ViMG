using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Cubes;
using ViMG.Items;

namespace ViMG.Recipes
{
	public abstract class Recipe
	{
		public struct ItemLayout
		{
			public ItemInstance[] items;
		}

		public readonly ItemLayout Layout;
		public readonly CubeCatalyst Catalyst;

		public Recipe(CubeCatalyst catalyst, ItemLayout layout)
		{
			this.Layout = layout;
			this.Catalyst = catalyst;
		}

		public abstract bool Matches(Inventory inventory);
	}
}
