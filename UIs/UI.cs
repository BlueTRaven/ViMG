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

			public readonly bool isEnabled;

			public readonly Vector2 position;

			public readonly bool valid;

			public ID(int parent, int id, Vector2 position, bool isEnabled)
			{
				this.parent = parent;
				this.id = id;

				this.position = position;

				this.isLast = false;

				this.valid = true;

				this.isEnabled = isEnabled;
			}

			public ID(int id, Vector2 position, bool isEnabled)
			{
				parent = -1;
				this.id = id;
				this.position = position;

				this.isLast = true;

				this.valid = true;

				this.isEnabled = isEnabled;
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

        public struct LabelConstructionParameters 
		{
            public string text;
            public TextHelper.FontInfo font;
            public float width;
            public Vector2 position;

			public bool valid;

            public LabelConstructionParameters(string text, TextHelper.FontInfo font, float width, Vector2 position)
            {
                this.text = text;
                this.font = font;
                this.width = width;
                this.position = position;

				valid = true;
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

			public readonly Label label;
			public readonly Texture2D texture;
			public readonly RectangleF sourceRect;
			public readonly RectangleF hoveredSourceRect;
			public readonly RectangleF clickedSourceRect;

			internal Button(ID id, bool hovered, 
				bool clickLeft, bool heldLeft, bool clickRight, bool heldRight,
				RectangleF bounds, Texture2D texture, Label label,
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
				this.label = label;
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
		private static bool isEnabled = true;

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
				ID id = new ID(currentParent.id, idCounter++, currentParent.position + position, isEnabled);
				ids.Add(id.id, id);

				return id;
			}
			else
			{
				ID id = new ID(idCounter++, position, isEnabled);
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

		public static void BeginDisable()
        {
			isEnabled = false;
        }

		public static void EndDisable()
        {
			isEnabled = true;
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

		//"Craft Recipe", fi, 256, Main.inputManager.GetMousePosition().ToVector2() + new Vector2(16)
		public static Label MakeLabel(string text, TextHelper.FontInfo fi, float width, Vector2 position)
        {
			return MakeLabel(new LabelConstructionParameters(text, fi, width, position));
        }

		public static Label MakeLabel(LabelConstructionParameters parameters)
		{
			if (parameters.valid)
			{
				ID id = MakeID(parameters.position);
				Label label = new Label(id, parameters.text, parameters.font, parameters.width, id.position);
				labels.Add(label);

				return label;
			}
			
			return default(Label);
		}

		public static Button MakeButton(RectangleF bounds, Texture2D texture, RectangleF? sourceRect)
		{
			return MakeButton(bounds, texture, sourceRect, sourceRect, sourceRect);
		}

		public static Button MakeButton(RectangleF bounds, Texture2D texture, RectangleF? sourceRect, RectangleF? hoveredSourceRect, RectangleF? clickedSourceRect)
        {
			return MakeButton(bounds, texture, new LabelConstructionParameters(), sourceRect, hoveredSourceRect, clickedSourceRect);
        }

		public static Button MakeButton(RectangleF bounds, Texture2D texture, LabelConstructionParameters label, RectangleF? sourceRect, RectangleF? hoveredSourceRect, RectangleF? clickedSourceRect)
		{
			ID id = MakeID(bounds.Position);
			RectangleF mouseBounds = new RectangleF(id.position, bounds.Size);

			bool hovered = mouseBounds.Contains(Main.inputManager.GetMousePosition().ToVector2()) && isEnabled;
			bool clickedLeft = hovered && Main.inputManager.JustPressed(A1r.Input.MouseInput.LeftButton);
			bool heldLeft = hovered && Main.inputManager.IsHeld(A1r.Input.MouseInput.LeftButton);
			bool clickedRight = hovered && Main.inputManager.JustPressed(A1r.Input.MouseInput.RightButton);
			bool heldRight = hovered && Main.inputManager.IsHeld(A1r.Input.MouseInput.RightButton);

			RectangleF defaultSr = new RectangleF(texture.Bounds.X, texture.Bounds.Y, texture.Bounds.Width, texture.Bounds.Height);

			StartParent(bounds.Position);

			Label constructedLabel = MakeLabel(label);

			EndParent();

			Button button = new Button(id, hovered, clickedLeft, heldLeft, clickedRight, heldRight, mouseBounds, texture, constructedLabel,
				sourceRect.GetValueOrDefault(defaultSr), hoveredSourceRect.GetValueOrDefault(defaultSr), clickedSourceRect.GetValueOrDefault(defaultSr));
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
				
				/*if (button.label.text != null)
					TextHelper.DrawText(batch, button.label.font, button.label.text, Color.White, 
						new RectangleF(button.label.position, button.label.width, 0).ToRectangle(), 
						Enums.Alignment.TopLeft, (int)button.label.width, 1, TextHelper.OverFlowAction.None);*/
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
				batch.Draw(tex.texture, new Rectangle(tex.bounds.ToRectangle().Location, 
					new Point((int)(tex.bounds.Size.Width * scale), (int)(tex.bounds.Size.Height * scale))),
					tex.sourceRect.ToRectangle(), Color.White, 0, Vector2.Zero, SpriteEffects.None, 0.76f);
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

					batch.DrawRectangle(bounds.ToRectangle(), new Color(139, 139, 139), 0.899f);

					bounds.x += 8;
					bounds.width -= 16;
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
