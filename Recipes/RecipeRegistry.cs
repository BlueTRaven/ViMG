using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Items;

namespace ViMG.Recipes
{
	public class RecipeRegistry
	{
		private List<Recipe> registry = new List<Recipe>();
		public int Count => registry.Count;

		public void RegisterAll()
		{
			registry.Add(new RecipeFuzzy(null,
				new ItemInstance[] { new ItemInstance(Main.Registry.ItemRegistry.Get("flask_empty"), 1, 1), new ItemInstance(Main.Registry.ItemRegistry.Get("slime_chunk"), 2, 1) },
				new ItemInstance[] { new ItemInstance(Main.Registry.ItemRegistry.Get("flask_healthpotion1"), 1, 1) }));

			registry.Add(new RecipeFuzzy(null,
				new ItemInstance[] { new ItemInstance(Main.Registry.ItemRegistry.Get("wood"), 1, 1), new ItemInstance(Main.Registry.ItemRegistry.Get("glowdust"), 4, 1) },
				new ItemInstance[] { new ItemInstance(Main.Registry.ItemRegistry.Get("glow_node"), 1, 1) }));
		}

		public IReadOnlyList<Recipe> GetRecipes()
		{
			return registry;
		}
	}
}
