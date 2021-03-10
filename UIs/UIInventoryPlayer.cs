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
	public class UIInventoryPlayer
	{
		private const int PADDING = 0 * SCALE;
		private const int MARGIN = 8 * SCALE;
		private const int PADDING_CRAFTING = 8 * SCALE;
		private const int MARGIN_CRAFTING = 8 * SCALE;
		private const int SIZE = 16 * SCALE;
		private const int SCALE = 2;

		private Inventory inventory;
		private int rows;
		private int columns;

		public int HoverIndex;
		public int HighlightIndex;
		public bool Opened;

		private ItemInstance held;

		public UIInventoryPlayer(Inventory inventory, int rows, int columns)
		{
			this.inventory = inventory;

			this.rows = rows;
			this.columns = columns;
		}

		public void Update()
		{
			UI.Start();

			for (int y = 0; y < (Opened ? rows : 1); y++)
			{
				for (int x = 0; x < columns; x++)
				{
					int i = y * columns + x;

					Vector2 pos = new Vector2(MARGIN + x * SIZE + x * PADDING, MARGIN + y * SIZE + y * PADDING);
					RectangleF bounds = new RectangleF(pos, SIZE, SIZE);

					var itemslot = UI.MakeItemSlot(UI.MakeButton(bounds, Main.assetsManager.GetAsset<Texture2D>("ui_inventory"),
									new RectangleF(16, 0, 16, 16), new RectangleF(0, 0, 16, 16), new RectangleF(0, 0, 16, 16)),
									inventory.Get(i));

					if (itemslot.button.clicked)
					{
						if (!held.valid && itemslot.item.valid)
						{
							held = itemslot.item.Copy();
							inventory.Remove(i, held.num);
						}
						else if (held.valid && itemslot.item.valid)
						{
							// Merge stacks
							if (inventory.Get(i).item == held.item)
							{
								inventory.Set(new ItemInstance(held, itemslot.item.num + held.num), i);
								held = new ItemInstance();
							}
							else
							{
								// Swap stacks
								var oldHeld = held;
								held = itemslot.item.Copy();
								inventory.Set(oldHeld, i);
							}
						}
						else if (held.valid && !itemslot.item.valid)
						{
							inventory.Set(held, i);
							held = new ItemInstance();
						}
					}
				}
			}

			if (Opened)
			{
				var recipes = Main.Registry.RecipeRegistry.GetRecipes();
				
				for (int i = 0; i < Main.Registry.RecipeRegistry.Count; i++)
				{
					int yoff = (MARGIN + SIZE + MARGIN + MARGIN) * i;

					Recipe recipe = recipes[i];

					for (int y = 0; y < 2; y++)
					{
						for (int x = 0; x < 3; x++)
						{
							Vector2 pos = new Vector2(MARGIN + columns * SIZE + columns * PADDING + MARGIN_CRAFTING + x * SIZE, MARGIN + SIZE + y * SIZE + yoff);
							RectangleF bounds = new RectangleF(pos, SIZE, SIZE);

							int recipeItemIndex = y * columns + x;

							if (recipeItemIndex < recipe.Layout.Length)
								UI.MakeItemSlot(UI.MakeButton(bounds, Main.assetsManager.GetAsset<Texture2D>("ui_inventory"), 
									new RectangleF(16, 0, 16, 16), new RectangleF(0, 0, 16, 16), new RectangleF(0, 0, 16, 16)), 
									recipe.Layout[recipeItemIndex]);
							else UI.MakeItemSlot(UI.MakeButton(bounds, Main.assetsManager.GetAsset<Texture2D>("ui_inventory"),
									new RectangleF(16, 0, 16, 16), new RectangleF(0, 0, 16, 16), new RectangleF(0, 0, 16, 16)),
									new ItemInstance());
						}
					}

					if (recipe.Outputs.Length > 0)
					{
						Vector2 pos = new Vector2(MARGIN + columns * SIZE + columns * PADDING + MARGIN_CRAFTING + 3 * SIZE + MARGIN_CRAFTING, MARGIN + SIZE + yoff);
						RectangleF bounds = new RectangleF(pos, SIZE, SIZE);

						UI.MakeItemSlot(UI.MakeButton(bounds, Main.assetsManager.GetAsset<Texture2D>("ui_inventory"),
									new RectangleF(16, 0, 16, 16), new RectangleF(0, 0, 16, 16), new RectangleF(0, 0, 16, 16)),
									recipe.Outputs[0]);

						pos.Y += SIZE;
						bounds = new RectangleF(pos, SIZE, SIZE);

						if (UI.MakeButton(bounds, Main.assetsManager.GetAsset<Texture2D>("ui_inventory"), new RectangleF(0, 96, 16, 16), new RectangleF(16, 96, 16, 16), new RectangleF(16, 96, 16, 16)).clicked)
						{
							CraftItem(recipe);
						}
					}
				}
			}
		}

		private void CraftItem(Recipe recipe)
		{
			if (recipe.Matches(inventory))
			{
				for (int i = 0; i < recipe.Layout.Length; i++)
				{
					if (recipe.Layout[i].valid)
					{
						int numLeft = recipe.Layout[i].num;

						while (numLeft > 0)
						{
							inventory.Find(recipe.Layout[i].item, out int index);

							int overflow = inventory.Get(i).num - numLeft;
							inventory.Remove(index, numLeft);

							if (overflow < 0)
								numLeft -= Math.Abs(overflow);
							else numLeft -= numLeft;
						}
					}
				}

				for (int i = 0; i < recipe.Outputs.Length; i++)
				{
					inventory.Add(recipe.Outputs[i]);
				}
			}
		}

		/*private Button MakeButton(RectangleF bounds, Texture2D texture, RectangleF sourceRect, RectangleF sourceRectHovered)
		{
			bool hovered = bounds.Contains(Main.inputManager.GetMousePosition().ToVector2());
			bool clicked = hovered && Main.inputManager.JustPressed(A1r.Input.MouseInput.LeftButton);
			bool held = hovered && Main.inputManager.IsHeld(A1r.Input.MouseInput.LeftButton);

			Button button = new Button()
			{
				hovered = hovered,
				clicked = clicked,
				held = held,

				bounds = bounds,
				texture = texture,
				sourceRect = hovered ? sourceRectHovered : sourceRect
			};

			buttons.Add(button);

			return button;
		}*/

		/*private ItemSlot MakeItemSlot(RectangleF bounds, ItemInstance item)
		{
			bool hovered = bounds.Contains(Main.inputManager.GetMousePosition().ToVector2());
			bool clicked = hovered && Main.inputManager.JustPressed(A1r.Input.MouseInput.LeftButton);
			bool held = hovered && Main.inputManager.IsHeld(A1r.Input.MouseInput.LeftButton);

			Button button = new Button()
			{
				hovered = hovered,
				clicked = clicked,
				held = held,

				bounds = bounds,
				texture = Main.assetsManager.GetAsset<Texture2D>("ui_inventory"),
				sourceRect = hovered ? new RectangleF(16, 0, 16, 16) : new RectangleF(0, 0, 16, 16)
			};

			ItemSlot itemSlot = new ItemSlot()
			{
				button = button,
				item = item
			};

			itemslots.Add(itemSlot);

			return itemSlot;
		}*/

		public void Draw(SpriteBatch batch)
		{
			if (Opened)
			{
				TextHelper.FontInfo fi = new TextHelper.FontInfo(Main.assetsManager.GetAsset<SpriteFont>("fira_mono_sml"), 1, false);

				TextHelper.DrawText(batch, fi, "Inputs", Color.White, new RectangleF(MARGIN + columns * SIZE + columns * PADDING + MARGIN_CRAFTING, MARGIN, 16 * 3 * SCALE, 16).ToRectangle(), Enums.Alignment.TopLeft, 16 * 3 * SCALE, overflowAction: TextHelper.OverFlowAction.None);

				TextHelper.DrawText(batch, fi, "Outputs", Color.White, new RectangleF(16 * 3.5f * SCALE + MARGIN + columns * SIZE + columns * PADDING + MARGIN_CRAFTING, MARGIN, 16 * 3 * SCALE, 16).ToRectangle(), Enums.Alignment.TopLeft, 16 * 3 * SCALE, overflowAction: TextHelper.OverFlowAction.None);
			}

			UI.Draw(batch, SCALE);

			/*foreach (ItemSlot itemSlot in itemslots)
			{
				DrawItemSlot(batch, itemSlot);
			}

			foreach (Button button in buttons)
			{
				batch.Draw(button.texture, button.bounds.Position, button.sourceRect.ToRectangle(), Color.White, 0, Vector2.Zero, SCALE, SpriteEffects.None, 0.75f);
			}*/

			if (Opened)
			{
				if (held.valid)
				{
					var fi = new TextHelper.FontInfo(Main.assetsManager.GetAsset<SpriteFont>("fira_mono_sml"), 1, true, Color.Black);

					Vector2 pos = Main.inputManager.GetMousePosition().ToVector2();
					RectangleF bounds = new RectangleF(pos, SIZE, SIZE);

					batch.Draw(held.item.Texture, pos, held.item.SourceRect.ToRectangle(), Color.White, 0, Vector2.Zero, SCALE, SpriteEffects.None, 0);

					TextHelper.DrawText(batch, fi,
						held.num.ToString(), Color.White, bounds.ToRectangle(), Enums.Alignment.BottomRight, (int)bounds.width, overflowAction: TextHelper.OverFlowAction.None);
				}
			}
		}

		/*private void DrawItemSlot(SpriteBatch batch, ItemSlot itemSlot)
		{
			Vector2 mousePos = Main.inputManager.GetMousePosition().ToVector2();

			if (itemSlot.button.hovered)
			{
				batch.Draw(itemSlot.button.texture, itemSlot.button.bounds.Position, new Rectangle(16, 0, 16, 16), Color.White, 0, Vector2.Zero, SCALE, SpriteEffects.None, 0.85f);

				if (itemSlot.item.item != null)
				{
					const int minW = 128;
					const int minH = 16;

					const int maxW = 256;

					var fi = new TextHelper.FontInfo(Main.assetsManager.GetAsset<SpriteFont>("fira_mono_sml"), 1, true, Color.Black);

					string name = itemSlot.item.item.Name;
					string description = itemSlot.item.item.Description;

					float widthName = fi.StringWidth(name);
					Size sizeDescription = fi.StringSize(TextHelper.WrapText(fi, description, maxW));

					float textWidthMax = Math.Max(minW, Math.Max(widthName, sizeDescription.Width));

					float height = fi.StringHeight(name);
					height += sizeDescription.Height;
					height += 8;	//for padding

					RectangleF bounds = new RectangleF(mousePos + new Vector2(16), textWidthMax, Math.Max(height, minH));
					batch.DrawRectangle(bounds, new Color(139, 139, 139, 255), 0.89f);
					
					TextHelper.DrawText(batch, fi, name, Color.White, bounds.ToRectangle(), Enums.Alignment.TopLeft, (int)bounds.width, 0.90f, overflowAction: TextHelper.OverFlowAction.None);

					bounds.y += fi.StringHeight(name);
					bounds.y += 8;
					TextHelper.DrawText(batch, fi, description, Color.White, bounds.ToRectangle(), Enums.Alignment.TopLeft, (int)bounds.width, 0.90f, overflowAction: TextHelper.OverFlowAction.None);
				}
			}
			else
				batch.Draw(itemSlot.button.texture, itemSlot.button.bounds.Position, new Rectangle(0, 0, 16, 16), Color.White, 0, Vector2.Zero, SCALE, SpriteEffects.None, 0.85f);

			if (itemSlot.item.item != null)
			{
				batch.Draw(itemSlot.item.item.Texture, itemSlot.button.bounds.Position, itemSlot.item.item.SourceRect.ToRectangle(), Color.White, 0, Vector2.Zero, SCALE, SpriteEffects.None, 0.86f);

				TextHelper.DrawText(batch, new TextHelper.FontInfo(Main.assetsManager.GetAsset<SpriteFont>("fira_mono_sml"), 1, true, Color.Black),
					itemSlot.item.num.ToString(), Color.White, itemSlot.button.bounds.ToRectangle(), Enums.Alignment.BottomRight, (int)itemSlot.button.bounds.width, 0.87f, overflowAction: TextHelper.OverFlowAction.None);
			}
		}*/
	}
}
