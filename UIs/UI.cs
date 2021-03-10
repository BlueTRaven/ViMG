using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Items;

namespace ViMG.UIs
{
	public static class UI
	{
		public readonly struct Button
		{
			public readonly bool hovered;
			public readonly bool clicked;
			public readonly bool held;

			public readonly RectangleF bounds;
				   
			public readonly Texture2D texture;
			public readonly RectangleF sourceRect;
			public readonly RectangleF hoveredSourceRect;
			public readonly RectangleF clickedSourceRect;

			public Button(bool hovered, bool clicked, bool held,
				RectangleF bounds, Texture2D texture,
				RectangleF sourceRect, RectangleF hoveredSourceRect, RectangleF clickedSourceRect)
			{
				this.hovered = hovered;
				this.clicked = clicked;
				this.held = held;
				this.bounds = bounds;
				this.texture = texture;
				this.sourceRect = sourceRect;
				this.hoveredSourceRect = hoveredSourceRect;
				this.clickedSourceRect = clickedSourceRect;
			}
		}

		public readonly struct ItemSlot
		{
			public readonly Button button;
			public readonly ItemInstance item;

			public ItemSlot(Button button, ItemInstance item)
			{
				this.button = button;
				this.item = item;
			}
		}

		private static List<Button> buttons = new List<Button>();
		private static List<ItemSlot> itemSlots = new List<ItemSlot>();

		public static void Start()
		{
			buttons.Clear();
			itemSlots.Clear();
		}

		public static void MakeButton(RectangleF bounds, Texture2D texture, RectangleF sourceRect)
		{
			MakeButton(bounds, texture, sourceRect, sourceRect, sourceRect);
		}

		public static Button MakeButton(RectangleF bounds, Texture2D texture, RectangleF sourceRect, RectangleF hoveredSourceRect, RectangleF clickedSourceRect)
		{
			bool hovered = bounds.Contains(Main.inputManager.GetMousePosition().ToVector2());
			bool clicked = hovered && Main.inputManager.JustPressed(A1r.Input.MouseInput.LeftButton);
			bool held = hovered && Main.inputManager.IsHeld(A1r.Input.MouseInput.LeftButton);

			Button button = new Button(hovered, clicked, held, bounds, texture, sourceRect, hoveredSourceRect, clickedSourceRect);
			buttons.Add(button);

			return button;
		}

		public static ItemSlot MakeItemSlot(Button button, ItemInstance item)
		{
			ItemSlot itemSlot = new ItemSlot(button, item);

			itemSlots.Add(itemSlot);

			return itemSlot;
		}

		public static void Draw(SpriteBatch batch, float scale)
		{
			foreach (ItemSlot itemSlot in itemSlots)
			{
				DrawItemSlot(batch, itemSlot, scale);
			}

			foreach (Button button in buttons)
			{
				RectangleF sourceRect = button.sourceRect;

				if (button.hovered)
					sourceRect = button.hoveredSourceRect;
				else if (button.clicked)
					sourceRect = button.clickedSourceRect;

				batch.Draw(button.texture, button.bounds.Position, sourceRect.ToRectangle(), Color.White, 0, Vector2.Zero, scale, SpriteEffects.None, 0.75f);
			}
		}

		private static void DrawItemSlot(SpriteBatch batch, ItemSlot itemSlot, float scale)
		{
			Vector2 mousePos = Main.inputManager.GetMousePosition().ToVector2();

			if (itemSlot.button.hovered)
			{
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
					height += 8;    //for padding

					RectangleF bounds = new RectangleF(mousePos + new Vector2(16), textWidthMax, Math.Max(height, minH));
					batch.DrawRectangle(bounds, new Color(139, 139, 139, 255), 0.89f);

					TextHelper.DrawText(batch, fi, name, Color.White, bounds.ToRectangle(), Enums.Alignment.TopLeft, (int)bounds.width, 0.90f, overflowAction: TextHelper.OverFlowAction.None);

					bounds.y += fi.StringHeight(name);
					bounds.y += 8;
					TextHelper.DrawText(batch, fi, description, Color.White, bounds.ToRectangle(), Enums.Alignment.TopLeft, (int)bounds.width, 0.90f, overflowAction: TextHelper.OverFlowAction.None);
				}
			}

			if (itemSlot.item.item != null)
			{
				batch.Draw(itemSlot.item.item.Texture, itemSlot.button.bounds.Position, itemSlot.item.item.SourceRect.ToRectangle(), Color.White, 0, Vector2.Zero, scale, SpriteEffects.None, 0.86f);

				TextHelper.DrawText(batch, new TextHelper.FontInfo(Main.assetsManager.GetAsset<SpriteFont>("fira_mono_sml"), 1, true, Color.Black),
					itemSlot.item.num.ToString(), Color.White, itemSlot.button.bounds.ToRectangle(), Enums.Alignment.BottomRight, (int)itemSlot.button.bounds.width, 0.87f, overflowAction: TextHelper.OverFlowAction.None);
			}
		}
	}
}
