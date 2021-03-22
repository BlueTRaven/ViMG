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
		internal readonly struct ID
		{
			public readonly int parent;
			public readonly int id;
			public readonly bool isLast;

			public readonly Vector2 position;

			public readonly bool valid;

			public ID(int parent, int id, Vector2 position)
			{
				this.parent = parent;
				this.id = id;

				this.position = position;

				this.isLast = false;

				this.valid = true;
			}

			public ID(int id, Vector2 position)
			{
				parent = -1;
				this.id = id;
				this.position = position;

				this.isLast = true;

				this.valid = true;
			}
		}

		public readonly struct Texture
		{
			internal readonly ID id;

			public readonly Texture2D texture;
			public readonly RectangleF bounds;
			public readonly RectangleF sourceRect;

			internal Texture(ID id, RectangleF bounds, Texture2D texture, RectangleF sourceRect)
			{
				this.id = id;

				this.texture = texture;
				this.sourceRect = sourceRect;
				this.bounds = bounds;
			}
		}

		public readonly struct Panel
		{
			internal readonly ID id;

			public readonly Color color;
			public readonly RectangleF bounds;

			internal Panel(ID id, Color color, RectangleF bounds)
			{
				this.id = id;

				this.color = color;
				this.bounds = bounds;
			}
		}

		public readonly struct Label
		{
			internal readonly ID id;

			public readonly string text;
			public readonly TextHelper.FontInfo font;
			public readonly float width;
			public readonly Vector2 position;

			internal Label(ID id, string text, TextHelper.FontInfo font, float width, Vector2 position)
			{
				this.id = id;

				this.text = text;
				this.font = font;
				this.width = width;
				this.position = position;
			}
		}

		public readonly struct Button
		{
			internal readonly ID id;

			public readonly bool hovered;
			public readonly bool clickLeft;
			public readonly bool heldLeft;
			public readonly bool clickRight;
			public readonly bool heldRight;

			public readonly RectangleF bounds;
				   
			public readonly Texture2D texture;
			public readonly RectangleF sourceRect;
			public readonly RectangleF hoveredSourceRect;
			public readonly RectangleF clickedSourceRect;

			internal Button(ID id, bool hovered, 
				bool clickLeft, bool heldLeft, bool clickRight, bool heldRight,
				RectangleF bounds, Texture2D texture,
				RectangleF sourceRect, RectangleF hoveredSourceRect, RectangleF clickedSourceRect)
			{
				this.id = id;

				this.hovered = hovered;
				this.clickLeft = clickLeft;
				this.heldLeft = heldLeft;
				this.clickRight = clickRight;
				this.heldRight = heldRight;
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
		private static List<Label> labels = new List<Label>();
		private static List<Panel> panels = new List<Panel>();
		private static List<Texture> textures = new List<Texture>();

		private static int idCounter;
		private static Dictionary<int, ID> ids = new Dictionary<int, ID>();
		private static ID currentParent;
		private static bool useParent;

		public static void Start()
		{
			ids.Clear();

			currentParent = new ID();
			buttons.Clear();
			itemSlots.Clear();
			labels.Clear();
			panels.Clear();
			textures.Clear();

			useParent = false;
		}

		internal static ID MakeID(Vector2 position)
		{
			if (currentParent.valid && useParent)
			{
				ID id = new ID(currentParent.id, idCounter++, currentParent.position + position);
				ids.Add(id.id, id);

				return id;
			}
			else
			{
				ID id = new ID(idCounter++, position);
				ids.Add(id.id, id);

				return id;
			}
		}

		public static void StartParent(Vector2 position)
		{
			useParent = true;
			currentParent = MakeID(position);
		}

		public static void EndParent()
		{
			if (currentParent.isLast)
			{
				currentParent = new ID();
				useParent = false;
			}
			else
			{
				currentParent = ids[currentParent.parent];
			}
		}

		public static void DisableParent()
		{
			useParent = false;
		}

		public static void EnableParent()
		{
			useParent = true;
		}

		public static Texture MakeTexture(RectangleF bounds, Texture2D texture, RectangleF sourceRect)
		{
			ID id = MakeID(bounds.Position);
			bounds = new RectangleF(id.position, bounds.Size);
			Texture tex = new Texture(id, bounds, texture, sourceRect);
			textures.Add(tex);

			return tex;
		}

		public static Panel MakePanel(Color color, RectangleF bounds)
		{
			ID id = MakeID(bounds.Position);
			bounds = new RectangleF(id.position, bounds.Size);
			Panel panel = new Panel(id, color, bounds);
			panels.Add(panel);

			return panel;
		}

		public static Label MakeLabel(string text, TextHelper.FontInfo font, float width, Vector2 position)
		{
			ID id = MakeID(position);
			Label label = new Label(id, text, font, width, id.position);
			labels.Add(label);

			return label;
		}

		public static Button MakeButton(RectangleF bounds, Texture2D texture, RectangleF sourceRect)
		{
			return MakeButton(bounds, texture, sourceRect, sourceRect, sourceRect);
		}

		public static Button MakeButton(RectangleF bounds, Texture2D texture, RectangleF sourceRect, RectangleF hoveredSourceRect, RectangleF clickedSourceRect)
		{
			ID id = MakeID(bounds.Position);
			bounds = new RectangleF(id.position, bounds.Size);

			bool hovered = bounds.Contains(Main.inputManager.GetMousePosition().ToVector2());
			bool clickedLeft = hovered && Main.inputManager.JustPressed(A1r.Input.MouseInput.LeftButton);
			bool heldLeft = hovered && Main.inputManager.IsHeld(A1r.Input.MouseInput.LeftButton);
			bool clickedRight = hovered && Main.inputManager.JustPressed(A1r.Input.MouseInput.RightButton);
			bool heldRight = hovered && Main.inputManager.IsHeld(A1r.Input.MouseInput.RightButton);

			Button button = new Button(id, hovered, clickedLeft, heldLeft, clickedRight, heldRight, bounds, texture, sourceRect, hoveredSourceRect, clickedSourceRect);
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
				else if (button.clickLeft)
					sourceRect = button.clickedSourceRect;

				batch.Draw(button.texture, button.bounds.Position, sourceRect.ToRectangle(), Color.White, 0, Vector2.Zero, scale, SpriteEffects.None, 0.75f);
			}

			foreach (Label label in labels)
			{
				TextHelper.DrawText(batch, label.font, label.text, Color.White, new RectangleF(label.position, label.width, 0).ToRectangle(), Enums.Alignment.TopLeft, (int)label.width, 1, TextHelper.OverFlowAction.None);
			}

			foreach (Panel panel in panels)
			{
				batch.DrawRectangle(panel.bounds, panel.color, 0.5f);
			}

			foreach (Texture tex in textures)
			{
				batch.Draw(tex.texture, tex.bounds.ToRectangle(), tex.sourceRect.ToRectangle(), Color.White, 0, Vector2.Zero, SpriteEffects.None, 0.76f);
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

					string name = itemSlot.item.item.GetName(itemSlot.item);
					string description = itemSlot.item.item.GetDescription(itemSlot.item);

					float widthName = fi.StringWidth(name);
					Size sizeDescription = fi.StringSize(TextHelper.WrapText(fi, description, maxW));

					float textWidthMax = Math.Max(minW, Math.Max(widthName, sizeDescription.Width));

					float height = fi.StringHeight(name);
					height += sizeDescription.Height;
					height += 8;    //for padding

					RectangleF bounds = new RectangleF(mousePos + new Vector2(16), textWidthMax, Math.Max(height, minH));
					//batch.DrawRectangle(bounds, new Color(139, 139, 139, 255), 0.89f);

					TextHelper.DrawText(batch, fi, name, Color.White, bounds.ToRectangle(), Enums.Alignment.TopLeft, (int)bounds.width, 0.90f, overflowAction: TextHelper.OverFlowAction.None);

					bounds.y += fi.StringHeight(name);
					bounds.y += 8;
					TextHelper.DrawText(batch, fi, description, Color.White, bounds.ToRectangle(), Enums.Alignment.TopLeft, (int)bounds.width, 0.90f, overflowAction: TextHelper.OverFlowAction.None);
				}
			}

			if (itemSlot.item.item != null)
			{
				itemSlot.item.item.DrawInInventory(batch, itemSlot.item, itemSlot.button.bounds.Position, scale);
				//batch.Draw(itemSlot.item.item.Texture, itemSlot.button.bounds.Position, itemSlot.item.item.SourceRect.ToRectangle(), Color.White, 0, Vector2.Zero, scale, SpriteEffects.None, 0.86f);

				TextHelper.DrawText(batch, new TextHelper.FontInfo(Main.assetsManager.GetAsset<SpriteFont>("fira_mono_sml"), 1, true, Color.Black),
					itemSlot.item.num.ToString(), Color.White, itemSlot.button.bounds.ToRectangle(), Enums.Alignment.BottomRight, (int)itemSlot.button.bounds.width, 0.87f, overflowAction: TextHelper.OverFlowAction.None);
			}
		}
	}
}
