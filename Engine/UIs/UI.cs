using BrNineSlice;
using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Items;
using static ViMG.UIs.UI;

namespace ViMG.UIs
{
	public static class UI
	{
		static UI()
		{
			Main.WindowTextInputEvent += Input;
		}

		private const string SPECIALCHARS = "~`!@#$%^&*()_+-={}[]:\";\'<>,.?/|\\ ";
		private static char character;
		private static Keys key;

		private static void Input(object? sender, TextInputEventArgs args)
		{
			if (currentStr == null)
				return;

			character = args.Character;
			key = args.Key;

			if (character == '\b')
			{
				if (currentStr.Length > 0)
				{
					if (cursor > 0)
					{
						currentStr = currentStr[0..cursor] + currentStr[(cursor + 1)..];
						cursor--;
					}
                    else
                    {
						currentStr = currentStr[0..cursor] + currentStr[(cursor + 1)..];
					}
				}
			}
			else if (character == '\u007f')
			{
				if (currentStr.Length > 0)
				{
					int i = currentStr.LastIndexOfAny(SPECIALCHARS.ToCharArray(), cursor);
					if (i > 0)
					{
						if (i == cursor)
						{   //we're on the special character, just delete it
							currentStr = currentStr[0..cursor] + currentStr[(cursor + 1)..];
							cursor--;
						}
						else
						{
							currentStr = currentStr[0..(i + 1)] + currentStr[(cursor + 1)..];
							cursor = i;
						}
						/*if (i == currentStr.Length - 1)
							currentStr = currentStr[0..^1];	
						else currentStr = currentStr[0..(i + 1)];*/
					}
					else
					{
						currentStr = currentStr[(cursor + 1)..];   //didn't find any special characters - just delete the entire string
						cursor = 0;
					}
				}
			}
			else
			{
				if (!flags.HasFlag(TextInputFlags.Numerical) && char.IsNumber(character))
					return;
				if (!flags.HasFlag(TextInputFlags.Alphabetical) && char.IsLetter(character))
					return;
				if (!flags.HasFlag(TextInputFlags.Special) && SPECIALCHARS.Contains(character))
					return;
				//TODO fix not allowed unless special characters are allowed.
				if (!flags.HasFlag(TextInputFlags.Special) && !flags.HasFlag(TextInputFlags.Space) && character == ' ')
					return;

				if (currentStr == "")
					currentStr += character;
				else
				{
					if (currentStr.Length >= 1)
					{
						int insertAt = cursor + 1;
						currentStr = currentStr.Insert(insertAt, character.ToString());
						cursor++;
					}
					else currentStr = currentStr.Insert(cursor, character.ToString());
				}
			}
		}

		public enum TextInputFlags
        {
			None = 0,
			Alphabetical = 1 << 0,
			Numerical = 1 << 1,
			Special = 1 << 2,
			Space = 1 << 3,	//if special & space > 0, space is allowed
			AlphaNumerical = Alphabetical | Numerical,
			AlphaNumericalSpecial = Alphabetical | Numerical | Special,
			All = ~0,
        }

		public readonly struct ID
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

		public struct TextureConstructionParameters
        {
			public Texture2D texture;
			public RectangleF bounds;
			public RectangleF sourceRect;
			public Color color;

			public bool below;

			public TextureConstructionParameters(RectangleF bounds, Texture2D texture, RectangleF sourceRect, Color? color = null, bool below = false)
            {
                this.bounds = bounds;
                this.texture = texture;
                this.sourceRect = sourceRect;
				this.color = color ?? Color.White;
				this.below = below;
            }
		}

		public readonly struct Texture
		{
			internal readonly ID id;

			public readonly Texture2D texture;
			public readonly RectangleF bounds;
			public readonly RectangleF sourceRect;
			public readonly Color color;
            public readonly bool below;

            internal Texture(ID id, RectangleF bounds, Texture2D texture, RectangleF sourceRect, Color? color = null, bool below = false)
			{
				this.id = id;

				this.texture = texture;
				this.sourceRect = sourceRect;
				this.bounds = bounds;
				this.color = color ?? Color.White;

				this.below = below;
			}
		}

		public struct PanelConstructionParameters
		{
            public Color color;
            public RectangleF bounds;
            public NineSlice nineslice;
            public bool appearAboveItemSlots;

