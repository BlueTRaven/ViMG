using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Items;

namespace ViMG
{
	public class InventoryInteractorPlayer
	{
		private const int PADDING = 0 * SCALE;
		private const int MARGIN = 8 * SCALE;
		private const int SIZE = 16 * SCALE;
		private const int SCALE = 4;

		private Inventory inventory;
		private int rows;
		private int columns;

		public int HoverIndex;
		public int HighlightIndex;

		private ItemInstance held;

		public InventoryInteractorPlayer(Inventory inventory, int rows, int columns)
		{
			this.inventory = inventory;

			this.rows = rows;
			this.columns = columns;
		}

		public void Update()
		{
			Point point = Main.inputManager.GetMousePosition();
			int hovered = -1;

			for (int y = 0; y < rows; y++)
			{
				for (int x = 0; x < columns; x++)
				{
					int i = y * columns + x;

					Vector2 pos = new Vector2(MARGIN + x * SIZE + x * PADDING, MARGIN + y * SIZE + y * PADDING);
					RectangleF bounds = new RectangleF(pos, SIZE, SIZE);

					if (bounds.Contains(point.ToVector2()))
					{
						hovered = i;

						if (Main.inputManager.JustPressed(A1r.Input.MouseInput.LeftButton))
						{
							if (!held.valid && inventory.Get(hovered).valid)
							{
								held = inventory.Get(hovered).Copy();
								inventory.Remove(hovered, held.num);
							}
							else if (held.valid && inventory.Get(hovered).valid)
							{
								// Merge stacks
								if (inventory.Get(hovered).item == held.item)
								{
									inventory.Set(new ItemInstance(held, inventory.Get(hovered).num + held.num), hovered);
								}
								else
								{
									// Swap stacks
									var oldHeld = held;
									held = inventory.Get(hovered).Copy();
									inventory.Set(oldHeld, hovered);
								}
							}
							else if (held.valid && !inventory.Get(hovered).valid)
							{
								inventory.Set(held, hovered);
								held = new ItemInstance();
							}
						}
					}
				}
			}

			HoverIndex = hovered;
		}

		public void DrawUnopened(SpriteBatch batch)
		{
			for (int y = 0; y < 1; y++)
			{
				for (int x = 0; x < columns; x++)
				{
					int i = y * columns + x;

					Item item = inventory.Get(i).item;

					Vector2 pos = new Vector2(MARGIN + x * SIZE + x * PADDING, MARGIN + y * SIZE + y * PADDING);
					RectangleF bounds = new RectangleF(pos, SIZE, SIZE);

					if (HoverIndex == i || HighlightIndex == i)
					{
						batch.DrawRectangle(bounds, Color.Gray * 0.25f);
						batch.DrawHollowRectangle(bounds, 4, Color.Black);
					}
					else
					{
						batch.DrawRectangle(bounds, Color.Gray * 0.25f);
						batch.DrawHollowRectangle(bounds, 4, Color.LightGray);
					}

					if (item != null)
					{
						batch.Draw(item.Texture, pos, item.SourceRect.ToRectangle(), Color.White, 0, Vector2.Zero, SCALE, SpriteEffects.None, 0);

						TextHelper.DrawText(batch, new TextHelper.FontInfo(Main.assetsManager.GetAsset<SpriteFont>("fira_mono"), 1, true, Color.Black),
							inventory.Get(i).num.ToString(), Color.White, bounds.ToRectangle(), Enums.Alignment.BottomRight, (int)bounds.width);
					}
				}
			}
		}

		public void DrawOpen(SpriteBatch batch)
		{
			for (int y = 1; y < rows; y++)
			{
				for (int x = 0; x < columns; x++)
				{
					int i = y * columns + x;

					Item item = inventory.Get(i).item;

					Vector2 pos = new Vector2(MARGIN + x * SIZE + x * PADDING, MARGIN + y * SIZE + y * PADDING);
					RectangleF bounds = new RectangleF(pos, SIZE, SIZE);

					if (HoverIndex == i)
					{
						batch.DrawRectangle(bounds, Color.Gray * 0.25f);
						batch.DrawHollowRectangle(bounds, 4, Color.Black);
					}
					else
					{
						batch.DrawRectangle(bounds, Color.Gray * 0.25f);
						batch.DrawHollowRectangle(bounds, 4, Color.LightGray);
					}

					if (item != null)
					{
						batch.Draw(item.Texture, pos, item.SourceRect.ToRectangle(), Color.White, 0, Vector2.Zero, SCALE, SpriteEffects.None, 0);

						TextHelper.DrawText(batch, new TextHelper.FontInfo(Main.assetsManager.GetAsset<SpriteFont>("fira_mono"), 1, true, Color.Black),
							inventory.Get(i).num.ToString(), Color.White, bounds.ToRectangle(), Enums.Alignment.BottomRight, (int)bounds.width);
					}
				}
			}

			if (held.valid)
			{
				Vector2 pos = Main.inputManager.GetMousePosition().ToVector2();
				RectangleF bounds = new RectangleF(pos, SIZE, SIZE);

				batch.Draw(held.item.Texture, pos, held.item.SourceRect.ToRectangle(), Color.White, 0, Vector2.Zero, SCALE, SpriteEffects.None, 0);

				TextHelper.DrawText(batch, new TextHelper.FontInfo(Main.assetsManager.GetAsset<SpriteFont>("fira_mono"), 1, true, Color.Black),
					held.num.ToString(), Color.White, bounds.ToRectangle(), Enums.Alignment.BottomRight, (int)bounds.width);
			}
		}
	}
}
