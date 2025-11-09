using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using ViMG.Cubes;
using ViMG.Items;

namespace ViMG.Recipes
{
	public class RecipeRegistry : ObjRegistry<Recipe>
	{
		private Dictionary<IRecipeCatalyst, List<Recipe>> recipesByCatalyst = new Dictionary<IRecipeCatalyst, List<Recipe>>();
		public Dictionary<string, IRecipeCatalyst> catalystByName = new Dictionary<string, IRecipeCatalyst>();

		private List<IRecipeCatalyst> catalysts = new List<IRecipeCatalyst>();

        public override void PostRegistration()
        {
            base.PostRegistration();

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

		public override void AddFromOther(ObjRegistry<Recipe>? other)
		{
			if (other != null)
			{
				base.AddFromOther(other);

				RecipeRegistry registry = other as RecipeRegistry;

				foreach (var catalyst in registry.GetCatalysts())
				{
					catalysts.Add(catalyst);
					catalystByName.Add(catalyst.GetName(), catalyst);
				}
			}
        }

		public void RegisterCatalyst(IRecipeCatalyst catalyst)
		{
			catalysts.Add(catalyst);
			catalystByName.Add(catalyst.GetName(), catalyst);
		}

		public override void Register(Recipe recipe)
		{
			if (!recipesByCatalyst.ContainsKey(recipe.Catalyst))
				recipesByCatalyst.Add(recipe.Catalyst, new List<Recipe>());

			recipesByCatalyst[recipe.Catalyst].Add(recipe);

			base.Register(recipe);
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
	}
}