			public PanelConstructionParameters(RectangleF bounds, Color color, NineSlice nineslice = null, bool appearAboveItemSlots = false)
			{
				this.color = color;
				this.bounds = bounds;
				this.nineslice = nineslice;
				this.appearAboveItemSlots = appearAboveItemSlots;
			}
        }

		public readonly struct Panel
		{
			internal readonly ID id;

			public readonly Color color;
			public readonly RectangleF bounds;
			public readonly NineSlice nineslice;
            public readonly bool appearAboveItemSlots;

			internal Panel(ID id, PanelConstructionParameters parameters)
			{
				color = parameters.color;
				bounds = parameters.bounds;
				nineslice = parameters.nineslice;
				appearAboveItemSlots = parameters.appearAboveItemSlots;
			}

            internal Panel(ID id, Color color, RectangleF bounds)
			{
				this.id = id;

				this.color = color;
				this.bounds = bounds;

				this.nineslice = null;
            }

            internal Panel(ID id, Color color, RectangleF bounds, NineSlice nineslice)
            {
                this.id = id;

                this.color = color;
                this.bounds = bounds;

                this.nineslice = nineslice;
            }
        }

        public struct LabelConstructionParameters 
		{
            public TextHelper.WrappedText text;
            public TextHelper.FontInfo font;
            public float width;
            public Vector2 position;
			public Color color;

			public bool valid;

            public LabelConstructionParameters(string text, TextHelper.FontInfo font, float width, Vector2 position, Color? color = null)
            {
				this.text = TextHelper.GetWrappedText(font, text, width);
                this.font = font;
                this.width = width;
                this.position = position;
				this.color = color ?? Color.White;

				valid = true;
            }

            public LabelConstructionParameters(TextHelper.WrappedText text, TextHelper.FontInfo font, float width, Vector2 position, Color? color = null)
            {
				this.text = text;
                this.font = font;
                this.width = width;
                this.position = position;
                this.color = color ?? Color.White;

                valid = true;
            }
        }

        public readonly struct Label
		{
			internal readonly ID id;

			public readonly TextHelper.WrappedText text;
			public readonly TextHelper.FontInfo font;
			public readonly float width;
			public readonly Vector2 position;
			public readonly Color color;

			internal Label(ID id, TextHelper.WrappedText text, TextHelper.FontInfo font, float width, Vector2 position, Color? color = null)
			{
				this.id = id;

				this.text = text;
				this.font = font;
				this.width = width;
				this.position = position;

				this.color = color ?? Color.White;
			}
		}

		public struct ButtonConstructionParameters
        {
			public RectangleF bounds;

			public LabelConstructionParameters label;
			public Texture2D texture;
			public Color color;
			public Color hoveredColor;
			public Color clickedColor;
			public RectangleF sourceRect;
			public RectangleF hoveredSourceRect;
			public RectangleF clickedSourceRect;

			public bool valid;

			public ButtonConstructionParameters(RectangleF bounds, Texture2D texture,
				RectangleF? sourceRect)
			{
				this.bounds = bounds;
				this.texture = texture;
				this.color = Color.White;
				this.hoveredColor = Color.White;
				this.clickedColor = Color.White;
				this.label = new LabelConstructionParameters();

				RectangleF sr = sourceRect.GetValueOrDefault(new RectangleF(texture.Bounds.X, texture.Bounds.Y, texture.Bounds.Width, texture.Bounds.Height));

				this.sourceRect = sr;
				this.hoveredSourceRect = sr;
				this.clickedSourceRect = sr;

				valid = true;
			}

			public ButtonConstructionParameters(RectangleF bounds, Texture2D texture,
				LabelConstructionParameters label,
				RectangleF? sourceRect)
            {
				this.bounds = bounds;
				this.texture = texture;
                this.color = Color.White;
                this.hoveredColor = Color.White;
                this.clickedColor = Color.White;
                this.label = label;
				
				RectangleF sr = sourceRect.GetValueOrDefault(new RectangleF(texture.Bounds.X, texture.Bounds.Y, texture.Bounds.Width, texture.Bounds.Height));

				this.sourceRect = sr;
				this.hoveredSourceRect = sr;
				this.clickedSourceRect = sr;

				valid = true;
			}

