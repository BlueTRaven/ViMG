using BrUtility;
using System;
using System.Collections.Generic;
using System.Text;

namespace ViMG.Recipes
{
	public interface IRecipeCatalyst
	{
		void RegisterRecipes(List<Recipe> recipes);

		void DoRecipeUI(out Size size, Recipe recipe, float textureSize, float textureScale);

		//void DoCatalystUI();
	}
}
