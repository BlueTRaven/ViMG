using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Items;

namespace ViMG.UIs
{
	public static class MenuHelper
	{
		public enum ItemSlotClickOutput
		{
			None,
			PickupFromSlot,
			PlaceInSlotAll,
			PlaceInSlotSome,
			MergeInSlotCompletely,
			MergeInSlotSome,
			MergeFromSlot,
			Swap,
			FilterRecipe,	// Pull up UIRecipeBook with item as filter
			NeedsSwapInventory,	// The item must be swapped from one inventory to another; call SwapInventory(a, b)
		}

		public interface IWhiteList
		{
			public bool Matches(Item item);
		}

		public readonly struct WhiteListName : IWhiteList
		{
			private readonly List<string> names;

			public WhiteListName(List<string> names)
			{
				this.names = names;
			}

			public bool Matches(Item item)
			{
				return names.Contains(item.Identifier);
			}
		}

		public readonly struct WhiteListOneName : IWhiteList
		{
			private readonly string name;

			public WhiteListOneName(string name)
			{
				this.name = name;
			}

			public bool Matches(Item item)
			{
				return item.Identifier == name;
			}
		}

		public readonly struct WhiteListNone : IWhiteList
		{
			public bool Matches(Item item)
			{
				return true;
			}
		}

        public readonly struct WhitelistTag : IWhiteList
        {
			private readonly string[] tags;
			public WhitelistTag(string[] tags)
            {
				this.tags = tags;
            }

            public bool Matches(Item item)
            {
				foreach (string tag in tags)
                {
					if (item.Tags.Contains(tag))
						return true;
                }

				return false;
            }
        }

		//To accept an item with this whitelist, it must be:
		//A. an item with the any of the given tags
		//B. not already contained in the inventory.
        public readonly struct WhitelistAccessories : IWhiteList
        {
			private readonly Inventory inventory;
			private readonly string[] tags;

			public WhitelistAccessories(Inventory inventory, string[] tags)
            {
				this.inventory = inventory;
				this.tags = tags;
            }

            public bool Matches(Item item)
            {
				for (int i = 0; i < inventory.NumSlots; i++)
				{
					if (inventory.Get(i).valid && inventory.Get(i).item.Id == item.Id)
						return false;
				}

				foreach (string tag in tags)
				{
					if (item.Tags.Contains(tag))
						return true;
				}

				return false;
            }
        }

        public static void DoPlayerInventory(Player player, Inventory inventory, ref Items.ItemInstance held, int rows = 4, int columns = 8, float size = 16, float padding = 8)
		{
			for (int y = 0; y < rows; y++)
			{
				for (int x = 0; x < columns; x++)
				{
					int i = y * columns + x;

					Vector2 pos = new Vector2(x * size + x * padding, y * size + y * padding);
					RectangleF bounds = new RectangleF(pos, size, size);

					var itemslot = UI.MakeItemSlot(UI.MakeButton(bounds, Main.assetsManager.GetAsset<Texture2D>("ui_inventory"),
									new RectangleF(0, 0, 16, 16), new RectangleF(16, 0, 16, 16), new RectangleF(16, 0, 16, 16)),
									inventory.Get(i));
					var oldItem = inventory.Get(i);

					var output = HandleItemSlot(player, inventory, i, itemslot, ref held, new MenuHelper.WhiteListNone());

					if (output == ItemSlotClickOutput.NeedsSwapInventory)
					{
						ref readonly var item = ref inventory.Get(i);

						int first = inventory.FirstEmpty();

						if (first != -1)
						{
							inventory.Set(item, first);
							inventory.Remove(i, item.num);
						}
					}
				}
			}
		}

		public static ItemSlotClickOutput HandleItemSlot(Player player, Inventory inventory, int index, in UI.ItemSlot itemSlot)
		{
			ItemSlotClickOutput output = ItemSlotClickOutput.None;

			if (itemSlot.button.hovered && Main.inputManager.JustPressed(Keys.R))
			{
				player.world.GameStateManager.GetCurrentGameState().PushMenu(new MenuRecipeBook(null, inventory.Get(index)));

				return ItemSlotClickOutput.FilterRecipe;
			}

			return output;
		}

