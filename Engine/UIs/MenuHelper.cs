using BepuPhysics.Constraints;
using BrNineSlice;
using BrUtility;
using Engine.Items;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SharpDX.DXGI;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG.GameStates;
using ViMG.Items;
using static ViMG.UIs.MenuHelper;
using static ViMG.UIs.UI;

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
			public bool Matches(Item? item);
		}

		public readonly struct WhiteListName : IWhiteList
		{
			private readonly List<string> names;

			public WhiteListName(List<string> names)
			{
				this.names = names;
			}

			public bool Matches(Item? item)
			{
				return names.Contains(item?.Identifier ?? "");
			}
		}

		public readonly struct WhiteListOneName : IWhiteList
		{
			private readonly string name;

			public WhiteListOneName(string name)
			{
				this.name = name;
			}

			public bool Matches(Item? item)
			{
				return item?.Identifier == name;
			}
		}

		public readonly struct WhiteListNone : IWhiteList
		{
			public bool Matches(Item? item)
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

            public bool Matches(Item? item)
            {
				foreach (string tag in tags)
                {
					if (item?.Tags.Contains(tag) ?? false)
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

		public static UI.ButtonConstructionParameters ButtonParameters = new UI.ButtonConstructionParameters(
			new RectangleF(0, 0, 18, 18), Main.assetsManager.GetAsset<Texture2D>("ui_inventory"), 
			new RectangleF(92, 0, 18, 18), new RectangleF(110, 0, 18, 18), new RectangleF(110, 0, 18, 18));
        public static UI.ButtonConstructionParameters ActionButtonParameters = new UI.ButtonConstructionParameters(
			new RectangleF(Vector2.Zero, 18 * 2, 18 * 2), Main.assetsManager.GetAsset<Texture2D>("ui_inventory"),
            new RectangleF(92, 18, 18, 18), new RectangleF(110, 18, 18, 18), new RectangleF(110, 18, 18, 18));

        public static NineSlice MainPanelNS = new NineSlice(Main.assetsManager.GetAsset<Texture2D>("ui_inventory"), new RectangleF(192, 64, 64, 64), 16);
        public static NineSlice SecondaryPanelNS = new NineSlice(Main.assetsManager.GetAsset<Texture2D>("ui_inventory"), new RectangleF(256, 64, 64, 64), 16);

        public static void DoPlayerInventory(Player player, Inventory inventory, Inventory heldInventory, 
			int rows = 4, int columns = 8, float size = 16, float padding = 8, UI.ItemSlot[] itemSlots = null)
		{
			UI.MakePanel(Color.White, new RectangleF(0, 0, GetInventorySize(rows, columns, size, padding)), MainPanelNS);

			UI.StartParent(new Vector2(16));

			UI.ButtonConstructionParameters buttonParameters = ButtonParameters;
			buttonParameters.bounds.Size = new Size(size);

            for (int y = 0; y < rows; y++)
			{
				for (int x = 0; x < columns; x++)
				{
					int i = y * columns + x;

					UI.StartParent(new Vector2(x * size + x * padding, y * size + y * padding));

					var itemslot = UI.MakeItemSlot(UI.MakeButton(buttonParameters), inventory.Get(i));
					
					if (itemSlots != null)
						itemSlots[i] = itemslot;

					var oldItem = inventory.Get(i);

					var output = HandleItemSlot(player, inventory, i, itemslot, heldInventory, new MenuHelper.WhiteListNone());

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

					if (output != ItemSlotClickOutput.None)
					{
						inventory.AddClick(player, i, output);
					}

					UI.EndParent();
				}
			}
			UI.EndParent();
		}

		public static Size GetInventorySize(int rows, int columns, float size, float padding)
        {
			return new Size(columns * size + columns * padding + 32, rows * size + rows * padding + 32);
        }

		public static ItemSlotClickOutput HandleItemSlot(Player player, Inventory inventory, int index, in UI.ItemSlot itemSlot)
		{
			ItemSlotClickOutput output = ItemSlotClickOutput.None;

			if (itemSlot.button.hovered && Main.inputManager.JustPressed(Keys.R))
			{
                Main.gameStateManager.GetCurrentGameState().PushMenu(new MenuRecipeBook(Main.gameStateManager, 
					null, inventory.Get(index)));

				return ItemSlotClickOutput.FilterRecipe;
			}

			return output;
		}

		public static ItemSlotClickOutput HandleRecipeFilter(GameStateManager gsManager, ItemInstance item, in UI.Button itemSlotButton)
        {
			if (itemSlotButton.hovered)
			{
				if (Main.inputManager.JustPressed(Keys.R))
				{
					if (MenuRecipeBook.HasAnyFilteredCatalysts(item, false, true))
					{
						gsManager.GetCurrentGameState().PushMenu(new MenuRecipeBook(gsManager, null, item, false, true));

						return ItemSlotClickOutput.FilterRecipe;
					}
				}
				else if (Main.inputManager.JustPressed(Keys.U))
				{
					if (MenuRecipeBook.HasAnyFilteredCatalysts(item, true, false))
					{
						gsManager.GetCurrentGameState().PushMenu(new MenuRecipeBook(gsManager, null, item, true, false));

						return ItemSlotClickOutput.FilterRecipe;
					}
				}
			}

			return ItemSlotClickOutput.None;
        }

		public static ItemSlotClickOutput HandleItemSlot<TWhiteList>(Player player, Inventory inventory, int index, in UI.ItemSlot itemSlot, Inventory heldInventory, TWhiteList whiteList) 
			where TWhiteList : struct, IWhiteList
		{
			ItemSlotClickOutput output = ItemSlotClickOutput.None;

			ItemSlotClickOutput reciperval = HandleRecipeFilter(Main.gameStateManager, inventory.Get(index), itemSlot.button);

			if (reciperval != ItemSlotClickOutput.None)
				return reciperval;

			if (itemSlot.button.clickLeft)
			{
				output = DoClick(player, inventory, heldInventory, index, Main.inputManager.IsHeld(Keys.LeftShift));
				//if (Main.inputManager.IsHeld(Keys.LeftShift) && itemSlot.item.valid)
				//{
				//	//defer this functionality to the user since we might need to swap between different inventories and this function can only see one inventory.
				//	output = ItemSlotClickOutput.NeedsSwapInventory;
				//}
				//else if (!heldInventory.Get(0).valid && itemSlot.item.valid)
				//{
				//	//Pick up the item - put it in the held item instance
				//	heldInventory.Set(itemSlot.item.Copy(), 0);
				//	inventory.Remove(index, heldInventory.Get(0).num);

				//	output = ItemSlotClickOutput.PickupFromSlot;

    //                heldInventory.Get(0).item?.StartHold(player, inventory, -1);
				//}
				//else if (heldInventory.Get(0).valid && itemSlot.item.valid)
				//{
				//	// Merge stacks
				//	if (inventory.Get(index).item == heldInventory.Get(0).item && heldInventory.Get(0).damage == itemSlot.item.damage)
				//	{
				//		int total = itemSlot.item.num + heldInventory.Get(0).num;

				//		if (total <= itemSlot.maxStackSize || itemSlot.maxStackSize == -1)
				//		{
				//			var newItem = heldInventory.Get(0);
				//			newItem = new ItemInstance(newItem, newItem.num + itemSlot.item.num);

    //                        inventory.Set(newItem, index);
				//			heldInventory.Remove(0);

				//			output = ItemSlotClickOutput.MergeInSlotCompletely;
				//		}
				//		else
				//		{
				//			//int rem = heldInventory.Get(0).num - (itemSlot.maxStackSize - itemSlot.item.num);

				//			inventory.Set(new ItemInstance(itemSlot.item, itemSlot.maxStackSize), index);
				//			heldInventory.Remove(itemSlot.maxStackSize - itemSlot.item.num);

    //                        //held = new ItemInstance(held, rem);

				//			output = ItemSlotClickOutput.MergeInSlotSome;
				//		}
				//	}
				//	else
				//	{
				//		// attempt to swap stacks
				//		if (itemSlot.maxStackSize == -1 || heldInventory.Get(0).num <= itemSlot.maxStackSize)
				//		{
				//			if (!whiteList.Matches(heldInventory.Get(0).item))
				//				return ItemSlotClickOutput.None;

				//			var oldHeld = heldInventory.Get(0);
				//			heldInventory.Set(itemSlot.item.Copy(), 0);
				//			inventory.Set(oldHeld, index);

				//			output = ItemSlotClickOutput.Swap;

				//			oldHeld.item.EndHold(player, inventory, -1);
    //                        heldInventory.Get(0).item?.StartHold(player, inventory, -1);
				//		}
				//		//Cannot swap stacks if doing so would put us above the max stack size. Swapping would have to involve actively removing or dropping items.
				//		else output = ItemSlotClickOutput.None;
				//	}
				//}
				//else if (heldInventory.Get(0).valid && !itemSlot.item.valid)
				//{
				//	// Place in slot. The held item is set to an empty item instance.

				//	int total = heldInventory.Get(0).num;

				//	if (total <= itemSlot.maxStackSize || itemSlot.maxStackSize == -1)
				//	{
				//		if (!whiteList.Matches(heldInventory.Get(0).item))
				//			return ItemSlotClickOutput.None;

    //                    heldInventory.Get(0).item.EndHold(player, inventory, index);

				//		inventory.Set(heldInventory.Get(0), index);
				//		heldInventory.Remove(0);

				//		output = ItemSlotClickOutput.PlaceInSlotAll;
				//	}
    //                else
    //                {
				//		//int rem = heldInventory.Get(0).num - itemSlot.maxStackSize;

				//		inventory.Set(new ItemInstance(heldInventory.Get(0), itemSlot.maxStackSize), index);
				//		heldInventory.Remove(itemSlot.maxStackSize);

				//		output = ItemSlotClickOutput.PlaceInSlotSome;
				//	}
				//}
			}
			else if (itemSlot.button.clickRight)
			{
				//right clicking picks up one item from the slot's stack and puts it 
				if (!heldInventory.Get(0).valid && itemSlot.item.valid)
				{
					//Pick up the item - put it in the held item instance
					heldInventory.Set(itemSlot.item, 1);
					inventory.Remove(index, 1);

					output = ItemSlotClickOutput.PickupFromSlot;
				}
				else if (heldInventory.Get(0).valid && itemSlot.item.valid)
				{
					// Merge stacks
					if (inventory.Get(index).item == heldInventory.Get(0).item && heldInventory.Get(0).damage == itemSlot.item.damage)
					{
						if (itemSlot.item.num != itemSlot.maxStackSize)
						{
							inventory.Set(new ItemInstance(heldInventory.Get(0), itemSlot.item.num + 1), index);

							heldInventory.Remove(0, 1);

							output = ItemSlotClickOutput.MergeInSlotSome;
						}
						else output = ItemSlotClickOutput.None;
					}
					else
					{
						// Don't do anything on the right click case. We can't swap.
					}
				}
				else if (heldInventory.Get(0).valid && !itemSlot.item.valid)
				{
					// Place in slot. The held item is set to an empty item instance.

					if (!whiteList.Matches(heldInventory.Get(0).item))
						return ItemSlotClickOutput.None;

					inventory.Set(new ItemInstance(heldInventory.Get(0), 1), index);
					heldInventory.Remove(0, 1);

					output = ItemSlotClickOutput.PlaceInSlotAll;
				}
			}

			return output;
		}

		public static ItemSlotClickOutput DoClick(Player player, Inventory inventory, Inventory heldInventory, int index, bool shiftHeld) 
		{
			ItemSlotClickOutput output = ItemSlotClickOutput.None;

			var ourItem = inventory.Get(index);
			var ourWhitelist = inventory.GetWhiteList(index);
			var ourMaxStackSize = inventory.GetMaxStackSize(index);

            if (shiftHeld && ourItem.valid)
            {
                //defer this functionality to the user since we might need to swap between different inventories and this function can only see one inventory.
                output = ItemSlotClickOutput.NeedsSwapInventory;
            }
            else if (!heldInventory.Get(0).valid && ourItem.valid)
            {
                //Pick up the item - put it in the held item instance
                heldInventory.Set(ourItem.Copy(), 0);
                inventory.Remove(index);

                output = ItemSlotClickOutput.PickupFromSlot;

                heldInventory.Get(0).item?.StartHold(player, inventory, -1);
            }
            else if (heldInventory.Get(0).valid && ourItem.valid)
            {
                // Merge stacks
                if (ourItem.item == heldInventory.Get(0).item && heldInventory.Get(0).damage == ourItem.damage)
                {
                    int total = ourItem.num + heldInventory.Get(0).num;

                    if (total <= ourMaxStackSize || ourMaxStackSize == -1)
                    {
                        var newItem = heldInventory.Get(0);
                        newItem = new ItemInstance(newItem, newItem.num + ourItem.num);

                        inventory.Set(newItem, index);
                        heldInventory.Remove(0);

                        output = ItemSlotClickOutput.MergeInSlotCompletely;
                    }
                    else
                    {
                        //int rem = heldInventory.Get(0).num - (itemSlot.maxStackSize - itemSlot.item.num);

                        inventory.Set(new ItemInstance(ourItem, ourMaxStackSize), index);
                        heldInventory.Remove(ourMaxStackSize - ourItem.num);

                        //held = new ItemInstance(held, rem);

                        output = ItemSlotClickOutput.MergeInSlotSome;
                    }
                }
                else
                {
                    // attempt to swap stacks
                    if (ourMaxStackSize == -1 || heldInventory.Get(0).num <= ourMaxStackSize)
                    {
                        if (!ourWhitelist?.Matches(heldInventory.Get(0).item) ?? false)
                            return ItemSlotClickOutput.None;

                        var oldHeld = heldInventory.Get(0);
                        heldInventory.Set(ourItem.Copy(), 0);
                        inventory.Set(oldHeld, index);

                        output = ItemSlotClickOutput.Swap;

                        oldHeld.item.EndHold(player, inventory, -1);
                        heldInventory.Get(0).item?.StartHold(player, inventory, -1);
                    }
                    //Cannot swap stacks if doing so would put us above the max stack size. Swapping would have to involve actively removing or dropping items.
                    else output = ItemSlotClickOutput.None;
                }
            }
            else if (heldInventory.Get(0).valid && !ourItem.valid)
            {
                // Place in slot. The held item is set to an empty item instance.

                int total = heldInventory.Get(0).num;

                if (total <= ourMaxStackSize || ourMaxStackSize == -1)
                {
                    if (!ourWhitelist?.Matches(heldInventory.Get(0).item) ?? false)
                        return ItemSlotClickOutput.None;

                    heldInventory.Get(0).item.EndHold(player, inventory, index);

                    inventory.Set(heldInventory.Get(0), index);
                    heldInventory.Remove(0);

                    output = ItemSlotClickOutput.PlaceInSlotAll;
                }
                else
                {
                    //int rem = heldInventory.Get(0).num - itemSlot.maxStackSize;

                    inventory.Set(new ItemInstance(heldInventory.Get(0), ourMaxStackSize), index);
                    heldInventory.Remove(ourMaxStackSize);

                    output = ItemSlotClickOutput.PlaceInSlotSome;
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
				var fi = new TextHelper.FontInfo(Main.assetsManager.GetAsset<SpriteFont>("fira_mono_tny"), 1, true, Color.Black);

				Vector2 pos = Main.inputManager.GetMousePosition().ToVector2();
				RectangleF bounds = new RectangleF(pos, size, size);

				held.item.DrawInInventory(batch, held, pos, scale);
				//batch.Draw(held.item.Texture, pos, held.item.SourceRect.ToRectangle(), Color.White, 0, Vector2.Zero, scale, SpriteEffects.None, 0.99f);

				int num = held.num;
				string numString;

                if (num > 1000)
                    numString = string.Format("{0:0.0}k", (float)num / 1000f);
                else numString = num.ToString();

                TextHelper.DrawText(batch, fi,
                    numString, Color.White, bounds.ToRectangle(), Enums.Alignment.BottomRight,
                    64, 1f, overflowAction: TextHelper.OverFlowAction.None);
			}
		}
	}
}
