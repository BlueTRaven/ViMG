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

        public override bool LeftClick(Player player, Inventory inventory, int index, Vector3 facing, out Player.ActionStats actionStats)
		{
			base.LeftClick(player, inventory, index, facing, out actionStats);

			ItemSwordBlade meta = Get(inventory.Get(index));

            actionStats = new Player.ActionStats(meta.GetStats().attackStats);
			int damage = meta.GetStats().attackStats.damage;
			float knockback = meta.GetStats().attackStats.knockback;
			player.PerformAttack(Player.DamageType.Melee, ref actionStats, ref damage, ref knockback);

			player.SpawnHitboxLater(index, damage, Player.DamageType.Melee, -Main.camera.Forward, knockback, meta.GetStats().range);

			actionStats.animationType = Player.UseAnimationType.SwingHorizontal;

			return true;
		}

		public static ItemInstance CreateSword(ItemInstance itemBlade)
		{
			return new ItemInstance(Main.Registry.ItemRegistry.Get("sword"), 1, itemBlade.item.Id);
		}
	}
}