			public ButtonConstructionParameters(RectangleF bounds, Texture2D texture,
				RectangleF sourceRect, RectangleF hoveredSourceRect, RectangleF clickedSourceRect)
			{
				this.bounds = bounds;
				this.texture = texture;
                this.color = Color.White;
                this.hoveredColor = Color.White;
                this.clickedColor = Color.White;
                this.label = new LabelConstructionParameters();
				this.sourceRect = sourceRect;
				this.hoveredSourceRect = hoveredSourceRect;
				this.clickedSourceRect = clickedSourceRect;

				valid = true;
			}

			public ButtonConstructionParameters(RectangleF bounds, Texture2D texture, 
				LabelConstructionParameters label, 
				RectangleF sourceRect, RectangleF hoveredSourceRect, RectangleF clickedSourceRect)
            {
                this.bounds = bounds;
                this.texture = texture;
                this.color = Color.White;
                this.hoveredColor = Color.White;
                this.clickedColor = Color.White;
                this.label = label;
                this.sourceRect = sourceRect;
                this.hoveredSourceRect = hoveredSourceRect;
                this.clickedSourceRect = clickedSourceRect;

				valid = true;
			}

			public ButtonConstructionParameters(RectangleF bounds, Texture2D texture,
				LabelConstructionParameters label)
            {
				this.bounds = bounds;
				this.texture = texture;
                this.color = Color.White;
                this.hoveredColor = Color.White;
                this.clickedColor = Color.White;
                this.label = label;

				RectangleF defaultSR = new RectangleF(texture.Bounds.X, texture.Bounds.Y, texture.Bounds.Width, texture.Bounds.Height);

				sourceRect = defaultSR;
				hoveredSourceRect = defaultSR;
				clickedSourceRect = defaultSR;

				valid = true;
			}