		public static ItemSlotClickOutput HandleItemSlot<TWhiteList>(Player player, Inventory inventory, int index, in UI.ItemSlot itemSlot, ref ItemInstance held, TWhiteList whiteList) 
			where TWhiteList : struct, IWhiteList
		{
			ItemSlotClickOutput output = ItemSlotClickOutput.None;

			if (itemSlot.button.hovered && Main.inputManager.JustPressed(Keys.R))
			{
				player.world.GameStateManager.GetCurrentGameState().PushMenu(new MenuRecipeBook(null, inventory.Get(index)));

				return ItemSlotClickOutput.FilterRecipe;
			}

			if (itemSlot.button.clickLeft)
			{
				if (Main.inputManager.IsHeld(Keys.LeftShift) && itemSlot.item.valid)
				{
					//defer this functionality to the user since we might need to swap between different inventories and this function can only see one inventory.
					output = ItemSlotClickOutput.NeedsSwapInventory;
				}
				else if (!held.valid && itemSlot.item.valid)
				{
					//Pick up the item - put it in the held item instance
					held = itemSlot.item.Copy();
					inventory.Remove(index, held.num);

					output = ItemSlotClickOutput.PickupFromSlot;

					held.item.StartHold(player, inventory, -1);
				}
				else if (held.valid && itemSlot.item.valid)
				{
					// Merge stacks
					if (inventory.Get(index).item == held.item && held.damage == itemSlot.item.damage)
					{
						int total = itemSlot.item.num + held.num;

						if (total <= itemSlot.maxStackSize || itemSlot.maxStackSize == -1)
						{
							inventory.Set(new ItemInstance(held, itemSlot.item.num + held.num), index);
							held = new ItemInstance();

							output = ItemSlotClickOutput.MergeInSlotCompletely;
						}
						else
						{
							int rem = held.num - (itemSlot.maxStackSize - itemSlot.item.num);

							inventory.Set(new ItemInstance(itemSlot.item, itemSlot.maxStackSize), index);
							held = new ItemInstance(held, rem);

							output = ItemSlotClickOutput.MergeInSlotSome;
						}
					}
					else
					{
						// attempt to swap stacks
						if (itemSlot.maxStackSize == -1 || held.num <= itemSlot.maxStackSize)
						{
							if (!whiteList.Matches(held.item))
								return ItemSlotClickOutput.None;

							var oldHeld = held;
							held = itemSlot.item.Copy();
							inventory.Set(oldHeld, index);

							output = ItemSlotClickOutput.Swap;

							oldHeld.item.EndHold(player, inventory, -1);
							held.item.StartHold(player, inventory, -1);
						}
						//Cannot swap stacks if doing so would put us above the max stack size. Swapping would have to involve actively removing or dropping items.
						else output = ItemSlotClickOutput.None;
					}
				}
				else if (held.valid && !itemSlot.item.valid)
				{
					// Place in slot. The held item is set to an empty item instance.

					int total = held.num;

					if (total <= itemSlot.maxStackSize || itemSlot.maxStackSize == -1)
					{
						if (!whiteList.Matches(held.item))
							return ItemSlotClickOutput.None;

						held.item.EndHold(player, inventory, index);

						inventory.Set(held, index);
						held = new ItemInstance();

						output = ItemSlotClickOutput.PlaceInSlotAll;
					}
                    else
                    {
						int rem = held.num - itemSlot.maxStackSize;

						inventory.Set(new ItemInstance(held, itemSlot.maxStackSize), index);
						held = new ItemInstance(held, rem);

						output = ItemSlotClickOutput.PlaceInSlotSome;
					}
				}
			}
			else if (itemSlot.button.clickRight)
			{
				if (!held.valid && itemSlot.item.valid)
				{
					//Pick up the item - put it in the held item instance
					held = new ItemInstance(itemSlot.item, 1);
					inventory.Remove(index, 1);

					output = ItemSlotClickOutput.PickupFromSlot;
				}
				else if (held.valid && itemSlot.item.valid)
				{
					// Merge stacks
					if (inventory.Get(index).item == held.item && held.damage == itemSlot.item.damage)
					{
						if (itemSlot.item.num != itemSlot.maxStackSize)
						{
							inventory.Set(new ItemInstance(held, itemSlot.item.num + 1), index);

							if (held.num - 1 > 0)
								held = new ItemInstance(held, held.num - 1);
							else held = new ItemInstance();

							output = ItemSlotClickOutput.MergeInSlotSome;
						}
						else output = ItemSlotClickOutput.None;
					}
					else
					{
						// Don't do anything on the right click case. We can't swap.
					}
				}
				else if (held.valid && !itemSlot.item.valid)
				{
					// Place in slot. The held item is set to an empty item instance.

					if (!whiteList.Matches(held.item))
						return ItemSlotClickOutput.None;

					inventory.Set(new ItemInstance(held, 1), index);
					if (held.num - 1 > 0)
						held = new ItemInstance(held, held.num - 1);
					else held = new ItemInstance();

					output = ItemSlotClickOutput.PlaceInSlotAll;
				}
			}

			return output;
		}

		public static void SwapInventory(Inventory a, Inventory b, int indexA)
		{
			ref readonly ItemInstance item = ref a.Get(indexA);

			if (item.valid)
			{
				b.Add(item);
				a.Remove(indexA, item.num);
			}
		}

		public static bool RequireNumItemSlots(Inventory inventory, int num)
		{
			if (inventory.NumSlots != num)
			{
				throw new Exception("Inventory " + inventory + " is required to have " + num + " item slots.");
			}
			return true;
		}

		public static void DrawHeldItem(SpriteBatch batch, in ItemInstance held, float size, float scale)
		{
			if (held.valid)
			{
				var fi = new TextHelper.FontInfo(Main.assetsManager.GetAsset<SpriteFont>("fira_mono_sml"), 1, true, Color.Black);

				Vector2 pos = Main.inputManager.GetMousePosition().ToVector2();
				RectangleF bounds = new RectangleF(pos, size, size);

				held.item.DrawInInventory(batch, held, pos, scale);
				//batch.Draw(held.item.Texture, pos, held.item.SourceRect.ToRectangle(), Color.White, 0, Vector2.Zero, scale, SpriteEffects.None, 0.99f);

				TextHelper.DrawText(batch, fi,
					held.num.ToString(), Color.White, bounds.ToRectangle(), Enums.Alignment.BottomRight, (int)bounds.width, 1, TextHelper.OverFlowAction.None);
			}
		}
	}
}
