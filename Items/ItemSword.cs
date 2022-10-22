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
		public ItemSword() : base("sword", Main.assetsManager.GetAsset<Texture2D>("swrod"), new RectangleF(16, 128, 16, 16))
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

        public override string GetDescription(ItemInstance item)
        {
			var meta = Get(item);

			if (meta != null)
            {
				return meta.GetStats().GetTooltip();
            }

            return base.GetDescription(item);
        }

        public override bool LeftClick(Player player, Inventory inventory, int index, Vector3 facing, out float itemCooldownTime)
		{
			base.LeftClick(player, inventory, index, facing, out itemCooldownTime);

			ItemSwordBlade meta = Get(inventory.Get(index));

			itemCooldownTime = meta.GetStats().cooldownTime;
			int damage = meta.GetStats().damage;
			float knockback = meta.GetStats().knockback;
			player.PerformAttack(Player.DamageType.Melee, ref itemCooldownTime, ref damage, ref knockback);

			player.SpawnHitbox(damage, Player.DamageType.Melee, -Main.camera.Forward, knockback);

			return true;
		}

		public static ItemInstance CreateSword(ItemInstance itemBlade)
		{
			return new ItemInstance(Main.Registry.ItemRegistry.Get("sword"), 1, itemBlade.item.Id);
		}
	}
}
