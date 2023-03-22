using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Cubes;
using ViMG.Entities;

namespace ViMG.Items
{
    public class ItemFlintlockPistol : Item
	{
		private static AttackStats attackStats = new AttackStats(Player.DamageType.Ranged, 1.25f, 2, 1f);

		private ProjectileManager.ProjectileVisStats visStats = new ProjectileManager.ProjectileVisStats(Main.assetsManager.GetAsset<Texture2D>("projectiles"),
			new RectangleF(16, 0, 16, 16), Cube.CUBE_SCALE);
		private ProjectileManager.ProjectileStats stats = new ProjectileManager.ProjectileStats(HitboxManager.Group.PLAYER_DEAL, 2, 1f,
			Cube.CUBE_SCALE * 0.25f, Cube.CUBE_SCALE, 1, false, 0, true);

		public ItemFlintlockPistol() : base("flintlock_pistol", Main.assetsManager.GetAsset<Texture2D>("swrod"), new RectangleF(144, 128, 16, 16))
        {
            name = "Flintlock Pistol";
            description = "An old flintlock pistol. Better than a matchlock pistol!\n" +
				attackStats.GetTooltip();

            flipXInHand = true;
        }

		public override bool LeftClick(Player player, Inventory inventory, int index, Vector3 facing, out Player.ActionStats actionStats)
		{
			if (inventory.FindTag("ammo_bullet", out int ammoIndex).valid)
			{
                actionStats = new Player.ActionStats(attackStats);
				int damage = attackStats.damage;
				float knockback = attackStats.knockback;
				player.PerformAttack(Player.DamageType.Ranged, ref actionStats, ref damage, ref knockback);
				stats.damage = damage;
				stats.knockback = knockback;

				player.GetWorld().ProjectileManager.Add(new ProjectileManager.Projectile(player, player.Position,
					Vector3.Normalize(facing) * Cube.CUBE_SCALE * 25, Cube.CUBE_SCALE * 10, visStats, stats, index),
					new Rectangle3D(new Vector3(-Cube.CUBE_SCALE / 10f), new Vector3(Cube.CUBE_SCALE / 5f)));
				inventory.Remove(ammoIndex, 1);
				return true;
			}

			actionStats = new Player.ActionStats();
			return false;
		}
	}
}
