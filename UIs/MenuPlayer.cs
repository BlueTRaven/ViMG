using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Buffs;
using ViMG.GameStates;
using ViMG.Items;
using ViMG.Recipes;

namespace ViMG.UIs
{
	public class MenuPlayer : Menu
	{
		private struct PickedUpItem
        {
			public const float DUR_HOLDMID = 0.2f;
			public const float DUR_ANIM_TO_SIDE = 0.4f;
			public const float DUR_HOLDSIDE = 4f;
			public const float DUR_TOTAL = 6f;

			public ItemInstance item;
			public float timer;
			public int currentIndex;
			public int previousIndex;

			public PickedUpItem(ItemInstance item, int index)
            {
				this.item = item;

				timer = 0;

				currentIndex = index;
				previousIndex = index;
            }
        }

		private const float HEALTHBAR_PADDING = 16;
		private const float HEALTHBAR_MAX = 128;
		private const float WIDTH_PER_HEALTH = HEALTHBAR_MAX / 20f;
		private const float WIDTH_PER_MAGIC = HEALTHBAR_MAX / 20f;

		private const float HEALTHBAR_HEIGHT = 16;

		private Player player;
		private Inventory inventory;
		private Inventory craftInventory;
		private Inventory accessoryInventory;
		private Inventory gearInventory;

		private static string[] tagsLegs = new string[1] { "armor_legs" };
		private static string[] tagsBody = new string[1] { "armor_body" };
		private static string[] tagsHead = new string[1] { "armor_head" };
		private static string[] tagsAccessories = new string[1] { "accessory" };

		private static string[][] tagsGearBySlot = new string[3][]
		{
			new string[1] { "gear_heart" },
			new string[1] { "gear_run" },
			new string[1] { "gear_dj" },
        };

		private static string[][] tagsAccessoriesBySlot = new string[6][]
		{
			tagsLegs,
			tagsBody,
			tagsHead,
			tagsAccessories,
			tagsAccessories,
			tagsAccessories,
		};

        public int HoverIndex;
		public int HighlightIndex;
		private bool opened;
		public bool IsOpened => opened;

		private ItemInstance held;
		private FastList<PickedUpItem> pickedupItems = new FastList<PickedUpItem>();

		private bool craftInventoryUpdated;
		private Recipe currentRecipe;

		private int DEBUGItemListScrollRow = 0;

		private TextHelper.FontInfo fi;

		public MenuPlayer(GameStateManager gsManager, Player player, Inventory playerInventory, Inventory craftInventory, Inventory accessoryInventory, Inventory gearInventory) : base(gsManager)
		{
			this.player = player;

			this.inventory = playerInventory;

			this.craftInventory = craftInventory;
            this.accessoryInventory = accessoryInventory;
			this.gearInventory = gearInventory;
            playerInventory.Get(HighlightIndex).item?.StartHold(player, playerInventory, HighlightIndex);

			fi = new TextHelper.FontInfo(Main.assetsManager.GetAsset<SpriteFont>("fira_mono_sml"), 1, true);
		}

		public void Open()
        {
			opened = true;

			Main.MouseControl = true;
			Main.DrawCursor = true;

			Options.CenterMouse();
        }

		public void Close()
        {
			opened = false;

			Main.MouseControl = false;
			Main.DrawCursor = false;

			Options.CenterMouse();
		}

        public override void OnOpen()
        {
            base.OnOpen();

			//reapply mouse/cursor settings
			if (opened)
				Open();
			else Close();
        }

        public void Toggle()
        {
			if (opened)
				Close();
			else Open();
        }

