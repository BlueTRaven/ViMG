using BrUtility;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace ViMG.Recipes
{
	public interface IRecipeCatalyst
	{
		string GetName();

		Texture2D GetTexture();
		RectangleF GetSourceRect();

		void RegisterRecipes(List<Recipe> recipes);

		void DoRecipeUI(out Size size, Recipe recipe, float textureSize, float textureScale);
	}
}
