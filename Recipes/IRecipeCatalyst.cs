using BrUtility;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG.UIs;

namespace ViMG.Recipes
{
	public interface IRecipeCatalyst
	{
		string GetName();

		Texture2D GetTexture();
		RectangleF GetSourceRect();

		void RegisterRecipes(List<Recipe> recipes);

		Size GetSize();

		void DoRecipeUI2(UI.ItemSlot[] itemSlots, Recipe recipe);

		void DoRecipeUI(out Size size, Recipe recipe, float textureSize, float textureScale);
	}
}
