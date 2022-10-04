using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Items;
using ViMG.Recipes;

namespace ViMG.UIs
{
	public class MenuRecipeBook : Menu
	{
		private IRecipeCatalyst filterCatalyst;
		private int page;
		private const float PAGE_HEADER = 16;
		private const float MAX_PAGE_HEIGHT = 320;
		private const float MAX_PAGE_WIDTH = SIZE * 8;

		private ItemInstance filterItem;

		private List<IRecipeCatalyst> currentCatalysts;
		private List<Recipe> currentRecipes;

		private TextHelper.FontInfo fi;

		public MenuRecipeBook(IRecipeCatalyst catalyst, ItemInstance filterItem)
		{
			this.filterCatalyst = catalyst;
			this.filterItem = filterItem;

			GetFilteredCatalysts();
			if (filterCatalyst == null && currentCatalysts.Count > 0)
				this.filterCatalyst = currentCatalysts[0];
			GetFilteredRecipes();

			fi = new TextHelper.FontInfo(Main.assetsManager.GetAsset<SpriteFont>("fira_mono_sml"), 1, true);
		}

		public override void OnOpen()
        {
            base.OnOpen();

			Main.DrawCursor = true;
			Main.MouseControl = true;
        }

        public override void OnClose()
        {
            base.OnClose();

			Main.DrawCursor = false;
			Main.MouseControl = false;
		}

        public override void Update(GraphicsDevice device, double deltaTime)
		{
			base.Update(device, deltaTime);

			//TODO: size should be determined statically for each catalyst rather than asking to do a UI.
			Size eachSize = Size.Zero;
			if (currentRecipes != null && currentRecipes.Count > 0)
				filterCatalyst.DoRecipeUI(out eachSize, currentRecipes[0], SIZE, SCALE);
			
			UI.Start();
			UI.StartParent(new Vector2(MARGIN));

			eachSize.Height += MARGIN_CRAFTING;
			eachSize.Width += MARGIN_CRAFTING;

			int numPerPageW = (int)((float)MAX_PAGE_WIDTH / eachSize.Width);
			int numPerPageH = (int)((float)MAX_PAGE_HEIGHT / eachSize.Height);
			int numPerPage = numPerPageW * numPerPageH;

			if (currentRecipes != null && currentRecipes.Count > 0)
				DoRecipes(numPerPageW, numPerPageH, eachSize);

			UI.MakePanel(new Color(139, 139, 139), new RectangleF(0, 0, MAX_PAGE_WIDTH, PAGE_HEADER));

			UI.MakeLabel(filterCatalyst.GetName(), fi, MAX_PAGE_WIDTH, Vector2.Zero);

			UI.MakePanel(new Color(139, 139, 139), new RectangleF(0, PAGE_HEADER, MAX_PAGE_WIDTH, MAX_PAGE_HEIGHT));

			UI.StartParent(new Vector2(MAX_PAGE_WIDTH, PAGE_HEADER));

			RectangleF bounds = new RectangleF(0, 0, SIZE, SIZE);

			foreach (IRecipeCatalyst catalyst in currentCatalysts)
			{
				var button = UI.MakeButton(bounds, Main.assetsManager.GetAsset<Texture2D>("ui_inventory"),
							new RectangleF(0, 0, 16, 16), new RectangleF(16, 0, 16, 16), new RectangleF(16, 0, 16, 16));

				UI.MakeTexture(bounds, catalyst.GetTexture(), catalyst.GetSourceRect());

				if (button.clickLeft)
				{
					filterCatalyst = catalyst;
					GetFilteredRecipes();
				}

				bounds.y += SIZE;
			}

			UI.EndParent();

			if (currentRecipes != null && currentRecipes.Count > 0)
			{
				if (page > 0)
				{
					if (UI.MakeButton(new RectangleF(0, MAX_PAGE_HEIGHT - SIZE / 2, SIZE / 2, SIZE / 2), Main.assetsManager.GetAsset<Texture2D>("ui_inventory"),
						new RectangleF(0, 112, 8, 8), new RectangleF(8, 112, 8, 8), new RectangleF(8, 112, 8, 8)).clickLeft)
					{
						page--;
					}
				}

				if (page * numPerPage + numPerPage < currentRecipes.Count)
				{
					if (UI.MakeButton(new RectangleF(SIZE * 8 - SIZE / 2, MAX_PAGE_HEIGHT - SIZE / 2, SIZE / 2, SIZE / 2), Main.assetsManager.GetAsset<Texture2D>("ui_inventory"),
						new RectangleF(0, 120, 8, 8), new RectangleF(8, 120, 8, 8), new RectangleF(8, 120, 8, 8)).clickLeft)
					{
						page++;
					}
				}
			}

			UI.EndParent();
		}

		private void DoRecipes(int numPerWidth, int numPerHeight, Size eachSize)
		{
			UI.StartParent(new Vector2(MARGIN));

			for (int x = 0; x < numPerWidth; x++)
			{
				for (int y = 0; y < numPerHeight; y++)
				{
					int i = y * numPerWidth + x;
					i += page * (numPerHeight * numPerWidth);

					if (i < currentRecipes.Count)
					{
						Recipe recipe = currentRecipes[i];

						UI.StartParent(eachSize.ToVector2() * new Vector2(x, y));
						Size size = new Size();
						filterCatalyst.DoRecipeUI(out size, recipe, SIZE, SCALE);
						UI.EndParent();
					}
				}
			}

			UI.EndParent();
		}

		private void GetFilteredCatalysts()
		{
			if (!filterItem.valid)
			{
				currentCatalysts = new List<IRecipeCatalyst>(Main.Registry.RecipeRegistry.GetCatalysts());
				return;
			}

			currentCatalysts = new List<IRecipeCatalyst>();

			foreach (var catalyst in Main.Registry.RecipeRegistry.GetCatalysts())
			{
				foreach (Recipe recipe in Main.Registry.RecipeRegistry.GetRecipesByCatalyst(catalyst))
				{
					if (HasFilteredItem(recipe, true, true))
					{
						currentCatalysts.Add(catalyst);
						break;
					}
				}
			}
		}

		private void GetFilteredRecipes()
		{
			if (filterCatalyst != null)
			{
				currentRecipes = new List<Recipe>();

				var catalystRecipes = Main.Registry.RecipeRegistry.GetRecipesByCatalyst(filterCatalyst);

				foreach (Recipe recipe in catalystRecipes)
				{
					if (HasFilteredItem(recipe, true, true))
						currentRecipes.Add(recipe);
				}
			}
		}

		private bool HasFilteredItem(Recipe recipe, bool includeInput, bool includeOutput)
		{
			if (!filterItem.valid)
				return true;
			else
			{
				for (int i = 0; i < Math.Max(recipe.Layout.Length, recipe.Outputs.Length); i++)
				{
					if (includeInput && i < recipe.Layout.Length)
						if (recipe.Layout[i].item == filterItem.item && recipe.Layout[i].damage == filterItem.damage)
							return true;

					if (includeOutput && i < recipe.Outputs.Length)
						if (recipe.Outputs[i].item == filterItem.item && recipe.Outputs[i].damage == filterItem.damage)
							return true;
				}
			}

			return false;
		}

		public override void Draw(SpriteBatch batch)
		{
			base.Draw(batch);

			UI.Draw(batch, SCALE);
		}
	}
}
