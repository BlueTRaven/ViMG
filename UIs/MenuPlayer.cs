using BrNineSlice;
using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
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
			public const float DUR_ANIM_TO_SIDE = 0.6f;
			public const float DUR_HOLDSIDE = 4f;
			public const float DUR_TOTAL = 6.2f;

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

		private const int HEALTHBAR_SCALE = 4;
        private const float HEALTHBAR_PADDING = 8 * HEALTHBAR_SCALE;
		private const float HEALTHBAR_MAX = 256;
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

		private static string[][] tagsGearBySlot = new string[4][]
		{
			new string[1] { "gear_heart" },
			new string[1] { "gear_run" },
			new string[1] { "gear_magic" },
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

		private int DEBUGItemListPage = 0;

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

		private UI.ItemSlot[] inventoryItemSlots = new UI.ItemSlot[Player.INVENTORY_ROWS * Player.INVENTORY_COLUMNS];
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

			MenuHelper.DoPlayerInventory(player, inventory, ref held, (opened ? Player.INVENTORY_ROWS : 1), Player.INVENTORY_COLUMNS, 18 * 2f, 2f, inventoryItemSlots);

			int s = Player.INVENTORY_COLUMNS;
			if (opened)
				s *= Player.INVENTORY_ROWS;

			HoverIndex = -1;
			for (int i = 0; i < s; i++)
			{
				if (inventoryItemSlots[i].button.hovered)
					HoverIndex = i;
			}

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
					UIWidgets.MakeTooltip(position, string.Format("{0} x{1} - {2:0.00}s", buff.buff.Name, buff.stack, buff.duration), buff.buff.Description);

					/*UI.DisableParent();

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
					UI.EnableParent();*/
                }

				index++;
            }

			UI.EndParent();

			if (opened)
			{
				if (Main.inputManager.JustPressed(Keys.Escape))
					Close();

				TextHelper.FontInfo fi = new TextHelper.FontInfo(Main.assetsManager.GetAsset<SpriteFont>("fira_mono_sml"), 1, true);

				UI.StartParent(new Vector2(MARGIN * 2f + MenuHelper.GetInventorySize(Player.INVENTORY_ROWS, Player.INVENTORY_COLUMNS, 18 * 2f, 2f).Width, MARGIN * 2f));

                Vector2 pos = new Vector2();
				RectangleF bounds = new RectangleF();

				pos = new Vector2(16 * 3.5f * SCALE, 0);

				UI.MakeTexture(new UI.TextureConstructionParameters(new RectangleF(0, 16, 123 * SCALE, 55 * SCALE), 
					Main.assetsManager.GetAsset<Texture2D>("ui_inventory"),
					new RectangleF(128, 0, 123, 55), below: true));
				//UI.MakePanel(new Color(139, 139, 139), new RectangleF(0, 0, SIZE * 7, SIZE * 2 + MARGIN * 2));

				UI.StartParent(new Vector2(MARGIN, 18 + MARGIN));

				UI.ButtonConstructionParameters buttonParameters = MenuHelper.ButtonParameters;
				UI.ButtonConstructionParameters actionButtonParameters = MenuHelper.ActionButtonParameters;
				for (int y = 0; y < 2; y++)
				{
					for (int x = 0; x < 3; x++)
					{
						int i = y * 3 + x;

						pos = new Vector2(x * 18 * 2 + x * 2f, y * 18 * 2 + y * 2f);
						bounds = new RectangleF(pos, 18 * 2, 18 * 2);

						buttonParameters.bounds = bounds;

						var itemslot = UI.MakeItemSlot(UI.MakeButton(buttonParameters), craftInventory.Get(i));

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

				bounds = new RectangleF(16 * 2 * 4.125f, 2, SIZE, SIZE);

				UI.MakeTexture(bounds, Main.assetsManager.GetAsset<Texture2D>("ui_inventory"), new RectangleF(32, 48, 16, 16));

				bounds.x += 29 * 2f;
				bounds.y -= 2;

				buttonParameters.bounds.Position = bounds.Position;
				UI.MakeItemSlot(UI.MakeButton(buttonParameters), craftInventory.Get(6));

				//bounds.x += SIZE;
                //buttonParameters.bounds.Position = bounds.Position;

                /*UI.MakeItemSlot(UI.MakeButton(new UI.ButtonConstructionParameters(bounds, Main.assetsManager.GetAsset<Texture2D>("ui_inventory"),
								new RectangleF(0, 0, 16, 16), new RectangleF(16, 0, 16, 16), new RectangleF(16, 0, 16, 16))),
								craftInventory.Get(7));*/

				bounds.y += 24 * SCALE;
                actionButtonParameters.bounds.Position = bounds.Position;

                UI.Button craftRecipeButton = UI.MakeButton(actionButtonParameters);
				UI.MakeTexture(new UI.TextureConstructionParameters(new RectangleF(bounds.x + 1, bounds.y + 1, 16 * SCALE, 16 * SCALE),
					Main.assetsManager.GetAsset<Texture2D>("ui_inventory"), new RectangleF(0, 96, 16, 16)));

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

				UI.StartParent(new Vector2(0, 2 * SIZE + MARGIN_CRAFTING * 2));
				UI.Button recipeBookButton = UI.MakeButton(new UI.ButtonConstructionParameters(new RectangleF(0, 0, 16, 16), Main.assetsManager.GetAsset<Texture2D>("ui_inventory"), 
					new RectangleF(0, 80, 16, 16), new RectangleF(16, 80, 16, 16), new RectangleF(16, 80, 16, 16)));
				UI.EndParent();

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

				UI.StartParent(new Vector2(MARGIN,
					MenuHelper.GetInventorySize(Player.INVENTORY_ROWS, Player.INVENTORY_COLUMNS, 18 * SCALE, 2f).Height + MARGIN * 4));

				Vector2 accessoryInventorySize = MenuHelper.GetInventorySize(1, 3, 18 * SCALE, 2f).ToVector2() * 
					new Vector2(2, 2) + new Vector2(MARGIN, MARGIN);

                UI.MakePanel(Color.White, new RectangleF(0, 0, accessoryInventorySize), MenuHelper.MainPanelNS);

				UI.StartParent(new Vector2(16, 16));

				buttonParameters.bounds.Position = Vector2.Zero;
				for (int i = 0; i < 3; i++)
				{
					UI.StartParent(new Vector2(i * 18 * SCALE + i * 2f, 0));

					var itemslot = UI.MakeItemSlot(UI.MakeButton(buttonParameters), accessoryInventory.Get(i), 1);

					if (!accessoryInventory.Get(i).valid)
						UI.MakeTexture(new RectangleF(Vector2.Zero, SIZE, SIZE), 
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

					UI.EndParent();
				}

                UI.EndParent();

                UI.StartParent(new Vector2(MenuHelper.GetInventorySize(1, 3, 18 * 2f, 2f).Width + MARGIN, 0));

                //UI.MakePanel(Color.White, new RectangleF(0, 0, MenuHelper.GetInventorySize(1, 3, 18 * 2f, 2f)), MenuHelper.MainPanelNS);

				UI.StartParent(new Vector2(16));

                buttonParameters.bounds.Position = Vector2.Zero;
                for (int i = 0; i < 3; i++)
                {
					UI.StartParent(new Vector2(i * 18 * SCALE + i * 2f, 0));

                    var itemslot = UI.MakeItemSlot(UI.MakeButton(buttonParameters), accessoryInventory.Get(i + 3), 1);

                    if (!accessoryInventory.Get(i + 3).valid)
                        UI.MakeTexture(new RectangleF(Vector2.Zero, SIZE, SIZE),
                            Main.assetsManager.GetAsset<Texture2D>("ui_inventory"), new RectangleF(32 + 16 * (i + 3), 96, 16, 16));

                    var output = MenuHelper.ItemSlotClickOutput.None;
                    if ((output = MenuHelper.HandleItemSlot(player, accessoryInventory, i + 3, itemslot, ref held,
                        new MenuHelper.WhitelistAccessories(accessoryInventory, tagsAccessoriesBySlot[i + 3]))) != MenuHelper.ItemSlotClickOutput.None)
                    {
                        if (output == MenuHelper.ItemSlotClickOutput.NeedsSwapInventory)
                        {
                            MenuHelper.SwapInventory(accessoryInventory, inventory, i + 3);
                        }
                    }

					UI.EndParent();
                }

				UI.EndParent();
                UI.EndParent();

				UI.StartParent(new Vector2(0, MenuHelper.GetInventorySize(1, 3, 18 * 2f, 2f).Height + MARGIN));

                //UI.MakePanel(Color.White, new RectangleF(0, 0, MenuHelper.GetInventorySize(1, 3, 18 * 2f, 2f)), MenuHelper.MainPanelNS);

				UI.StartParent(new Vector2(16));

				buttonParameters.bounds.Position = Vector2.Zero;

				for (int i = 0; i < 4; i++)
				{
					UI.StartParent(new Vector2(i * 18 * SCALE + i * 2, 0));
					//pos = new Vector2(i * 18 * SCALE, 0);

					//bounds = new RectangleF(pos, SIZE, SIZE);

					//buttonParameters.bounds.Position = pos;

					var itemslot = UI.MakeItemSlot(UI.MakeButton(buttonParameters), gearInventory.Get(i), 1);

					if (!gearInventory.Get(i).valid)
						UI.MakeTexture(new RectangleF(Vector2.Zero, SIZE, SIZE),
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

					UI.EndParent();
				}

				UI.EndParent();
                UI.EndParent();

                UI.StartParent(MenuHelper.GetInventorySize(1, 3, 18 * 2f, 2f).ToVector2() + new Vector2(MARGIN));

				UIWidgets.MakeCoinCounter(Vector2.Zero, player.Currency, SCALE, fi);

				UI.EndParent();

                UI.EndParent();

                pos = new Vector2(3 * 18 * SCALE + MARGIN, 0);

                pos.X = 0;
				pos.Y += SIZE + MARGIN;

				if (Main.Debug)
				{
					UI.StartParent(new Vector2(MARGIN, 256 + 128));

					var allItems = Main.Registry.ItemRegistry.GetIterable();

					for (int i = 0; i < 8 * 8; i++)
					{
						int offsetI = (DEBUGItemListPage * (8 * 8)) + i;

						if (offsetI > allItems.Count - 1) 
							offsetI = allItems.Count - 1;

						int x = i % 8;
						int y = i / 8;

						UI.StartParent(new Vector2(x * 18, y * 18) * SCALE);

                        var cheatSlot = UI.MakeItemSlot(UI.MakeButton(buttonParameters), new ItemInstance(allItems[offsetI], 999, 1));

                        if (cheatSlot.button.clickLeft)
                        {
                            inventory.Add(new ItemInstance(allItems[offsetI], 999, 1));
                        }
                        else if (cheatSlot.button.clickRight)
                        {
                            inventory.Add(new ItemInstance(allItems[offsetI], 1, 1));
                        }

                        if (cheatSlot.button.hovered)
                        {
                            int scroll = -Main.inputManager.GetMouseScroll();

                            if (Main.inputManager.JustPressed(Keys.PageUp))
                                scroll--;
                            else if (Main.inputManager.JustPressed(Keys.PageDown))
                                scroll++;

                            int maxScroll = (int)Math.Round(allItems.Count / (8.0 * 8.0), MidpointRounding.AwayFromZero);

                            if (scroll != 0)
                            {
                                DEBUGItemListPage += scroll;

                                DEBUGItemListPage = Math.Clamp(DEBUGItemListPage, 0, maxScroll);
                            }
                        }

                        UI.EndParent();
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

            Vector2 hbPos = new Vector2(Options.CurrentWindowResolution.X - HEALTHBAR_PADDING -
                WIDTH_PER_HEALTH * player.GetCalculatedMaxHealth(), HEALTHBAR_PADDING);
            RectangleF hbRect = new RectangleF(hbPos,
                new Vector2(WIDTH_PER_HEALTH * player.GetCalculatedMaxHealth(), 8 * HEALTHBAR_SCALE));

			if (hbRect.Contains(Main.inputManager.GetMousePosition().ToVector2()))
			{
				UIWidgets.MakeTooltip(hbPos, "Health", string.Format("{0}/{1}\n" +
					"Your health. If this is reduced to zero, you die. So don't let that happen.", player.Health, player.GetCalculatedMaxHealth()));
			}

            Vector2 mbPos = new Vector2(Options.CurrentWindowResolution.X - HEALTHBAR_PADDING -
                WIDTH_PER_MAGIC * player.GetCalculatedMaxMagic(), HEALTHBAR_PADDING + HEALTHBAR_HEIGHT + HEALTHBAR_PADDING);
            RectangleF mbRect = new RectangleF(mbPos,
                new Vector2(WIDTH_PER_MAGIC * player.GetCalculatedMaxMagic(), 8 * HEALTHBAR_SCALE));

            if (mbRect.Contains(Main.inputManager.GetMousePosition().ToVector2()))
            {
                UIWidgets.MakeTooltip(mbPos, "Magic", string.Format("{0}/{1}\n" +
                    "Your magic. Used to cast magical spells.", player.Magic, player.GetCalculatedMaxMagic()));
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
					p = Easings.EaseInOutElastic(p);

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

		private NineSlice healthbarLowerNS = new NineSlice(Main.assetsManager.GetAsset<Texture2D>("bars"), new RectangleF(48, 48, 24, 8), 8, 8, 2, 2);
        private NineSlice healthbarUpperNS = new NineSlice(Main.assetsManager.GetAsset<Texture2D>("bars"), new RectangleF(120, 32, 24, 8), 7, 7, 1, 1);
        private NineSlice magicbarLowerNS = new NineSlice(Main.assetsManager.GetAsset<Texture2D>("bars"), new RectangleF(48, 264, 24, 8), 8, 8, 2, 2);
        private NineSlice magicbarUpperNS = new NineSlice(Main.assetsManager.GetAsset<Texture2D>("bars"), new RectangleF(120, 248, 24, 8), 7, 7, 1, 1);

        public override void Draw(SpriteBatch batch)
		{
			base.Draw(batch);

			UI.Draw(batch, SCALE);

			if (opened)
			{
				MenuHelper.DrawHeldItem(batch, held, SIZE, SCALE);
			}

			Vector2 hbPos = new Vector2(Options.CurrentWindowResolution.X - HEALTHBAR_PADDING - 
				WIDTH_PER_HEALTH * player.GetCalculatedMaxHealth(), HEALTHBAR_PADDING);
			RectangleF hbRect = new RectangleF(hbPos,
                new Vector2(WIDTH_PER_HEALTH * player.GetCalculatedMaxHealth(), 8 * HEALTHBAR_SCALE));

			float health = player.Health;
			float lostHealth = player.GetCalculatedMaxHealth() - player.Health;

            healthbarLowerNS.Draw(batch, Color.White, hbRect, HEALTHBAR_SCALE, 0);

			if (WIDTH_PER_HEALTH * health > (healthbarUpperNS.distLeft + healthbarUpperNS.distRight) * HEALTHBAR_SCALE)
			{
				healthbarUpperNS.Draw(batch, Color.White, new RectangleF(hbPos +
					new Vector2(WIDTH_PER_HEALTH * lostHealth, 0),
					new Vector2(WIDTH_PER_HEALTH * health, 8 * HEALTHBAR_SCALE)), HEALTHBAR_SCALE, 0);
			}

            Vector2 mbPos = new Vector2(Options.CurrentWindowResolution.X - HEALTHBAR_PADDING -
                WIDTH_PER_MAGIC * player.GetCalculatedMaxMagic(), HEALTHBAR_PADDING + HEALTHBAR_HEIGHT + HEALTHBAR_PADDING);
            RectangleF mbRect = new RectangleF(mbPos,
                new Vector2(WIDTH_PER_MAGIC * player.GetCalculatedMaxMagic(), 8 * HEALTHBAR_SCALE));

            float magic = player.Magic;
			float lostMagic = player.GetCalculatedMaxMagic() - player.Magic;

			magicbarLowerNS.Draw(batch, Color.White, mbRect, HEALTHBAR_SCALE, 0);
		
			//Since this is done using a nineslice, we get problems if the left and right segments begin to overlap
			//(which occurs when the width < the dist of both sides).
			//To rememdy this we pretty much have to draw manual versions of these textures at smaller sizes.
			//TODO draw smaller sized versions of bars when slices overlap
			if (WIDTH_PER_MAGIC * magic > (magicbarUpperNS.distRight + magicbarUpperNS.distLeft) * HEALTHBAR_SCALE)
			{
				magicbarUpperNS.Draw(batch, Color.White, new RectangleF(mbPos +
					new Vector2(WIDTH_PER_MAGIC * lostMagic, 0),
					new Vector2(WIDTH_PER_MAGIC * magic, 8 * HEALTHBAR_SCALE)), HEALTHBAR_SCALE, 0);
			}
		}
	}
}