		public override void Update(GraphicsDevice device, double deltaTime)
		{
			base.Update(device, deltaTime);

			UI.Start();

			if (gsManager.TheIsland.IsLoading)
			{
				/*string message = gsManager.TheIsland.LoadMessage;
				Size size = fi.StringSize(message);

				UI.MakeLabel(new UI.LabelConstructionParameters(gsManager.TheIsland.LoadMessage, fi, Options.CurrentWindowResolution.X,
					  new Vector2(Options.CurrentWindowResolution.X / 2f - size.Width / 2f, Options.CurrentWindowResolution.Y / 2f - size.Height / 2f)));*/

				return;
			}

			UI.StartParent(new Vector2(MARGIN, MARGIN + 32));

			ItemInstance preHighlightedHotbar = inventory.Get(HighlightIndex);
			MenuHelper.DoPlayerInventory(player, inventory, ref held, (opened ? Player.INVENTORY_ROWS : 1), Player.INVENTORY_COLUMNS, 18 * 2f, 2f);

			if (preHighlightedHotbar.valid && preHighlightedHotbar.item != inventory.Get(HighlightIndex).item)
            {
				preHighlightedHotbar.item.StartHold(player, inventory, HighlightIndex);

				if (inventory.Get(HighlightIndex).valid)
					inventory.Get(HighlightIndex).item.StartHold(player, inventory, HighlightIndex);
            }

			UI.EndParent();

			UI.StartParent(new Vector2(Options.CurrentWindowResolution.X - HEALTHBAR_PADDING - HEALTHBAR_MAX, HEALTHBAR_PADDING + HEALTHBAR_HEIGHT + HEALTHBAR_PADDING));

			List<Buff.BuffInstance> buffs = player.GetBuffManager().GetBuffs();

			int index = 0;
			foreach (Buff.BuffInstance buff in buffs)
            {
				int x = index % 8;
				int y = index / 8;

				Texture2D texture = buff.buff.texture ?? Main.assetsManager.GetAsset<Texture2D>("ui_inventory");
				RectangleF sourceRect = buff.buff.sourceRect;

				Vector2 position = new Vector2(x * (SIZE + MARGIN), y * (SIZE + MARGIN));
				var button = UI.MakeButton(new UI.ButtonConstructionParameters(new RectangleF(position, new Size(SIZE)), texture, sourceRect));

				if (button.hovered)
                {
					UI.DisableParent();

					const int minW = 128;
					const int minH = 16;

					const int maxW = 256;

					string name = buff.buff.Name;
					string description = buff.buff.Description;

					float widthName = fi.StringWidth(name);
					Size sizeDescription = fi.StringSize(TextHelper.WrapText(fi, description, maxW));

					float textWidthMax = Math.Max(minW, Math.Max(widthName, sizeDescription.Width));

					float height = fi.StringHeight(name);
					height += sizeDescription.Height;
					height += 8;    //for padding

					RectangleF bounds = new RectangleF(Main.inputManager.GetMousePosition().ToVector2() + new Vector2(16), textWidthMax, Math.Max(height, minH));

					int overlapFarX = (int)bounds.x + (int)bounds.width - Options.CurrentWindowResolution.X;
					int overlapFarY = (int)bounds.y + (int)bounds.height - Options.CurrentWindowResolution.Y;

					if (overlapFarX > 0)
						bounds.x -= overlapFarX;
					if (overlapFarY > 0)
						bounds.y -= overlapFarY;

					UI.MakeLabel(name, fi, bounds.width, bounds.Position);
					bounds.y += fi.StringHeight(name);
					bounds.y += 8;
					UI.MakeLabel(description, fi, bounds.width, bounds.Position);
					UI.EnableParent();
                }

				index++;
            }

			UI.EndParent();

			if (opened)
			{
				if (Main.inputManager.JustPressed(Keys.Escape))
					Close();

				TextHelper.FontInfo fi = new TextHelper.FontInfo(Main.assetsManager.GetAsset<SpriteFont>("fira_mono_sml"), 1, true);

				UI.StartParent(new Vector2(MARGIN + MenuHelper.GetInventorySize(Player.INVENTORY_ROWS, Player.INVENTORY_COLUMNS, 18 * 2f, 2f).Width, MARGIN * 2f));

				Vector2 pos = new Vector2();
				RectangleF bounds = new RectangleF();

				pos = new Vector2(16 * 3.5f * SCALE, 0);

				UI.MakePanel(new Color(139, 139, 139), new RectangleF(0, 0, SIZE * 7, SIZE * 2 + MARGIN * 2));

				UI.StartParent(new Vector2(MARGIN));

				for (int y = 0; y < 2; y++)
				{
					for (int x = 0; x < 3; x++)
					{
						int i = y * 3 + x;

						pos = new Vector2(x * SIZE, y * SIZE);
						bounds = new RectangleF(pos, SIZE, SIZE);

						var itemslot = UI.MakeItemSlot(UI.MakeButton(new UI.ButtonConstructionParameters(bounds, Main.assetsManager.GetAsset<Texture2D>("ui_inventory"),
							new RectangleF(0, 0, 16, 16), new RectangleF(16, 0, 16, 16), new RectangleF(16, 0, 16, 16))),
							craftInventory.Get(i));


						var output = MenuHelper.ItemSlotClickOutput.None;
						if ((output = MenuHelper.HandleItemSlot(player, craftInventory, i, itemslot, ref held, new MenuHelper.WhiteListNone())) != MenuHelper.ItemSlotClickOutput.None)
						{
							if (output == MenuHelper.ItemSlotClickOutput.NeedsSwapInventory)
							{
								MenuHelper.SwapInventory(craftInventory, inventory, i);
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

				UI.MakeItemSlot(UI.MakeButton(new UI.ButtonConstructionParameters(bounds, Main.assetsManager.GetAsset<Texture2D>("ui_inventory"),
								new RectangleF(0, 0, 16, 16), new RectangleF(16, 0, 16, 16), new RectangleF(16, 0, 16, 16))),
								craftInventory.Get(6));

				bounds.x += SIZE;

				UI.MakeItemSlot(UI.MakeButton(new UI.ButtonConstructionParameters(bounds, Main.assetsManager.GetAsset<Texture2D>("ui_inventory"),
								new RectangleF(0, 0, 16, 16), new RectangleF(16, 0, 16, 16), new RectangleF(16, 0, 16, 16))),
								craftInventory.Get(7));

				bounds.y += SIZE;

				UI.Button craftRecipeButton = UI.MakeButton(new UI.ButtonConstructionParameters(bounds, Main.assetsManager.GetAsset<Texture2D>("ui_inventory"), 
					new RectangleF(0, 96, 16, 16), new RectangleF(16, 96, 16, 16), new RectangleF(16, 96, 16, 16)));
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

				UI.Button recipeBookButton = UI.MakeButton(new UI.ButtonConstructionParameters(bounds, Main.assetsManager.GetAsset<Texture2D>("ui_inventory"), 
					new RectangleF(0, 80, 16, 16), new RectangleF(16, 80, 16, 16), new RectangleF(16, 80, 16, 16)));

				if (recipeBookButton.hovered)
				{
					UI.DisableParent();
					UI.MakeLabel("Recipe Book", fi, 128, Main.inputManager.GetMousePosition().ToVector2() + new Vector2(16));
					UI.EnableParent();
				}
				
				if (recipeBookButton.clickLeft)
				{
					player.world.GameStateManager.GetCurrentGameState().PushMenu(new MenuRecipeBook(gsManager, Main.Registry.RecipeRegistry.PlayerInventoryCatalyst, new ItemInstance()));
				}

				UI.EndParent();

				UI.EndParent();

				UI.StartParent(new Vector2(MARGIN, 192));

				for (int i = 0; i < 6; i++)
				{
					pos = new Vector2(i * SIZE, 0);

					if (i >= 3)
						pos.X += MARGIN;

					bounds = new RectangleF(pos, SIZE, SIZE);

					var itemslot = UI.MakeItemSlot(UI.MakeButton(new UI.ButtonConstructionParameters(bounds, Main.assetsManager.GetAsset<Texture2D>("ui_inventory"),
									new RectangleF(0, 0, 16, 16), new RectangleF(16, 0, 16, 16), new RectangleF(16, 0, 16, 16))),
									accessoryInventory.Get(i), 1);

					if (!accessoryInventory.Get(i).valid)
						UI.MakeTexture(new RectangleF(pos, SIZE, SIZE), 
							Main.assetsManager.GetAsset<Texture2D>("ui_inventory"), new RectangleF(32 + 16 * i, 96, 16, 16));

					var output = MenuHelper.ItemSlotClickOutput.None;
					if ((output = MenuHelper.HandleItemSlot(player, accessoryInventory, i, itemslot, ref held,
						new MenuHelper.WhitelistAccessories(accessoryInventory, tagsAccessoriesBySlot[i]))) != MenuHelper.ItemSlotClickOutput.None)
					{
						if (output == MenuHelper.ItemSlotClickOutput.NeedsSwapInventory)
						{
							MenuHelper.SwapInventory(accessoryInventory, inventory, i);
						}
					}
				}

				pos.X = 0;
				pos.Y += SIZE + MARGIN;

				for (int i = 0; i < 3; i++)
                {
					pos.X = i * SIZE;

					bounds = new RectangleF(pos, SIZE, SIZE);

					var itemslot = UI.MakeItemSlot(UI.MakeButton(new UI.ButtonConstructionParameters(bounds, Main.assetsManager.GetAsset<Texture2D>("ui_inventory"),
									new RectangleF(0, 0, 16, 16), new RectangleF(16, 0, 16, 16), new RectangleF(16, 0, 16, 16))),
									gearInventory.Get(i), 1);

					if (!gearInventory.Get(i).valid)
						UI.MakeTexture(new RectangleF(pos, SIZE, SIZE),
							Main.assetsManager.GetAsset<Texture2D>("ui_inventory"), new RectangleF(48 + 16 * i, 112, 16, 16));

					var output = MenuHelper.ItemSlotClickOutput.None;
					if ((output = MenuHelper.HandleItemSlot(player, gearInventory, i, itemslot, ref held,
						new MenuHelper.WhitelistTag(tagsGearBySlot[i]))) != MenuHelper.ItemSlotClickOutput.None)
					{
						if (output == MenuHelper.ItemSlotClickOutput.NeedsSwapInventory)
						{
							MenuHelper.SwapInventory(gearInventory, inventory, i);
						}
					}
				}

				UI.EndParent();

				if (Main.Debug)
				{
					UI.StartParent(new Vector2(MARGIN, 256 + 64));

					var allItems = Main.Registry.ItemRegistry.GetIterable();

					for (int i = DEBUGItemListScrollRow * 8; i < allItems.Count; i++)
					{
						int x = i % 8;
						int y = i / 8;
						RectangleF b = new RectangleF(x * SIZE, (y - DEBUGItemListScrollRow) * SIZE , SIZE, SIZE);

						var cheatSlot = UI.MakeItemSlot(UI.MakeButton(new UI.ButtonConstructionParameters(b, Main.assetsManager.GetAsset<Texture2D>("ui_inventory"),
									new RectangleF(0, 0, 16, 16), new RectangleF(16, 0, 16, 16), new RectangleF(16, 0, 16, 16))),
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
				if (Main.inputManager.JustPressed(Keys.Escape))
					gsManager.GetCurrentGameState().PushMenu(new MenuPause(gsManager, player.world));

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

			float unit = (float)Options.CurrentWindowResolution.X / 80f;

			for (int i = pickedupItems.Length - 1; i >= 0; i--)
            {
				ref PickedUpItem pu = ref pickedupItems.Buffer[i];

				if (pu.timer >= PickedUpItem.DUR_TOTAL)
				{
					pickedupItems.Remove(pu);
					continue;
				}

				pu.timer += (float)deltaTime;

				if (pu.timer <= PickedUpItem.DUR_HOLDMID)
				{
					float p = pu.timer / PickedUpItem.DUR_HOLDMID;

					RectangleF rect = new RectangleF(-unit * 4, -unit * 4,
						unit * 8, unit * 8);
					rect = rect.Offset(Options.CurrentWindowResolution.ToVector2() / 2f);

					UI.MakeTexture(new UI.TextureConstructionParameters(rect, pu.item.item.Texture, pu.item.item.SourceRect, Color.White * p));
				}
				else if (pu.timer <= PickedUpItem.DUR_HOLDMID + PickedUpItem.DUR_ANIM_TO_SIDE)
                {
					float p = (pu.timer - PickedUpItem.DUR_HOLDMID) / PickedUpItem.DUR_ANIM_TO_SIDE;

					RectangleF rect = new RectangleF(
						MathHelper.Lerp(-unit * 4, -unit, p),
						MathHelper.Lerp(-unit * 4, -unit, p),
						MathHelper.Lerp(unit * 8, unit * 2, p),
						MathHelper.Lerp(unit * 8, unit * 2, p));
					rect = rect.Offset(MathHelper.Lerp(Options.CurrentWindowResolution.X / 2f,
						unit * 4f + unit, p), 
						MathHelper.Lerp(Options.CurrentWindowResolution.Y / 2f, 
							Options.CurrentWindowResolution.Y / 2f + (pu.currentIndex * (unit * 2 + (unit / 2f))), p));

					UI.MakeTexture(new UI.TextureConstructionParameters(rect, pu.item.item.Texture, pu.item.item.SourceRect));
				}
				else if (pu.timer <= PickedUpItem.DUR_HOLDMID + PickedUpItem.DUR_ANIM_TO_SIDE + PickedUpItem.DUR_HOLDSIDE)
                {
					RectangleF rect = new RectangleF(-unit, -unit, unit * 2, unit * 2);

					rect = rect.Offset(unit * 4f + unit, Options.CurrentWindowResolution.Y / 2f + (pu.currentIndex * (unit * 2 + (unit / 2f))));

					UI.MakeTexture(new UI.TextureConstructionParameters(rect, pu.item.item.Texture, pu.item.item.SourceRect));
					UI.MakeLabel(new UI.LabelConstructionParameters(
						string.Format("x{0} {1}", pu.item.num, pu.item.item.GetName(pu.item)), fi, 128, 
						rect.Position + new Vector2(unit, 0), Color.White));
				}
				else if (pu.timer <= PickedUpItem.DUR_TOTAL)
                {
					float min = PickedUpItem.DUR_HOLDMID + PickedUpItem.DUR_ANIM_TO_SIDE + PickedUpItem.DUR_HOLDSIDE;
					float max = PickedUpItem.DUR_TOTAL - min;
					float p = (pu.timer - min) / max;

					RectangleF rect = new RectangleF(-unit, -unit, unit * 2, unit * 2);

					rect = rect.Offset(unit * 4f + unit, Options.CurrentWindowResolution.Y / 2f + (pu.currentIndex * (unit * 2 + (unit / 2f))));

					UI.MakeTexture(new UI.TextureConstructionParameters(rect, pu.item.item.Texture, pu.item.item.SourceRect, color: Color.White * (1 - p)));
					UI.MakeLabel(new UI.LabelConstructionParameters(
						string.Format("x{0} {1}", pu.item.num, pu.item.item.GetName(pu.item)), fi, 128,
						rect.Position + new Vector2(unit, 0), Color.White * (1 - p)));
				}
            }
		}

		public void AddPickedUpItem(ItemInstance item)
        {
			if (pickedupItems.Length > 8)
				pickedupItems.RemoveAt(0);

			bool found = false;
			for (int i = 0; i < Math.Min(pickedupItems.Length, 8); i++)
            {
				if (pickedupItems[i].item.item == item.item)
                {
					pickedupItems.Buffer[i].timer = PickedUpItem.DUR_HOLDMID + PickedUpItem.DUR_ANIM_TO_SIDE;
					pickedupItems.Buffer[i].item = new ItemInstance(pickedupItems[i].item, pickedupItems[i].item.num + item.num);
					found = true;
					break;
                }
            }

			if (!found)
				pickedupItems.Add(new PickedUpItem(item, pickedupItems.Length));
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

							craftInventory.FindExact(recipe.Layout[i], 6, out int index);

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

			if (opened)
			{
				MenuHelper.DrawHeldItem(batch, held, SIZE, SCALE);
			}

			Vector2 healthBarPos = new Vector2(Options.CurrentWindowResolution.X - HEALTHBAR_PADDING, HEALTHBAR_PADDING);
			batch.Draw(DrawHelper.WhitePixel, healthBarPos, null, Color.Gray, 0, Vector2.Zero, new Vector2(-WIDTH_PER_HEALTH * player.GetRealMaxHealth(), HEALTHBAR_HEIGHT), SpriteEffects.None, 0);
			batch.Draw(DrawHelper.WhitePixel, healthBarPos, null, Color.Red, 0, Vector2.Zero, new Vector2(-WIDTH_PER_HEALTH * player.Health, HEALTHBAR_HEIGHT), SpriteEffects.None, 0.1f);

			healthBarPos.Y += HEALTHBAR_HEIGHT + HEALTHBAR_PADDING;
			batch.Draw(DrawHelper.WhitePixel, healthBarPos, null, Color.Gray, 0, Vector2.Zero, new Vector2(-WIDTH_PER_MAGIC * player.MaxMagic, HEALTHBAR_HEIGHT), SpriteEffects.None, 0);
			batch.Draw(DrawHelper.WhitePixel, healthBarPos, null, Color.Blue, 0, Vector2.Zero, new Vector2(-WIDTH_PER_MAGIC * player.Magic, HEALTHBAR_HEIGHT), SpriteEffects.None, 0.1f);
		}
	}
}
