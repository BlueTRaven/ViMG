using Engine.Networking.Messages;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using ViMG;
using ViMG.Entities;
using ViMG.Items;
using ViMG.UIs;

namespace Engine.Items
{
	public class Inventory
	{
		private enum InventoryActionType
		{
			Add,
			Set,
			Remove,
			Clicked,
		}
		private struct InventoryAction
		{
			public required InventoryActionType type;
            public required int index;
            public required ItemInstance oldInstance, newInstance;
			public MenuHelper.ItemSlotClickOutput outputType;
        }

		public readonly int id;
		private int numSlots;
		public int NumSlots => numSlots;
		private ItemInstance[] items;
		private MenuHelper.IWhiteList?[] whitelists;
		private int[] maxStackSizes;

		private int lastEmpty;

		private List<InventoryAction> actions = new List<InventoryAction>();

		public Inventory(int id, int numSlots)
		{
			this.id = id;
			this.numSlots = numSlots;
			items = new ItemInstance[numSlots];
			whitelists = new MenuHelper.IWhiteList?[numSlots];
			maxStackSizes = new int[numSlots];
			Array.Fill(maxStackSizes, -1);

			lastEmpty = 0;
		}

		public Inventory(int id, int numSlots, MenuHelper.IWhiteList whitelist, int maxStackSize = -1)
		{
            this.id = id;
            this.numSlots = numSlots;
            items = new ItemInstance[numSlots];
            whitelists = new MenuHelper.IWhiteList?[numSlots];
			Array.Fill(whitelists, whitelist);
            maxStackSizes = new int[numSlots];
			Array.Fill(maxStackSizes, maxStackSize);

            lastEmpty = 0;
        }

        public Inventory(int id, int numSlots, MenuHelper.IWhiteList?[] whitelists, int[]? maxStackSizes = null)
        {
            this.id = id;
            this.numSlots = numSlots;
            items = new ItemInstance[numSlots];
			this.whitelists = whitelists;
			if (maxStackSizes == null)
			{
				this.maxStackSizes = new int[numSlots];
				Array.Fill(this.maxStackSizes, -1);
			}
			else this.maxStackSizes = maxStackSizes;

			lastEmpty = 0;
        }


        public void AddClick(Entity owner, int index, MenuHelper.ItemSlotClickOutput outputType)
		{
			actions.Add(new InventoryAction
			{
				index = index,
				oldInstance = new(),
				newInstance = new(),
				type = InventoryActionType.Clicked,
				outputType = outputType,
			});
		}

		public void ProcessActions(Entity owner)
		{
			//foreach (var action in actions)
			//{
			//	//Console.WriteLine("Inventory update: {0} {1} {2} -> {3}", id, action.type.ToString(), action.oldInstance.item, action.newInstance.item);
			//	var invUpdate = new SyncInventoryUpdate.QueuedInventoryUpdate
			//	{
			//		id = id,
			//		entityId = owner.Id,
			//		index = action.index,
			//		oldInstance = action.oldInstance,
			//		newInstance = action.newInstance,
			//		time = Main.Time
			//	};

			//	if (action.type != InventoryActionType.Clicked)
			//	{
			//		if (Main.gameStateManager.netMode == ViMG.GameStates.GameStateManager.NetworkingMode.Server)
			//		{
			//			Main.Registry.MessageRegistry.SendMessageToAll(SyncInventoryUpdate.Instance, Main.gameStateManager.TheIsland.netManager.netManager, invUpdate);
			//		}
			//		else if (Main.gameStateManager.netMode == ViMG.GameStates.GameStateManager.NetworkingMode.Client)
			//		{
			//			Main.Registry.MessageRegistry.SendMessageToAll(SyncInventoryUpdateAuditRequest.Instance, Main.gameStateManager.TheIsland.netManager.netManager, invUpdate);
			//		}
			//	}
			//	else
			//	{
			//		if (Main.gameStateManager.netMode == ViMG.GameStates.GameStateManager.NetworkingMode.Client)
			//		{
			//			Main.Registry.MessageRegistry.SendMessageToAll(SyncInventoryUpdateAuditRequest.Instance, Main.gameStateManager.TheIsland.netManager.netManager, invUpdate);
			//		}
			//	}
			//}

			actions.Clear();
		}

		public void DoUpdateAction(SyncInventoryUpdate.QueuedInventoryUpdate action)
		{
			items[action.index] = action.newInstance;
		}

        public bool CanAdd(Item item)
		{
			for (int i = 0; i < numSlots; i++)
			{
				if (!items[i].valid && items[i].item == null || (items[i].valid && items[i].item == item))
					return true;
			}

			return false;
		}

