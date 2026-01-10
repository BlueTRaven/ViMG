using Engine.Networking.Messages;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using ViMG;
using ViMG.Entities;
using ViMG.IMGUIImpl;
using ViMG.Items;
using ViMG.UIs;

namespace Engine.Items
{
	public class Inventory
	{
		public const int VERSION = 1;

		private enum InventoryActionType
		{
			Add,
			Set,
			Remove,
		}
		private struct InventoryAction
		{
			public required InventoryActionType type;
            public required int index;
            public required ItemInstance oldInstance, newInstance;
			public MenuHelper.ItemSlotClickOutput outputType;
        }

		public readonly int id;
		public readonly int generation;
		private int numSlots;
		public int NumSlots => numSlots;
		private ItemInstance[] items;
		private MenuHelper.IWhiteList?[] whitelists;
		private int[] maxStackSizes;

		private int lastEmpty;

		private List<InventoryAction> actions = new List<InventoryAction>();

		public record struct InventoryConfig
		{
			public int id;
			public int generation;
			public int numSlots;
			public MenuHelper.IWhiteList? whitelist;
			public MenuHelper.IWhiteList[]? whitelists;
			public int maxStackSize;
			public int[]? maxStackSizes;

			public InventoryConfig(int numSlots) 
			{
                this.numSlots = numSlots;
				whitelist = null;
				whitelists = null;
				maxStackSize = -1;
				maxStackSizes = null;
            } 

			public InventoryConfig(int numSlots, MenuHelper.IWhiteList whitelist, int maxStackSize = -1)
			{
				this.numSlots = numSlots;
				this.whitelist = whitelist;
				this.maxStackSize = maxStackSize;
                this.whitelists = null;
                this.maxStackSizes = null;
            }

            public InventoryConfig(int numSlots, MenuHelper.IWhiteList[] whitelists, int[]? maxStackSizes)
            {
                this.numSlots = numSlots;
                this.whitelists = whitelists;
                this.maxStackSizes = maxStackSizes;
				this.whitelist = null;
				this.maxStackSize = -1;
            }
        }

		public Inventory(InventoryConfig config)
		{
			this.id = config.id;
			this.generation = config.generation;
			this.numSlots = config.numSlots;
			items = new ItemInstance[numSlots];

			if (config.whitelist != null)
			{
				whitelists = new MenuHelper.IWhiteList?[numSlots];
				Array.Fill(whitelists, config.whitelist);
			} 
			else if (config.whitelists != null)
			{
				whitelists = config.whitelists;
			} 
			else
			{
                whitelists = new MenuHelper.IWhiteList?[numSlots];
                Array.Fill(whitelists, null);
            }

			if (config.maxStackSize != -1)
			{
				maxStackSizes = new int[numSlots];
				Array.Fill(maxStackSizes, config.maxStackSize);
			}
			else if (config.maxStackSizes != null)
			{
				maxStackSizes = config.maxStackSizes;
			}
			else
			{
				maxStackSizes = new int[numSlots];
				Array.Fill(maxStackSizes, -1);
			}

			lastEmpty = 0;
		}

		public void ProcessActionsServer<T>(T owner) where T : Entity, IHasInventory
		{
			foreach (var action in actions)
			{
				Console.WriteLine("Inventory action: {0:02} {1} {2} {3} {4} -> {5}", Main.Time, owner.ToString(), id, action.type.ToString(), action.oldInstance.item, action.newInstance.item);
				var invUpdate = new SyncInventoryUpdate.QueuedInventoryUpdate
				{
					inventory = new InventoryManager.InventoryReference((ushort)id, (short)generation),
					entity = owner.world.EntityManager.GetReference(owner),
					inventoryIndex = action.index,
					oldInstance = action.oldInstance,
					newInstance = action.newInstance,
					time = Main.Time
				};

                Main.gameStateManager.TheIsland.netManagerServer.SendMessageToAll(SyncInventoryUpdate.Instance, Main.gameStateManager.TheIsland.netManagerServer.netManager, invUpdate);
			}

			actions.Clear();
		}

		public virtual void DoUpdateAction(SyncInventoryUpdate.QueuedInventoryUpdate action)
		{
			items[action.inventoryIndex] = action.newInstance;
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

		public virtual ref readonly ItemInstance Get(int index)
		{
			return ref items[index];
		}

		public void Save(List<byte> saveBytes)
		{
			SaveHelper.SaveInt32(saveBytes, VERSION);
			SaveHelper.SaveInt32(saveBytes, id);
			SaveHelper.SaveInt32(saveBytes, generation);
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
            // TODO invalidate old versions
            int version = SaveHelper.LoadInt32(loadBytes, ref index);
            int id = SaveHelper.LoadInt32(loadBytes, ref index);
            int generation = SaveHelper.LoadInt32(loadBytes, ref index);
            int numSlots = SaveHelper.LoadInt32(loadBytes, ref index);

            //IMGUIConsole.Assert(id == this.id);

			int numValid = SaveHelper.LoadInt32(loadBytes, ref index);

			for (int i = 0; i < numValid; i++)
			{
				int itemIndex = SaveHelper.LoadInt32(loadBytes, ref index);
				ItemInstance itemInstance = SaveHelper.LoadItemInstance(loadBytes, ref index);

				ForceSet(itemInstance, itemIndex);
			}
		}

		public static Inventory ClientLoad(byte[] loadBytes, ref int index)
		{
            int version = SaveHelper.LoadInt32(loadBytes, ref index);
            int id = SaveHelper.LoadInt32(loadBytes, ref index);
            int generation = SaveHelper.LoadInt32(loadBytes, ref index);
            int numSlots = SaveHelper.LoadInt32(loadBytes, ref index);

            int numValid = SaveHelper.LoadInt32(loadBytes, ref index);

			// Client doesn't care about whitelists or max stack sizes
			InventoryConfig config = new InventoryConfig(numSlots);
			Inventory inv = new Inventory(config with { id = id, generation = generation });

            for (int i = 0; i < numValid; i++)
            {
                int itemIndex = SaveHelper.LoadInt32(loadBytes, ref index);
                ItemInstance itemInstance = SaveHelper.LoadItemInstance(loadBytes, ref index);

                inv.ForceSet(itemInstance, itemIndex);
            }

			return inv;
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
