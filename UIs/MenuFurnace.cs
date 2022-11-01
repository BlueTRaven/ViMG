using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Cubes;
using ViMG.Entities;
using ViMG.GameStates;
using ViMG.Items;
using ViMG.Recipes;

namespace ViMG.UIs
{
	public class MenuFurnace : Menu
	{
		private Player player;
		private Inventory playerInventory;
		private Inventory furnaceInventory;
		private bool furnaceInventoryUpdated;
		private Recipe currentRecipe;

		private EntityFurnace furnace;

		private Items.ItemInstance held;

		public MenuFurnace(GameStateManager gsManager, Player player, Inventory playerInventory, Inventory furnaceInventory, EntityFurnace furnace) : base(gsManager)
		{
			this.player = player;
			this.playerInventory = playerInventory;
			this.furnaceInventory = furnaceInventory;

			this.furnace = furnace;
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

			TextHelper.FontInfo fi = new TextHelper.FontInfo(Main.assetsManager.GetAsset<SpriteFont>("fira_mono_sml"), 1, true);

			UI.Start();

			UI.StartParent(new Vector2(MARGIN, MARGIN + 32));

			MenuHelper.DoPlayerInventory(player, playerInventory, ref held, Player.INVENTORY_ROWS, Player.INVENTORY_COLUMNS, SIZE, PADDING);

			UI.EndParent();

			UI.StartParent(new Vector2(MARGIN + Player.INVENTORY_COLUMNS * SIZE + Player.INVENTORY_COLUMNS * PADDING + MARGIN_CRAFTING, MARGIN + SIZE));

			UI.MakePanel(new Color(139, 139, 139), new RectangleF(0, 0, SIZE * 4.5f, SIZE * 3 + MARGIN * 2));

			UI.StartParent(new Vector2(MARGIN));

			Vector2 pos = Vector2.Zero;
			RectangleF bounds = new RectangleF(pos, SIZE, SIZE);

			var itemSlotA = UI.MakeItemSlot(UI.MakeButton(bounds, Main.assetsManager.GetAsset<Texture2D>("ui_inventory"),
								new RectangleF(0, 0, 16, 16), new RectangleF(16, 0, 16, 16), new RectangleF(16, 0, 16, 16)),
								furnaceInventory.Get(0));

			bounds.x += SIZE;

			var itemSlotB = UI.MakeItemSlot(UI.MakeButton(bounds, Main.assetsManager.GetAsset<Texture2D>("ui_inventory"),
								new RectangleF(0, 0, 16, 16), new RectangleF(16, 0, 16, 16), new RectangleF(16, 0, 16, 16)),
								furnaceInventory.Get(1));

			bounds.x += SIZE + MARGIN;

			var itemSlotFuel = UI.MakeItemSlot(UI.MakeButton(bounds, Main.assetsManager.GetAsset<Texture2D>("ui_inventory"),
								new RectangleF(0, 0, 16, 16), new RectangleF(16, 0, 16, 16), new RectangleF(16, 0, 16, 16)),
								furnaceInventory.Get(2));

			bounds.x -= SIZE + MARGIN;

			MenuHelper.ItemSlotClickOutput output = MenuHelper.ItemSlotClickOutput.None;
			if ((output = MenuHelper.HandleItemSlot(player, furnaceInventory, 0, itemSlotA, ref held, new MenuHelper.WhiteListNone())) != MenuHelper.ItemSlotClickOutput.None);
			{
				if (output == MenuHelper.ItemSlotClickOutput.NeedsSwapInventory)
					MenuHelper.SwapInventory(furnaceInventory, playerInventory, 0);

				furnaceInventoryUpdated = true;
			}

			if ((output = MenuHelper.HandleItemSlot(player, furnaceInventory, 1, itemSlotB, ref held, new MenuHelper.WhiteListNone())) != MenuHelper.ItemSlotClickOutput.None)
			{
				if (output == MenuHelper.ItemSlotClickOutput.NeedsSwapInventory)
					MenuHelper.SwapInventory(furnaceInventory, playerInventory, 1);

				furnaceInventoryUpdated = true;
			}

			if ((output = MenuHelper.HandleItemSlot(player, furnaceInventory, 2, itemSlotFuel, ref held, new MenuHelper.WhiteListOneName("glowdust"))) != MenuHelper.ItemSlotClickOutput.None)
			{
				if (output == MenuHelper.ItemSlotClickOutput.NeedsSwapInventory)
					MenuHelper.SwapInventory(furnaceInventory, playerInventory, 2);

				furnaceInventoryUpdated = true;
			}

			if (furnaceInventoryUpdated)
			{
				currentRecipe = FindRecipe(furnaceInventory);

				if (currentRecipe != null)
				{
					for (int i = 0; i < Math.Min(2, currentRecipe.Outputs.Length); i++)
					{
						furnaceInventory.Set(currentRecipe.Outputs[i], 3 + i);
					}
				}
				else
				{
					furnaceInventory.Set(new ItemInstance(), 3);
					furnaceInventory.Set(new ItemInstance(), 4);
				}
			}

			bounds.x -= SIZE;
			bounds.y += SIZE;
			UI.MakeTexture(bounds, Main.assetsManager.GetAsset<Texture2D>("ui_inventory"), new RectangleF(32, 32, 16, 16));
			bounds.y += SIZE;

			UI.MakeItemSlot(UI.MakeButton(bounds, Main.assetsManager.GetAsset<Texture2D>("ui_inventory"),
								new RectangleF(0, 0, 16, 16), new RectangleF(16, 0, 16, 16), new RectangleF(16, 0, 16, 16)),
								furnaceInventory.Get(3));

			bounds.x += SIZE;

			UI.MakeItemSlot(UI.MakeButton(bounds, Main.assetsManager.GetAsset<Texture2D>("ui_inventory"),
								new RectangleF(0, 0, 16, 16), new RectangleF(16, 0, 16, 16), new RectangleF(16, 0, 16, 16)),
								furnaceInventory.Get(4));

			bounds.x += SIZE;

			UI.Button craftRecipeButton = UI.MakeButton(bounds, Main.assetsManager.GetAsset<Texture2D>("ui_inventory"), new RectangleF(0, 96, 16, 16), new RectangleF(16, 96, 16, 16), new RectangleF(16, 96, 16, 16));
			if (craftRecipeButton.clickLeft)
			{
				if (itemSlotFuel.item.num > 0)
				{
					if (currentRecipe != null)
						CraftItem(currentRecipe);
				}
			}
			else if (craftRecipeButton.hovered)
			{
				UI.DisableParent();
				UI.MakeLabel("Craft Recipe", fi, 256, Main.inputManager.GetMousePosition().ToVector2() + new Vector2(16));
				UI.EnableParent();
			}

			UI.EndParent();

			bounds.x = MARGIN;
			bounds.y += SIZE * 2 + MARGIN;

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
				player.world.GameStateManager.GetCurrentGameState().PushMenu(new MenuRecipeBook(gsManager, Main.Registry.CubeRegistry.Get("furnace_t1") as CubeFurnace, new ItemInstance()));
			}

			UI.EndParent();
		}

		private Recipe FindRecipe(Inventory inventory)
		{
			var recipes = Main.Registry.RecipeRegistry.GetRecipesByCatalyst(Main.Registry.CubeRegistry.Get("furnace_t1") as CubeFurnace);

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
			if (recipe.Matches(furnaceInventory))
			{
				for (int i = 0; i < recipe.Layout.Length; i++)
				{
					if (recipe.Layout[i].valid)
					{
						int numLeft = recipe.Layout[i].num;

						furnaceInventory.FindExact(recipe.Layout[i], 2, out int index);

						int overflow = furnaceInventory.Get(i).num - numLeft;
						furnaceInventory.Remove(index, numLeft);
						furnaceInventoryUpdated = true;

						furnace.OnCraft();

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

			// remove fuel
			furnaceInventory.Remove(2, 1);
		}

		public override void Draw(SpriteBatch batch)
		{
			base.Draw(batch);

			UI.Draw(batch, SCALE);

			MenuHelper.DrawHeldItem(batch, held, SIZE, SCALE);
		}
	}
}