		public bool Add(ItemInstance item, out int placedIndex)
		{
			for (int i = 0; i < numSlots; i++)
			{
				// merge stacks
				if (items[i].item == item.item && items[i].damage == item.damage)
				{
					var oldInstance = items[i];
					items[i] = new ItemInstance(items[i], items[i].num + item.num);
					placedIndex = i;

					actions.Add(new InventoryAction
					{
						type = InventoryActionType.Add,
						index = i,
						oldInstance = oldInstance,
						newInstance = items[i],
					});
					//if (Main.gameStateManager.netMode == GameStates.GameStateManager.NetworkingMode.Server)
					//{
					//	var a = new SyncInventoryUpdate.QueuedInventoryUpdate
					//	{
					//		id = id,
					//		index = placedIndex,
					//		oldInstance = oldInstance,
					//		newInstance = items[i],
					//		player = -1,
					//		time = Main.Time
					//	};

					//	Main.Registry.MessageRegistry.SendMessageToAll(SyncInventoryUpdate.Instance, Main.gameStateManager.TheIsland.netManager.netManager, a);
					//}

					return true;
				}
			}

			// after above merge check, check for empty slots.
			// do this after so that if the inventory already has the item, it will go there first instead of an empty slot.
			for (int i = 0; i < numSlots; i++)
			{
				if (!items[i].valid)
				{
					items[i] = item;
					placedIndex = i;

                    actions.Add(new InventoryAction
                    {
                        type = InventoryActionType.Add,
						index = i,
                        oldInstance = new ItemInstance(),
                        newInstance = items[i],
                    });

                    return true;
				}
			}

			placedIndex = -1;
			return false;
		}

		public bool Add(ItemInstance item)
        {
			return Add(item, out _);
        }

		public void Set(ItemInstance item, int index, bool markDirty = true)
		{
			var oldInstance = items[index];
			items[index] = item;

			if (markDirty)
			{
				actions.Add(new InventoryAction
				{
					type = InventoryActionType.Set,
					index = index,
					oldInstance = oldInstance,
					newInstance = items[index],
				});
			}
        }

		public void ForceSet(ItemInstance item, int index)
		{
			Set(item, index, false);
		}

		public ref readonly ItemInstance Find(Item item)
		{
			for (int i = 0; i < numSlots; i++)
			{
				if (items[i].item == item)
					return ref Get(i);
			}

			return ref ItemInstance.Empty;
		}

		public int FirstEmpty()
		{
			for (int i = 0; i < NumSlots; i++)
			{
				if (!items[i].valid)
					return i;
			}

			return -1;
		}

		//returns the first item instance with the given tag.
		public ref readonly ItemInstance FindTag(string tag, out int index) 
		{
			for (int i = 0; i < numSlots; i++)
			{
				if (items[i].valid && items[i].item.Tags.Contains(tag))
				{
					index = i;
					return ref Get(i);
				}
			}

			index = -1;
			return ref ItemInstance.Empty;
		}

		public ref readonly ItemInstance FindType(Item item, out int index)
		{
			for (int i = 0; i < numSlots; i++)
			{
				if (items[i].item == item)
				{
					index = i;
					return ref Get(i);
				}
			}

			index = -1;
			return ref ItemInstance.Empty;
		}

		public ref readonly ItemInstance FindExact(ItemInstance item, int max, out int index)
		{
			for (int i = 0; i < max; i++)
			{
				if (items[i].item == item.item && items[i].damage == item.damage)
				{
					index = i;
					return ref Get(i);
				}
			}

			index = -1;
			return ref ItemInstance.Empty;
		}

		public ref readonly ItemInstance Find(Item item, int max, out int index)
		{
			for (int i = 0; i < max; i++)
			{
				if (items[i].item == item)
				{
					index = i;
					return ref Get(i);
				}
			}

			index = -1;
			return ref ItemInstance.Empty;
		}

		public void Remove(int index, int num)
		{
			if (num == -1)
			{
                actions.Add(new InventoryAction
                {
                    type = InventoryActionType.Remove,
                    index = index,
                    oldInstance = items[index],
                    newInstance = new ItemInstance(),
                });

                items[index] = new ItemInstance();

				return;
			}
			else
			{
				var oldInstance = items[index];

				items[index] = new ItemInstance(items[index], items[index].num - num);

				if (items[index].num <= 0)
					items[index] = ItemInstance.Empty;

                actions.Add(new InventoryAction
                {
                    type = InventoryActionType.Remove,
                    index = index,
                    oldInstance = oldInstance,
                    newInstance = items[index],
                });
            }
		}

		public void Remove(int index)
		{
			Remove(index, -1);
		}

		public ref readonly ItemInstance Get(int index)
		{
			return ref items[index];
		}

		public void Save(List<byte> saveBytes)
		{
			SaveHelper.SaveInt32(saveBytes, id);
			SaveHelper.SaveInt32(saveBytes, numSlots);

			int numValid = 0;
			for (int i = 0; i < numSlots; i++)
			{
				if (Get(i).valid)
				{
					numValid++;
				}
			}

			SaveHelper.SaveInt32(saveBytes, numValid);

			for (int i = 0; i < numSlots; i++)
			{
				if (Get(i).valid)
				{
					SaveHelper.SaveInt32(saveBytes, i);
					SaveHelper.SaveItemInstance(saveBytes, Get(i));
				}
			}
		}

		public void Load(byte[] loadBytes, ref int index)
		{
			int id = SaveHelper.LoadInt32(loadBytes, ref index);
			int numSlots = SaveHelper.LoadInt32(loadBytes, ref index);

			Debug.Assert(id == this.id);

			int numValid = SaveHelper.LoadInt32(loadBytes, ref index);

			for (int i = 0; i < numValid; i++)
			{
				int itemIndex = SaveHelper.LoadInt32(loadBytes, ref index);
				ItemInstance itemInstance = SaveHelper.LoadItemInstance(loadBytes, ref index);

				ForceSet(itemInstance, itemIndex);
			}
		}

		public MenuHelper.IWhiteList? GetWhiteList(int index)
		{
			return whitelists[index];
		}

		public int GetMaxStackSize(int index)
		{
			return maxStackSizes[index];
		}
	}
}
