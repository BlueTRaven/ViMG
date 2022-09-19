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
	public class UIInventoryPlayer : UIInventory
	{
		private Player player;
		private Inventory inventory;
		private Inventory craftInventory;

		public int HoverIndex;
		public int HighlightIndex;
		public bool Opened;

		private ItemInstance held;

		private bool craftInventoryUpdated;
		private Recipe currentRecipe;

		private int DEBUGItemListScrollRow = 0;

		public UIInventoryPlayer(Player player, Inventory playerInventory, Inventory craftInventory)
		{
			this.player = player;

			this.inventory = playerInventory;

			this.craftInventory = craftInventory;

			playerInventory.Get(HighlightIndex).item.StartHold(player, playerInventory, HighlightIndex);
		}

		public override void Update()
		{
			base.Update();

			UI.Start();

			UI.StartParent(new Vector2(MARGIN, MARGIN + 32));

			ItemInstance preHighlightedHotbar = inventory.Get(HighlightIndex);
			UIInventoryHelper.DoPlayerInventory(player, inventory, ref held, (Opened ? Player.INVENTORY_ROWS : 1), Player.INVENTORY_COLUMNS, SIZE, PADDING);

			if (preHighlightedHotbar.valid && preHighlightedHotbar.item != inventory.Get(HighlightIndex).item)
            {
				preHighlightedHotbar.item.StartHold(player, inventory, HighlightIndex);

				if (inventory.Get(HighlightIndex).valid)
					inventory.Get(HighlightIndex).item.StartHold(player, inventory, HighlightIndex);
            }

			UI.EndParent();

			if (Opened)
			{
				TextHelper.FontInfo fi = new TextHelper.FontInfo(Main.assetsManager.GetAsset<SpriteFont>("fira_mono_sml"), 1, true);

				UI.StartParent(new Vector2(MARGIN + Player.INVENTORY_COLUMNS * SIZE + Player.INVENTORY_COLUMNS * PADDING + MARGIN_CRAFTING, MARGIN));

				Vector2 pos = new Vector2();
				RectangleF bounds = new RectangleF();

				pos = new Vector2(16 * 3.5f * SCALE, 0);

				UI.EndParent();

				UI.StartParent(new Vector2(MARGIN + Player.INVENTORY_COLUMNS * SIZE + Player.INVENTORY_COLUMNS * PADDING + MARGIN_CRAFTING, MARGIN + 32));

				UI.MakePanel(new Color(139, 139, 139), new RectangleF(0, 0, SIZE * 7, SIZE * 2 + MARGIN * 2));

				UI.StartParent(new Vector2(MARGIN));

				for (int y = 0; y < 2; y++)
				{
					for (int x = 0; x < 3; x++)
					{
						int i = y * 3 + x;

						pos = new Vector2(x * SIZE, y * SIZE);
						bounds = new RectangleF(pos, SIZE, SIZE);

						var itemslot = UI.MakeItemSlot(UI.MakeButton(bounds, Main.assetsManager.GetAsset<Texture2D>("ui_inventory"),
										new RectangleF(0, 0, 16, 16), new RectangleF(16, 0, 16, 16), new RectangleF(16, 0, 16, 16)),
										craftInventory.Get(i));


						var output = UIInventoryHelper.ItemSlotClickOutput.None;
						if ((output = UIInventoryHelper.HandleItemSlot(player, craftInventory, i, itemslot, ref held, new UIInventoryHelper.WhiteListNone())) != UIInventoryHelper.ItemSlotClickOutput.None)
						{
							if (output == UIInventoryHelper.ItemSlotClickOutput.NeedsSwapInventory)
							{
								UIInventoryHelper.SwapInventory(craftInventory, inventory, i);
							}

							craftInventoryUpdated = true;
						}
					}
				}

				if (craftInventoryUpdated)
				{
					currentRecipe = FindRecipe(craftInventory);

					if (currentRecipe != null)
					{
						for (int i = 0; i < Math.Min(2, currentRecipe.Outputs.Length); i++)
						{
							craftInventory.Set(currentRecipe.Outputs[i], 6 + i);
						}
					}
					else
					{
						craftInventory.Set(new ItemInstance(), 6);
						craftInventory.Set(new ItemInstance(), 7);
					}

					craftInventoryUpdated = false;
				}

				bounds = new RectangleF(3 * SIZE, 0, SIZE, SIZE);

				UI.MakeTexture(bounds, Main.assetsManager.GetAsset<Texture2D>("ui_inventory"), new RectangleF(32, 48, 16, 16));

				bounds.x += SIZE;

				UI.MakeItemSlot(UI.MakeButton(bounds, Main.assetsManager.GetAsset<Texture2D>("ui_inventory"),
								new RectangleF(0, 0, 16, 16), new RectangleF(16, 0, 16, 16), new RectangleF(16, 0, 16, 16)),
								craftInventory.Get(6));

				bounds.x += SIZE;

				UI.MakeItemSlot(UI.MakeButton(bounds, Main.assetsManager.GetAsset<Texture2D>("ui_inventory"),
								new RectangleF(0, 0, 16, 16), new RectangleF(16, 0, 16, 16), new RectangleF(16, 0, 16, 16)),
								craftInventory.Get(7));

				bounds.y += SIZE;

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

				pos = new Vector2(0, 2 * SIZE + MARGIN_CRAFTING * 2);
				bounds = new RectangleF(pos, SIZE, SIZE);

				UI.Button recipeBookButton = UI.MakeButton(bounds, Main.assetsManager.GetAsset<Texture2D>("ui_inventory"), new RectangleF(0, 80, 16, 16), new RectangleF(16, 80, 16, 16), new RectangleF(16, 80, 16, 16));

				if (recipeBookButton.hovered)
				{
					UI.DisableParent();
					UI.MakeLabel("Recipe Book", fi, 128, Main.inputManager.GetMousePosition().ToVector2() + new Vector2(16));
					UI.EnableParent();
				}
				
				if (recipeBookButton.clickLeft)
				{
					player.OpenUI(new UIRecipeBook(Main.Registry.RecipeRegistry.PlayerInventoryCatalyst, new ItemInstance()));
				}

				UI.EndParent();

				UI.EndParent();

				if (Main.Debug)
				{
					UI.StartParent(new Vector2(MARGIN, 256));

					var allItems = Main.Registry.ItemRegistry.GetIterable();

					for (int i = DEBUGItemListScrollRow * 8; i < allItems.Count; i++)
					{
						int x = i % 8;
						int y = i / 8;
						RectangleF b = new RectangleF(x * SIZE, (y - DEBUGItemListScrollRow) * SIZE , SIZE, SIZE);

						var cheatSlot = UI.MakeItemSlot(UI.MakeButton(b, Main.assetsManager.GetAsset<Texture2D>("ui_inventory"),
									new RectangleF(0, 0, 16, 16), new RectangleF(16, 0, 16, 16), new RectangleF(16, 0, 16, 16)),
									new ItemInstance(allItems[i], 999, 1));

						if (cheatSlot.button.clickLeft)
						{
							inventory.Add(new ItemInstance(allItems[i], 999, 1));
						}
						else if (cheatSlot.button.clickRight)
						{
							inventory.Add(new ItemInstance(allItems[i], 1, 1));
						}

						if (cheatSlot.button.hovered)
                        {
							int scroll = -Main.inputManager.GetMouseScroll();

							int maxScroll = (int)Math.Ceiling((double)allItems.Count / 8.0) - 7;	//7 is the maximum number of rows on screen at a time.

							if (scroll != 0)
                            {
								DEBUGItemListScrollRow += scroll;

								DEBUGItemListScrollRow = Math.Clamp(DEBUGItemListScrollRow, 0, maxScroll);
                            }

						}
					}

					UI.EndParent();
				}
			}
			else
			{
				for (int y = 0; y < 2; y++)
				{
					for (int x = 0; x < 3; x++)
					{
						int i = y * 3 + x;

						ItemInstance item = craftInventory.Get(i);
						if (item.valid)
						{
							inventory.Add(item);
							craftInventory.Remove(i, item.num);
						}
					}
				}
			}
		}

		private Recipe FindRecipe(Inventory inventory)
		{
			// Null is the equivalent of the "inventory" catalyst
			var recipes = Main.Registry.RecipeRegistry.GetRecipesByCatalyst(Main.Registry.RecipeRegistry.PlayerInventoryCatalyst);

			Recipe rr = null;

			for (int i = 0; i < recipes.Count; i++)
			{
				Recipe recipe = recipes[i];

				if (recipe.Matches(inventory) && (rr == null || recipe.Weight > rr.Weight))
					rr = recipe;
			}

			return rr;
		}

		private void CraftItem(Recipe recipe)
		{
			if (recipe.Matches(craftInventory))
			{
				for (int i = 0; i < recipe.Layout.Length; i++)
				{
					if (recipe.Layout[i].valid)
					{
						int numLeft = recipe.Layout[i].num;

							craftInventory.Find(recipe.Layout[i], 6, out int index);

							int overflow = inventory.Get(i).num - numLeft;
							craftInventory.Remove(index, numLeft);
							craftInventoryUpdated = true;

							if (overflow < 0)
								numLeft -= Math.Abs(overflow);
							else numLeft -= numLeft;
					}
				}

				for (int i = 0; i < recipe.Outputs.Length; i++)
				{
					inventory.Add(recipe.Outputs[i]);
				}
			}
		}

		public override void Draw(SpriteBatch batch)
		{
			base.Draw(batch);

			UI.Draw(batch, SCALE);

			if (Opened)
			{
				UIInventoryHelper.DrawHeldItem(batch, held, SIZE, SCALE);
			}

			const float HEALTHBAR_PADDING = 16;
			const float HEALTHBAR_MAX = 128;

			const float HEALTHBAR_HEIGHT = 16;

			Vector2 healthBarPos = new Vector2(Options.CurrentWindowResolution.X - HEALTHBAR_PADDING - HEALTHBAR_MAX, HEALTHBAR_PADDING);
			Vector2 healthWidthScale = new Vector2((float)player.health / (float)player.maxHealth * HEALTHBAR_MAX, HEALTHBAR_HEIGHT);
			batch.Draw(DrawHelper.WhitePixel, healthBarPos, null, Color.Gray, 0, Vector2.Zero, new Vector2(HEALTHBAR_MAX, HEALTHBAR_HEIGHT), SpriteEffects.None, 0);
			batch.Draw(DrawHelper.WhitePixel, healthBarPos, null, Color.Red, 0, Vector2.Zero, healthWidthScale, SpriteEffects.None, 0.1f);
		}
	}
}
