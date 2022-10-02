using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Cubes;

namespace ViMG.Items
{
	public class ItemSwordBase : Item
	{
		public ItemSwordBase() : base("sword_base", Main.assetsManager.GetAsset<Texture2D>("swrod"), new BrUtility.RectangleF(0, 0, 16, 16))
		{
		}

		public override bool LeftClick(Player player, Inventory inventory, int index, Vector3 facing, out float itemCooldownTime)
		{
			base.LeftClick(player, inventory, index, facing, out itemCooldownTime);

			player.SpawnHitbox(1, Player.PlayerDamageType.Melee, 1f);
			player.PerformAttack(0.35f);

			return true;
		}
	}
}
