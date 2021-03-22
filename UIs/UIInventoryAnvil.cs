using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Cubes;
using ViMG.Items;
using ViMG.Recipes;

namespace ViMG.UIs
{
	public class UIInventoryAnvil : UIInventory
	{
		private readonly Player player;
		private Inventory playerInventory;
		private Inventory anvilInventory;
		private bool inventoryUpdated;
		private Recipe currentRecipe;

		private Items.ItemInstance held;

		public UIInventoryAnvil(Player player, Inventory playerInventory, Inventory anvilInventory)
		{
			this.player = player;
			this.playerInventory = playerInventory;
			this.anvilInventory = anvilInventory;
		}

		public override void Update()
		{
			base.Update();

			TextHelper.FontInfo fi = new TextHelper.FontInfo(Main.assetsManager.GetAsset<SpriteFont>("fira_mono_sml"), 1, true);

			UI.Start();

			UI.StartParent(new Vector2(MARGIN, MARGIN + 32));

			UIInventoryHelper.DoPlayerInventory(player, playerInventory, ref held, Player.INVENTORY_ROWS, Player.INVENTORY_COLUMNS, SIZE, PADDING);

			UI.EndParent();

			UI.StartParent(new Vector2(MARGIN + Player.INVENTORY_COLUMNS * SIZE + Player.INVENTORY_COLUMNS * PADDING + MARGIN_CRAFTING, MARGIN + SIZE));

			UI.MakePanel(new Color(139, 139, 139), new RectangleF(0, 0, SIZE * 3f, SIZE * 4 + MARGIN * 2));

			UI.StartParent(new Vector2(MARGIN));

			Vector2 pos = Vector2.Zero;
			RectangleF bounds = new RectangleF(pos, SIZE, SIZE);

			var itemSlotA = UI.MakeItemSlot(UI.MakeButton(bounds, Main.assetsManager.GetAsset<Texture2D>("ui_inventory"),
								new RectangleF(0, 0, 16, 16), new RectangleF(16, 0, 16, 16), new RectangleF(16, 0, 16, 16)),
								anvilInventory.Get(0));

			bounds.x += SIZE;

			var itemSlotB = UI.MakeItemSlot(UI.MakeButton(bounds, Main.assetsManager.GetAsset<Texture2D>("ui_inventory"),
								new RectangleF(0, 0, 16, 16), new RectangleF(16, 0, 16, 16), new RectangleF(16, 0, 16, 16)),
								anvilInventory.Get(1));

			bounds.x -= SIZE;
			bounds.y += SIZE;

			var itemSlotC = UI.MakeItemSlot(UI.MakeButton(bounds, Main.assetsManager.GetAsset<Texture2D>("ui_inventory"),
								new RectangleF(0, 0, 16, 16), new RectangleF(16, 0, 16, 16), new RectangleF(16, 0, 16, 16)),
								anvilInventory.Get(2));

			bounds.y -= SIZE;

			UIInventoryHelper.ItemSlotClickOutput output = UIInventoryHelper.ItemSlotClickOutput.None;
			if ((output = UIInventoryHelper.HandleItemSlot(player, anvilInventory, 0, itemSlotA, ref held, new UIInventoryHelper.WhiteListNone())) != UIInventoryHelper.ItemSlotClickOutput.None) ;
			{
				if (output == UIInventoryHelper.ItemSlotClickOutput.NeedsSwapInventory)
					UIInventoryHelper.SwapInventory(anvilInventory, playerInventory, 0);
				inventoryUpdated = true;
			}

			if ((output = UIInventoryHelper.HandleItemSlot(player, anvilInventory, 1, itemSlotB, ref held, new UIInventoryHelper.WhiteListNone())) != UIInventoryHelper.ItemSlotClickOutput.None)
			{
				if (output == UIInventoryHelper.ItemSlotClickOutput.NeedsSwapInventory)
					UIInventoryHelper.SwapInventory(anvilInventory, playerInventory, 1);

				inventoryUpdated = true;
			}

			if ((output = UIInventoryHelper.HandleItemSlot(player, anvilInventory, 2, itemSlotC, ref held, new UIInventoryHelper.WhiteListNone())) != UIInventoryHelper.ItemSlotClickOutput.None)
			{
				if (output == UIInventoryHelper.ItemSlotClickOutput.NeedsSwapInventory)
					UIInventoryHelper.SwapInventory(anvilInventory, playerInventory, 2);

				inventoryUpdated = true;
			}

			if (inventoryUpdated)
			{
				currentRecipe = FindRecipe(anvilInventory);

				if (currentRecipe != null)
				{
					for (int i = 0; i < Math.Min(1, currentRecipe.Outputs.Length); i++)
					{
						anvilInventory.Set(currentRecipe.Outputs[i], 3 + i);
					}
				}
				else
				{
					anvilInventory.Set(new ItemInstance(), 3);
				}
			}

			bounds.y += SIZE * 2;

			UI.MakeTexture(bounds, Main.assetsManager.GetAsset<Texture2D>("ui_inventory"), new RectangleF(32, 32, 16, 16));

			bounds.y += SIZE;

			UI.MakeItemSlot(UI.MakeButton(bounds, Main.assetsManager.GetAsset<Texture2D>("ui_inventory"),
								new RectangleF(0, 0, 16, 16), new RectangleF(16, 0, 16, 16), new RectangleF(16, 0, 16, 16)),
								anvilInventory.Get(3));

			bounds.x += SIZE;

			UI.Button craftRecipeButton = UI.MakeButton(bounds, Main.assetsManager.GetAsset<Texture2D>("ui_inventory"), new RectangleF(0, 96, 16, 16), new RectangleF(16, 96, 16, 16), new RectangleF(16, 96, 16, 16));
			if (craftRecipeButton.clickLeft)
			{
				if (currentRecipe != null)
					CraftItem(currentRecipe);
			}
			else if (craftRecipeButton.hovered)
			{
				UI.DisableParent();
				UI.MakeLabel("Craft Recipe", fi, 256, Main.inputManager.GetMousePosition().ToVector2() + new Vector2(16));
				UI.EnableParent();
			}

			bounds.x -= SIZE;
			bounds.y += SIZE * 2f;

			UI.Button recipeBookButton = UI.MakeButton(bounds, Main.assetsManager.GetAsset<Texture2D>("ui_inventory"),
				new RectangleF(0, 80, 16, 16), new RectangleF(16, 80, 16, 16), new RectangleF(16, 80, 16, 16));

			if (recipeBookButton.hovered)
			{
				UI.DisableParent();
				UI.MakeLabel("Recipe Book", fi, 128, Main.inputManager.GetMousePosition().ToVector2() + new Vector2(16));
				UI.EnableParent();
			}

			if (recipeBookButton.clickLeft)
			{
				player.OpenUI(new UIRecipeBook(Main.Registry.CubeRegistry.Get("anvil_iron") as CubeAnvilIron, new ItemInstance()));
			}

			UI.EndParent();
			UI.EndParent();
		}

