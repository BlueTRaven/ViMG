using BrUtility;
using Engine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG.GameStates;
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
		private bool includeInputs;
		private bool includeOutputs;

		private List<IRecipeCatalyst> currentCatalysts;
		private List<Recipe> currentRecipes;

		private TextHelper.FontInfo fi;

		public MenuRecipeBook(GameStateManager gsManager, IRecipeCatalyst catalyst, ItemInstance filterItem, bool includeInputs = false, bool includeOutputs = false) : base(gsManager)
		{
			this.filterCatalyst = catalyst;
			this.filterItem = filterItem;
			this.includeInputs = includeInputs;
			this.includeOutputs = includeOutputs;

			currentCatalysts = GetFilteredCatalysts(filterItem, includeInputs, includeOutputs);
			if (filterCatalyst == null && currentCatalysts.Count > 0)
				this.filterCatalyst = currentCatalysts[0];
			GetFilteredRecipes();

			fi = new TextHelper.FontInfo(GlobalState.assetsManager.GetAsset<SpriteFont>("fira_mono_sml"), 1, true);
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

        public override void Update(double deltaTime)
		{
			base.Update(deltaTime);

            if (Main.gameStateManager.GetCurrentGameState().GetCurrentMenu() == this && (Main.inputManager.JustPressed(Microsoft.Xna.Framework.Input.Keys.E) || Main.inputManager.JustPressed(Microsoft.Xna.Framework.Input.Keys.Escape)))
                Main.gameStateManager.GetCurrentGameState().PopMenu();

            //TODO: size should be determined statically for each catalyst rather than asking to do a UI.
            Size eachSize = Size.Zero;
			if (currentRecipes != null && currentRecipes.Count > 0)
				eachSize = filterCatalyst.GetSize();
			
			UI.Start();
			UI.StartParent(new Vector2(MARGIN));

			eachSize.Height += MARGIN_CRAFTING;
			eachSize.Width += MARGIN_CRAFTING;

			int numPerPageW = (int)((float)MAX_PAGE_WIDTH / eachSize.Width);
			int numPerPageH = (int)((float)MAX_PAGE_HEIGHT / eachSize.Height);
			int numPerPage = numPerPageW * numPerPageH;

			if (currentRecipes != null && currentRecipes.Count > 0)
				DoRecipes( numPerPageW, numPerPageH, eachSize);

			UI.MakePanel(new Color(139, 139, 139), new RectangleF(0, 0, MAX_PAGE_WIDTH, PAGE_HEADER));

			UI.MakeLabel(filterCatalyst.GetName(), fi, MAX_PAGE_WIDTH, Vector2.Zero);

			UI.MakePanel(new Color(139, 139, 139), new RectangleF(0, PAGE_HEADER, MAX_PAGE_WIDTH, MAX_PAGE_HEIGHT));

			UI.StartParent(new Vector2(MAX_PAGE_WIDTH, PAGE_HEADER));

			RectangleF bounds = new RectangleF(0, 0, SIZE, SIZE);

			foreach (IRecipeCatalyst catalyst in currentCatalysts)
			{
				var button = UI.MakeButton(new UI.ButtonConstructionParameters(bounds, GlobalState.assetsManager.GetAsset<Texture2D>("ui_inventory"),
							new RectangleF(0, 0, 16, 16), new RectangleF(16, 0, 16, 16), new RectangleF(16, 0, 16, 16)));

				UI.MakeTexture(bounds, catalyst.GetTexture(), catalyst.GetSourceRect());

				if (button.clickLeft)
				{
					page = 0;
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
					if (UI.MakeButton(new UI.ButtonConstructionParameters(new RectangleF(0, MAX_PAGE_HEIGHT - SIZE / 2, SIZE / 2, SIZE / 2), GlobalState.assetsManager.GetAsset<Texture2D>("ui_inventory"),
						new RectangleF(0, 112, 8, 8), new RectangleF(8, 112, 8, 8), new RectangleF(8, 112, 8, 8))).clickLeft)
					{
						page--;
					}
				}

				if (page * numPerPage + numPerPage < currentRecipes.Count)
				{
					if (UI.MakeButton(new UI.ButtonConstructionParameters(new RectangleF(SIZE * 8 - SIZE / 2, MAX_PAGE_HEIGHT - SIZE / 2, SIZE / 2, SIZE / 2), GlobalState.assetsManager.GetAsset<Texture2D>("ui_inventory"),
						new RectangleF(0, 120, 8, 8), new RectangleF(8, 120, 8, 8), new RectangleF(8, 120, 8, 8))).clickLeft)
					{
						page++;
					}
				}
			}

			UI.EndParent();
		}
		
		//we don't actually use this, however DoRecipeUI2 relies on this array so it must be used.
		//# item slots can vary wildly between different recipe catalysts, so just choose a large ish number.
		private UI.ItemSlot[] dummyItemSlots = new UI.ItemSlot[32];
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
						filterCatalyst.DoRecipeUI2(dummyItemSlots, recipe);

						for (int j = 0; j < 32; j++)
						{
							if (dummyItemSlots[j].item.valid)
								MenuHelper.HandleRecipeFilter(gsManager, dummyItemSlots[j].item, dummyItemSlots[j].button);
						}
						UI.EndParent();
					}
				}
			}

			UI.EndParent();
		}

		public static bool HasAnyFilteredCatalysts(ItemInstance itemInstance, bool includeInputs, bool includeOutputs)
        {
			if (!itemInstance.valid)
				return false;

			foreach (var catalyst in GlobalState.Registry.RecipeRegistry.GetCatalysts())
			{
				foreach (Recipe recipe in GlobalState.Registry.RecipeRegistry.GetRecipesByCatalyst(catalyst))
				{
					if (HasFilteredItem(recipe, itemInstance, includeInputs, includeOutputs))
					{
						return true;
					}
				}
			}

			return false;
		}

		public static List<IRecipeCatalyst> GetFilteredCatalysts(ItemInstance filterItem, bool includeInputs, bool includeOutputs)
		{
			if (!filterItem.valid)
			{
				return new List<IRecipeCatalyst>(GlobalState.Registry.RecipeRegistry.GetCatalysts());
			}

			List<IRecipeCatalyst> catalysts = new List<IRecipeCatalyst>();

			foreach (var catalyst in GlobalState.Registry.RecipeRegistry.GetCatalysts())
			{
				foreach (Recipe recipe in GlobalState.Registry.RecipeRegistry.GetRecipesByCatalyst(catalyst))
				{
					if (HasFilteredItem(recipe, filterItem, includeInputs, includeOutputs))
					{
						catalysts.Add(catalyst);
						break;
					}
				}
			}

			return catalysts;
		}

		private void GetFilteredRecipes()
		{
			if (filterCatalyst != null)
			{
				currentRecipes = new List<Recipe>();

				var catalystRecipes = GlobalState.Registry.RecipeRegistry.GetRecipesByCatalyst(filterCatalyst);

				if (catalystRecipes != null)
				{
					foreach (Recipe recipe in catalystRecipes)
					{
						if (HasFilteredItem(recipe, filterItem, includeInputs, includeOutputs))
							currentRecipes.Add(recipe);
					}
				}
			}
		}

		private static bool HasFilteredItem(Recipe recipe, ItemInstance filterItem, bool includeInputs, bool includeOutputs)
		{
			if (!filterItem.valid)
				return true;
			else
			{
				for (int i = 0; i < Math.Max(recipe.Layout.Length, recipe.Outputs.Length); i++)
				{
					if (includeInputs && i < recipe.Layout.Length)
						if (recipe.Layout[i].item == filterItem.item && recipe.Layout[i].damage == filterItem.damage)
							return true;

					if (includeOutputs && i < recipe.Outputs.Length)
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
