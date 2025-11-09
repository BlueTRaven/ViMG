using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Cubes;
using ViMG.Items;

namespace ViMG.Recipes
{
	public abstract class Recipe : IRegisterable
	{
        private string identifier;
        public string Identifier => identifier;

        public readonly IRecipeCatalyst Catalyst;
		public readonly ItemInstance[] Layout;
		public readonly ItemInstance[] Outputs;

		public readonly int Weight;

		public Recipe(string identifier, IRecipeCatalyst catalyst, ItemInstance[] layout, ItemInstance[] outputs, int weight = 1)
		{
			this.identifier = identifier;

			this.Layout = layout;
			this.Catalyst = catalyst;
			this.Outputs = outputs;
			this.Weight = weight;
		}

        public abstract bool Matches(Inventory inventory);
	}
}
