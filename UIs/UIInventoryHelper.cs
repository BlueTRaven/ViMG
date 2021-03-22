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
	public static class UIInventoryHelper
	{
		public enum ItemSlotClickOutput
		{
			None,
			PickupFromSlot,
			PlaceInSlot,
			MergeInSlot,
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

					var output = HandleItemSlot(player, inventory, i, itemslot, ref held, new UIInventoryHelper.WhiteListNone());

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
				player.OpenUI(new UIRecipeBook(null, inventory.Get(index)));

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
				player.OpenUI(new UIRecipeBook(null, inventory.Get(index)));

				return ItemSlotClickOutput.FilterRecipe;
			}

			if (itemSlot.button.clickLeft)
			{
				if (Main.inputManager.IsHeld(Microsoft.Xna.Framework.Input.Keys.LeftShift) && itemSlot.item.valid)
				{
					output = ItemSlotClickOutput.NeedsSwapInventory;
				}
				else if (!held.valid && itemSlot.item.valid)
				{
					//Pick up the item - put it in the held item instance
					held = itemSlot.item.Copy();
					inventory.Remove(index, held.num);

					output = ItemSlotClickOutput.PickupFromSlot;
				}
				else if (held.valid && itemSlot.item.valid)
				{
					// Merge stacks
					if (inventory.Get(index).item == held.item && held.damage == itemSlot.item.damage)
					{
						inventory.Set(new ItemInstance(held, itemSlot.item.num + held.num), index);
						held = new ItemInstance();

						output = ItemSlotClickOutput.MergeInSlot;
					}
					else
					{
						// Swap stacks

						if (!whiteList.Matches(held.item))
							return ItemSlotClickOutput.None;

						var oldHeld = held;
						held = itemSlot.item.Copy();
						inventory.Set(oldHeld, index);

						output = ItemSlotClickOutput.Swap;
					}
				}
				else if (held.valid && !itemSlot.item.valid)
				{
					// Place in slot. The held item is set to an empty item instance.

					if (!whiteList.Matches(held.item))
						return ItemSlotClickOutput.None;

					inventory.Set(held, index);
					held = new ItemInstance();

					output = ItemSlotClickOutput.PlaceInSlot;
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
						inventory.Set(new ItemInstance(held, itemSlot.item.num + 1), index);

						if (held.num - 1 > 0)
							held = new ItemInstance(held, held.num - 1);
						else held = new ItemInstance();

						output = ItemSlotClickOutput.MergeInSlot;
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

					output = ItemSlotClickOutput.PlaceInSlot;
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