			public ButtonConstructionParameters(RectangleF bounds, Color color, Color hoveredColor, Color clickedColor)
			{
                this.bounds = bounds;
                this.texture = DrawHelper.WhitePixel;
                this.color = color;
                this.hoveredColor = hoveredColor;
                this.clickedColor = clickedColor;
                this.label = new LabelConstructionParameters();
                this.sourceRect = new RectangleF();
                this.hoveredSourceRect = new RectangleF();
                this.clickedSourceRect = new RectangleF();

                valid = true;
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
			public readonly Color color;
            public readonly Color hoveredColor;
            public readonly Color clickedColor;
            public readonly RectangleF sourceRect;
			public readonly RectangleF hoveredSourceRect;
			public readonly RectangleF clickedSourceRect;

			internal Button(ID id, bool hovered, 
				bool clickLeft, bool heldLeft, bool clickRight, bool heldRight,
				RectangleF bounds, Texture2D texture, Color color, Color hoveredColor,
				Color clickedColor, Label label,
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
				this.color = color;
				this.hoveredColor = hoveredColor;
				this.clickedColor = clickedColor;
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
			public readonly bool lookForInputs;
			public readonly bool lookForOutputs;

            public ItemSlot(Button button, ItemInstance item, bool lookForInputs, bool lookForOutputs)
			{
				this.button = button;
				this.item = item;

				this.lookForInputs = lookForInputs;
				this.lookForOutputs = lookForOutputs;
            }
		}

		private static List<Button> buttons = new List<Button>();
		private static List<ItemSlot> itemSlots = new List<ItemSlot>();
		private static List<Label> labels = new List<Label>();
		private static List<Panel> panels = new List<Panel>();
		private static List<Texture> textures = new List<Texture>();

		private static int iteration;
		private static int idCounter;
		private static Dictionary<int, ID> ids = new Dictionary<int, ID>();
		private static ID currentParent;
		private static bool useParent;
		private static bool isEnabled = true;

		public static void Start()
		{
			iteration++;

			idCounter = 0;
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

		public static ID StartParent(Vector2 position)
		{
			useParent = true;
			currentParent = MakeID(position);

			return currentParent;
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

		public static Vector2 GetParentPosition()
		{
			return currentParent.position;
		}

		public static Texture MakeTexture(TextureConstructionParameters parameters)
        {
			ID id = MakeID(parameters.bounds.Position);
			parameters.bounds = new RectangleF(id.position, parameters.bounds.Size);
			Texture tex = new Texture(id, parameters.bounds, parameters.texture, parameters.sourceRect, parameters.color, parameters.below);
			textures.Add(tex);

			return tex;
		}

		public static Texture MakeTexture(RectangleF bounds, Texture2D texture, RectangleF sourceRect)
		{
			ID id = MakeID(bounds.Position);
			bounds = new RectangleF(id.position, bounds.Size);
			Texture tex = new Texture(id, bounds, texture, sourceRect);
			textures.Add(tex);

			return tex;
		}

		public static Panel MakePanel(PanelConstructionParameters parameters)
		{
            ID id = MakeID(parameters.bounds.Position);
            parameters.bounds = new RectangleF(id.position, parameters.bounds.Size);

			Panel panel = new Panel(id, parameters);
			panels.Add(panel);

			return panel;
        }

		public static Panel MakePanel(Color color, RectangleF bounds, NineSlice nineslice = null)
		{
			ID id = MakeID(bounds.Position);
			bounds = new RectangleF(id.position, bounds.Size);
			Panel panel = new Panel(id, color, bounds, nineslice);
			panels.Add(panel);

			return panel;
		}

		//"Craft Recipe", fi, 256, Main.inputManager.GetMousePosition().ToVector2() + new Vector2(16)
		public static Label MakeLabel(string text, TextHelper.FontInfo fi, float width, Vector2 position, Color? color = null)
        {
			return MakeLabel(new LabelConstructionParameters(text, fi, width, position, color));
        }

		public static Label MakeLabel(LabelConstructionParameters parameters)
		{
			if (parameters.valid)
			{
				ID id = MakeID(parameters.position);
				Label label = new Label(id, parameters.text, parameters.font, parameters.width, id.position, parameters.color);
				labels.Add(label);

				return label;
			}
			
			return default(Label);
		}
		
		public static Button MakeButton(ButtonConstructionParameters parameters)
        {
			ID id = MakeID(parameters.bounds.Position);
			RectangleF mouseBounds = new RectangleF(id.position, parameters.bounds.Size);

			bool hovered = mouseBounds.Contains(Main.inputManager.GetMousePosition().ToVector2()) && isEnabled;
			bool clickedLeft = hovered && Main.inputManager.JustPressed(A1r.Input.MouseInput.LeftButton);
			bool heldLeft = hovered && Main.inputManager.IsHeld(A1r.Input.MouseInput.LeftButton);
			bool clickedRight = hovered && Main.inputManager.JustPressed(A1r.Input.MouseInput.RightButton);
			bool heldRight = hovered && Main.inputManager.IsHeld(A1r.Input.MouseInput.RightButton);

			if (clickedLeft)
				Main.inputManager.InputCaptured = false;

			StartParent(parameters.bounds.Position);

			Label constructedLabel = MakeLabel(parameters.label);

			EndParent();

			Button button = new Button(id, hovered, clickedLeft, heldLeft, clickedRight, 
				heldRight, mouseBounds, parameters.texture, parameters.color, parameters.hoveredColor, 
				parameters.clickedColor, constructedLabel, parameters.sourceRect, parameters.hoveredSourceRect, 
				parameters.clickedSourceRect);
			buttons.Add(button);

			return button;
		}

		private static string currentStr;
		private static TextInputFlags flags;
		private static ID currentFocusedId;
		private static int cursor;
		public static void MakeTextbox(ButtonConstructionParameters buttonParams, ref string str, TextInputFlags flags, TextHelper.FontInfo fontInfo)
		{
			var id = MakeID(buttonParams.bounds.Position);

			StartParent(buttonParams.bounds.Position);
			buttonParams.bounds.Position = Vector2.Zero;

			var button = MakeButton(buttonParams);
			var label = MakeLabel(new LabelConstructionParameters(str, fontInfo, 1000, Vector2.Zero));

			if (button.clickLeft)
			{
                currentStr = str;
				cursor = Math.Max(0, currentStr.Length - 1);

				currentFocusedId = label.id;
				UI.flags = flags;
            }

            if (Main.inputManager.JustPressed(A1r.Input.MouseInput.LeftButton) && !button.clickLeft && currentFocusedId.id == label.id.id)
				currentFocusedId = new ID();

			if (currentFocusedId.valid && currentFocusedId.id == label.id.id)
			{
				str = currentStr;

				if (!Main.inputManager.IsHeld(Keys.LeftControl))
				{
					if (Main.inputManager.JustPressed(Keys.Left))
						cursor--;
					if (Main.inputManager.JustPressed(Keys.Right))
						cursor++;
				}
                else
                {
					if (Main.inputManager.JustPressed(Keys.Left) && cursor > 0)
					{
						cursor = currentStr.LastIndexOfAny(SPECIALCHARS.ToCharArray(), cursor - 1);
						//didn't find any
						if (cursor == -1)
							cursor = 0;
					}
					if (Main.inputManager.JustPressed(Keys.Right) && cursor < currentStr.Length)
					{
						cursor = currentStr.IndexOfAny(SPECIALCHARS.ToCharArray(), cursor + 1);
						if (cursor == -1)
							cursor = currentStr.Length - 1;
					}
				}

				if (Main.inputManager.JustPressed(Keys.End))
					cursor = currentStr.Length - 1;
				if (Main.inputManager.JustPressed(Keys.Home))
					cursor = 0;

				if (currentStr != null)
					cursor = MathHelper.Clamp(cursor, 0, currentStr.Length - 1);
			}

			EndParent();
		}

        public static ItemSlot MakeItemSlot(Button button, ItemInstance item)
		{
			bool lookForInputs = button.hovered && Main.inputManager.JustPressed(Keys.U);
			bool lookForOutputs = button.hovered && Main.inputManager.JustPressed(Keys.R);
			ItemSlot itemSlot = new ItemSlot(button, item, lookForInputs, lookForOutputs);

			if (item.item != null && button.hovered)
			{
				UIWidgets.MakeTooltip(Vector2.Zero, item.item.GetName(item), item.item.GetDescription(item));
			}

			itemSlots.Add(itemSlot);

			return itemSlot;
		}

		public static void Draw(SpriteBatch batch, float scale)
		{
			foreach (ItemSlot itemSlot in itemSlots)
			{
				DrawItemSlot(batch, itemSlot, scale);
			}

			/*foreach (Tooltip tooltip in tooltips) 
			{
				DrawTooltip(batch, tooltip, scale);
			}*/

			foreach (Button button in buttons)
			{
				RectangleF sourceRect = button.sourceRect;
				Color color = button.color;

				if (button.hovered)
				{
					sourceRect = button.hoveredSourceRect;
					color = button.hoveredColor;
				}
				else if (button.clickLeft)
				{
					sourceRect = button.clickedSourceRect;
					color = button.clickedColor;
				}

				Vector2 s = new Vector2(scale);
				if (button.texture == DrawHelper.WhitePixel)
					s *= button.bounds.Size.ToVector2();

				batch.Draw(button.texture, button.bounds.Position, sourceRect.ToRectangle(), color, 0, Vector2.Zero, s, SpriteEffects.None, 0.75f);
				
				/*if (button.label.text != null)
					TextHelper.DrawText(batch, button.label.font, button.label.text, Color.White, 
						new RectangleF(button.label.position, button.label.width, 0).ToRectangle(), 
						Enums.Alignment.TopLeft, (int)button.label.width, 1, TextHelper.OverFlowAction.None);*/
			}

			foreach (Label label in labels)
			{
				Rectangle bounds = new RectangleF(label.position, label.width, 0).ToRectangle();

                Vector2 alignmentOffset = TextHelper.GetAlignmentOffset(label.font, label.text.text, label.text.offset, label.text.length,
					bounds, Enums.Alignment.TopLeft);

				TextHelper.DrawText(batch, label.font, label.text, alignmentOffset, label.color, bounds, 1, TextHelper.OverFlowAction.None);

				//This is exclusively here for drawing textInput's cursor, since textInput uses a label.
				if (iteration % 60 < 30)
				{
					//Note that this doesn't really work at all if the text is wrapped...
					//But mapping pre-wrapped text to post-wrapped text is actually pretty hard.
					//For now I'm not even going to bother.
					if (currentFocusedId.id == label.id.id && cursor < label.text.offset + label.text.length && label.text.length > 0)
					{
						Vector2 leadUp = label.position + label.font.StringSize(label.text.text[..cursor]).ToVector2();
						leadUp.Y -= label.font.LineSpacing;
						Vector2 charSize = label.font.StringSize(label.text.text[cursor].ToString()).ToVector2();
						batch.DrawRectangle(new Rectangle(leadUp.ToPoint(), charSize.ToPoint()), Color.White, 1);
					}
				}
			}

            for (int i = 0; i < panels.Count; i++)
			{
                Panel panel = panels[i];

				float baseLayer = 0.5f;
				if (panel.appearAboveItemSlots)
					baseLayer = 0.90f;
				
				RectangleF bounds = panel.bounds;

				float layer = baseLayer + 0.1f * (i / (float)panels.Count);

                if (panel.nineslice == null)
					batch.DrawRectangle(panel.bounds, panel.color, layer);
				else panel.nineslice.Draw(batch, panel.color, bounds, scale, layer);
			}

			foreach (Texture tex in textures)
			{
				batch.Draw(tex.texture, tex.bounds.ToRectangle(),
					tex.sourceRect.ToRectangle(), tex.color, 0, Vector2.Zero, SpriteEffects.None, tex.below ? 0.74f : 0.76f);
			}
		}

		//private static TextHelper.FontInfo tooltipFontLabel;
		/*private static void DrawTooltip(SpriteBatch batch, Tooltip tooltip, float scale)
		{
            var fi = new TextHelper.FontInfo(Main.assetsManager.GetAsset<SpriteFont>("fira_mono_sml"), 1, true, Color.Black);

            Vector2 mousePos = Main.inputManager.GetMousePosition().ToVector2();

            const int minH = 16;

            int minW = Options.CurrentWindowResolution.X / 10;
            int maxW = Options.CurrentWindowResolution.X / 5;

            
			float widthName = fi.StringWidth(tooltip.title);
            Size sizeDescription = fi.StringSize(TextHelper.WrapText(fi, tooltip.description, maxW));

            float textWidthMax = Math.Max(minW, Math.Max(widthName, sizeDescription.Width));

            float height = fi.StringHeight(tooltip.title);
            height += sizeDescription.Height;
            height += 8;    //for padding

            RectangleF bounds = new RectangleF(mousePos + new Vector2(16), textWidthMax, float.Max(height, minH));
            //batch.DrawRectangle(bounds, new Color(139, 139, 139, 255), 0.89f);

            batch.DrawRectangle(bounds.ToRectangle(), new Color(139, 139, 139), 0.899f);

            bounds.x += 8;
            bounds.width -= 16;
            TextHelper.DrawText(batch, fi, tooltip.title, Color.White, bounds.ToRectangle(), Enums.Alignment.TopLeft, (int)bounds.width, 0.90f, overflowAction: TextHelper.OverFlowAction.None);

            bounds.y += fi.StringHeight(tooltip.title);
            bounds.y += 8;
            TextHelper.DrawText(batch, fi, tooltip.description, Color.White, bounds.ToRectangle(), Enums.Alignment.TopLeft, (int)bounds.width, 0.90f, overflowAction: TextHelper.OverFlowAction.None);
        }*/

		private static void DrawItemSlot(SpriteBatch batch, ItemSlot itemSlot, float scale)
		{
			Vector2 mousePos = Main.inputManager.GetMousePosition().ToVector2();

			/*if (itemSlot.button.hovered)
			{
				if (itemSlot.item.item != null)
				{
					const int minH = 16;

					int minW = Options.CurrentWindowResolution.X / 10;
					int maxW = Options.CurrentWindowResolution.X / 5;

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
			}*/

			if (itemSlot.item.item != null)
			{
				itemSlot.item.item.DrawInInventory(batch, itemSlot.item, itemSlot.button.bounds.Position, scale);
				//batch.Draw(itemSlot.item.item.Texture, itemSlot.button.bounds.Position, itemSlot.item.item.SourceRect.ToRectangle(), Color.White, 0, Vector2.Zero, scale, SpriteEffects.None, 0.86f);

				int num = itemSlot.item.num;

				string numString;

				if (num > 1000)
					numString = string.Format("{0:0.0}k", (float)num / 1000f);
				else if (num == -1)
					numString = "\u221E";
				else numString = num.ToString();

				TextHelper.DrawText(batch, new TextHelper.FontInfo(Main.assetsManager.GetAsset<SpriteFont>("fira_mono_tny"), 1, true, Color.Black),
					numString, Color.White, itemSlot.button.bounds.ToRectangle(), Enums.Alignment.BottomRight, 
					64, 0.87f, overflowAction: TextHelper.OverFlowAction.None);
			}
		}
	}
}
