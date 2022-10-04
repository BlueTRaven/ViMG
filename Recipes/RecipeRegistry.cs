using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Cubes;
using ViMG.Items;

namespace ViMG.Recipes
{
	public class RecipeRegistry
	{
		public CatalystPlayerInventory PlayerInventoryCatalyst;

		private List<Recipe> registry = new List<Recipe>();
		private Dictionary<IRecipeCatalyst, List<Recipe>> recipesByCatalyst = new Dictionary<IRecipeCatalyst, List<Recipe>>();
		public int Count => registry.Count;

		private List<IRecipeCatalyst> catalysts = new List<IRecipeCatalyst>();

		public void RegisterAll()
		{
			PlayerInventoryCatalyst = new CatalystPlayerInventory();
			RegisterCatalyst(PlayerInventoryCatalyst);

			RegisterCatalyst(new CatalystAnvilIronTools());
			RegisterCatalyst(new CatalystAnvilIronArmor());

			foreach (IRecipeCatalyst catalyst in catalysts)
			{
				List<Recipe> recipesMadeByCatalyst = new List<Recipe>();
				catalyst.RegisterRecipes(recipesMadeByCatalyst);

				foreach (Recipe catalystRecipe in recipesMadeByCatalyst)
				{
					Register(catalystRecipe);
				}
			}
		}

		public void RegisterCatalyst(IRecipeCatalyst catalyst)
		{
			catalysts.Add(catalyst);
		}

		private void Register(Recipe recipe)
		{
			if (!recipesByCatalyst.ContainsKey(recipe.Catalyst))
				recipesByCatalyst.Add(recipe.Catalyst, new List<Recipe>());

			recipesByCatalyst[recipe.Catalyst].Add(recipe);

			registry.Add(recipe);
		}

		public IReadOnlyList<Recipe> GetRecipesByCatalyst(IRecipeCatalyst catalyst)
		{
			if (recipesByCatalyst.ContainsKey(catalyst))
				return recipesByCatalyst[catalyst];
			else return null;
		}

		public IReadOnlyList<IRecipeCatalyst> GetCatalysts()
		{
			return catalysts;
		}

		public IReadOnlyList<Recipe> GetRecipes()
		{
			return registry;
		}
	}
}
