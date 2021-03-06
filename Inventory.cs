using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Items;

namespace ViMG
{
	public class Inventory
	{
		private int numSlots;
		public int NumSlots => numSlots;
		private ItemInstance[] items;

		private int lastEmpty;
		public Inventory(int numSlots)
		{
			this.numSlots = numSlots;
			items = new ItemInstance[numSlots];

			lastEmpty = 0;
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

		public bool Add(ItemInstance item)
		{
			for (int i = 0; i < numSlots; i++)
			{
				// merge stacks
				if (items[i].item == item.item)
				{
					items[i] = new ItemInstance(items[i], items[i].num + item.num);
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
					return true;
				}
			}

			return false;
		}

		public void Set(ItemInstance item, int index)
		{
			items[index] = item;
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

		public ref readonly ItemInstance Find(Item item, out int index)
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

		public void Remove(int index, int num)
		{
			items[index] = new ItemInstance(items[index], items[index].num - num);

			if (items[index].num <= 0)
				items[index] = ItemInstance.Empty;
		}

		public ref readonly ItemInstance Get(int index)
		{
			return ref items[index];
		}
	}
}