		private Recipe FindRecipe(Inventory inventory)
		{
			var recipes = Main.Registry.RecipeRegistry.GetRecipesByCatalyst(Main.Registry.CubeRegistry.Get("anvil_iron") as CubeAnvilIron);

			Recipe foundRecipe = null;

			for (int i = 0; i < recipes.Count; i++)
			{
				Recipe recipe = recipes[i];

				if (recipe.Matches(inventory))
				{
					if (foundRecipe == null || recipe.Weight > foundRecipe.Weight)
						foundRecipe = recipe;
				}
			}

			return foundRecipe;
		}

		private void CraftItem(Recipe recipe)
		{
			if (recipe.Matches(anvilInventory))
			{
				for (int i = 0; i < recipe.Layout.Length; i++)
				{
					if (recipe.Layout[i].valid)
					{
						int numLeft = recipe.Layout[i].num;

						anvilInventory.Find(recipe.Layout[i], 3, out int index);

						int overflow = anvilInventory.Get(i).num - numLeft;
						anvilInventory.Remove(index, numLeft);
						inventoryUpdated  = true;

						if (overflow < 0)
							numLeft -= Math.Abs(overflow);
						else numLeft -= numLeft;
					}
				}

				for (int i = 0; i < recipe.Outputs.Length; i++)
				{
					playerInventory.Add(recipe.Outputs[i]);
				}
			}
		}

		public override void Draw(SpriteBatch batch)
		{
			base.Draw(batch);

			UI.Draw(batch, SCALE);

			UIInventoryHelper.DrawHeldItem(batch, held, SIZE, SCALE);
		}
	}
}
