using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Cubes;
using ViMG.Entities.Renderers;

namespace ViMG.Items
{
	public class ItemSwordBase : Item
	{
		public ItemSwordBase() : base("sword_base", StaticMaterials.Items, new BrUtility.RectangleF(0, 0, 16, 16))
		{
		}

		public override bool LeftClick(Player player, Inventory inventory, int index, Vector3 facing, out Player.ActionStats actionStats)
		{
			base.LeftClick(player, inventory, index, facing, out actionStats);

            actionStats = new Player.ActionStats(0.35f);
			int damage = 1;
			float knockback = 1f;
			player.PerformAttack(Player.DamageType.Melee, ref actionStats, ref damage, ref knockback);

			player.SpawnHitboxLater(index, 1, Player.DamageType.Melee, -Main.camera.Forward, 1f);

            actionStats.animationType = Player.UseAnimationType.SwingHorizontal;

            return true;
		}
	}
}
