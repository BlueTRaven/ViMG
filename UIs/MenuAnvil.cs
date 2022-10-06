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
	public class MenuAnvil : Menu
	{
		private readonly Player player;
		private readonly Inventory playerInventory;
		private readonly Inventory anvilInventoryTools;
        private readonly Inventory anvilInventoryArmor;
        private bool inventoryUpdated;
		private Recipe currentRecipe;

		private Items.ItemInstance held;

		private TextHelper.FontInfo fi;

		private static Vector2 inventoryRight = new Vector2(MARGIN + Player.INVENTORY_COLUMNS * SIZE + Player.INVENTORY_COLUMNS * PADDING + MARGIN_CRAFTING, MARGIN + SIZE);

		public MenuAnvil(Player player, Inventory playerInventory, Inventory anvilInventoryTools, Inventory anvilInventoryArmor)
		{
			this.player = player;
			this.playerInventory = playerInventory;
			this.anvilInventoryTools = anvilInventoryTools;
            this.anvilInventoryArmor = anvilInventoryArmor;

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


			UI.Start();

			UI.StartParent(new Vector2(MARGIN, MARGIN + 32));

			MenuHelper.DoPlayerInventory(player, playerInventory, ref held, Player.INVENTORY_ROWS, Player.INVENTORY_COLUMNS, SIZE, PADDING);

			UI.EndParent();

			DoTools();
		}

		private void DoTools()
        {
			UI.StartParent(inventoryRight);

			UI.MakePanel(new Color(139, 139, 139), new RectangleF(0, 0, SIZE * 3f, SIZE * 4 + MARGIN * 2));

			UI.StartParent(new Vector2(MARGIN));

			Vector2 pos = Vector2.Zero;
			RectangleF bounds = new RectangleF(pos, SIZE, SIZE);

			var itemSlotA = UI.MakeItemSlot(UI.MakeButton(bounds, Main.assetsManager.GetAsset<Texture2D>("ui_inventory"),
								new RectangleF(0, 0, 16, 16), new RectangleF(16, 0, 16, 16), new RectangleF(16, 0, 16, 16)),
								anvilInventoryTools.Get(0));

			bounds.x += SIZE;

			var itemSlotB = UI.MakeItemSlot(UI.MakeButton(bounds, Main.assetsManager.GetAsset<Texture2D>("ui_inventory"),
								new RectangleF(0, 0, 16, 16), new RectangleF(16, 0, 16, 16), new RectangleF(16, 0, 16, 16)),
								anvilInventoryTools.Get(1));

			bounds.x -= SIZE;
			bounds.y += SIZE;

			var itemSlotC = UI.MakeItemSlot(UI.MakeButton(bounds, Main.assetsManager.GetAsset<Texture2D>("ui_inventory"),
								new RectangleF(0, 0, 16, 16), new RectangleF(16, 0, 16, 16), new RectangleF(16, 0, 16, 16)),
								anvilInventoryTools.Get(2));

			bounds.y -= SIZE;

			MenuHelper.ItemSlotClickOutput output = MenuHelper.ItemSlotClickOutput.None;
			if ((output = MenuHelper.HandleItemSlot(player, anvilInventoryTools, 0, itemSlotA, ref held, new MenuHelper.WhiteListNone())) != MenuHelper.ItemSlotClickOutput.None)
			{
				if (output == MenuHelper.ItemSlotClickOutput.NeedsSwapInventory)
					MenuHelper.SwapInventory(anvilInventoryTools, playerInventory, 0);
				inventoryUpdated = true;
			}

			if ((output = MenuHelper.HandleItemSlot(player, anvilInventoryTools, 1, itemSlotB, ref held, new MenuHelper.WhiteListNone())) != MenuHelper.ItemSlotClickOutput.None)
			{
				if (output == MenuHelper.ItemSlotClickOutput.NeedsSwapInventory)
					MenuHelper.SwapInventory(anvilInventoryTools, playerInventory, 1);

				inventoryUpdated = true;
			}

			if ((output = MenuHelper.HandleItemSlot(player, anvilInventoryTools, 2, itemSlotC, ref held, new MenuHelper.WhiteListNone())) != MenuHelper.ItemSlotClickOutput.None)
			{
				if (output == MenuHelper.ItemSlotClickOutput.NeedsSwapInventory)
					MenuHelper.SwapInventory(anvilInventoryTools, playerInventory, 2);

				inventoryUpdated = true;
			}

			if (inventoryUpdated)
			{
				currentRecipe = FindRecipe(anvilInventoryTools);

				if (currentRecipe != null)
				{
					for (int i = 0; i < Math.Min(1, currentRecipe.Outputs.Length); i++)
					{
						anvilInventoryTools.Set(currentRecipe.Outputs[i], 3 + i);
					}
				}
				else
				{
					anvilInventoryTools.Set(new ItemInstance(), 3);
				}
			}

			bounds.y += SIZE * 2;

			UI.MakeTexture(bounds, Main.assetsManager.GetAsset<Texture2D>("ui_inventory"), new RectangleF(32, 32, 16, 16));

			bounds.y += SIZE;

			UI.MakeItemSlot(UI.MakeButton(bounds, Main.assetsManager.GetAsset<Texture2D>("ui_inventory"),
								new RectangleF(0, 0, 16, 16), new RectangleF(16, 0, 16, 16), new RectangleF(16, 0, 16, 16)),
								anvilInventoryTools.Get(3));

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
				player.world.GameStateManager.GetCurrentGameState().PushMenu(new MenuRecipeBook(Main.Registry.CubeRegistry.Get("anvil_iron") as CubeAnvilIron, new ItemInstance()));
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
			if (recipe.Matches(anvilInventoryTools))
			{
				for (int i = 0; i < recipe.Layout.Length; i++)
				{
					if (recipe.Layout[i].valid)
					{
						int numLeft = recipe.Layout[i].num;

						anvilInventoryTools.FindExact(recipe.Layout[i], 3, out int index);

						int overflow = anvilInventoryTools.Get(i).num - numLeft;
						anvilInventoryTools.Remove(index, numLeft);
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

			MenuHelper.DrawHeldItem(batch, held, SIZE, SCALE);
		}
	}
}
