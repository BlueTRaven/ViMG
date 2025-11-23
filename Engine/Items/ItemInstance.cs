using LiteNetLib.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace ViMG.Items
{
	public readonly struct ItemInstance
	{
		public readonly Item? item;
		public readonly int num;

		public readonly int damage;

		public readonly bool valid;

		private static ItemInstance empty;
		public static ref readonly ItemInstance Empty => ref empty;

		// Constructs a new ItemInstance by adding the num of other with num.
		public ItemInstance(ItemInstance other, int num)
		{
			item = other.item;
			this.num = num;
			damage = other.damage;

			valid = other.item != null;
		}

		public ItemInstance(Item? item, int num, int damage)
		{
			this.item = item;
			this.num = num;
			this.damage = damage;

			valid = item != null;
		}

		public ItemInstance Copy()
		{
			return new ItemInstance(item, num, damage);
		}
    }
}
