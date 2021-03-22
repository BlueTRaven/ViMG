using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace ViMG.Items
{
	public class ItemSword : ItemMetaItem<ItemSwordBlade>
	{
		public ItemSword() : base("sword", Main.assetsManager.GetAsset<Texture2D>("swrod"), new RectangleF(112, 48, 16, 16))
		{
		}

		public override string GetName(ItemInstance item)
		{
			var meta = Get(item);

			if (meta != null)
			{
				return meta.GetMaterial() + " Sword";
			}
			else return base.GetName(item);
		}

		public override bool LeftClick(Player player, Inventory inventory, int index, Vector3 facing, out float itemCooldownTime)
		{
			base.LeftClick(player, inventory, index, facing, out itemCooldownTime);

			player.SpawnHitbox(Get(inventory.Get(index)).GetStats().damage, 1f);
			player.PerformAttack(0.35f);

			return true;
		}

		public static ItemInstance CreateSword(ItemInstance itemBlade)
		{
			return new ItemInstance(Main.Registry.ItemRegistry.Get("sword"), 1, itemBlade.item.Id);
		}
	}
}
