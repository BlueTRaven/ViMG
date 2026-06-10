using BrUtility;
using Engine;
using Engine.Items;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Cubes;
using ViMG.Entities;
using ViMG.Rendering;

namespace ViMG.Items
{
    public class ItemFlintlockPistol : Item
	{
		private static AttackStats attackStats = new AttackStats(DamageType.Ranged, 1.25f, 2, 1f);

		private ProjectileManager.ProjectileStats stats = new ProjectileManager.ProjectileStats(HitboxManager.Group.PLAYER_DEAL, 2, 1f,
			Cube.CUBE_SCALE * 0.25f, Cube.CUBE_SCALE, 1, false, 0, true);

		public ItemFlintlockPistol() : base("flintlock_pistol")
        {
            name = "Flintlock Pistol";
            description = "An old flintlock pistol. Better than a matchlock pistol!\n" +
				attackStats.GetTooltip();
        }

        protected override ClientItem ClientInit()
        {
            return new ClientItem(this, new RectangleF(144, 128, 16, 16), flipXInHand: false);
        }

		public override bool LeftClick(Player player, Inventory inventory, int index, Vector3 facing, out ActionStats actionStats)
		{
			if (inventory.FindTag("ammo_bullet", out int ammoIndex).valid)
			{
                actionStats = new ActionStats(attackStats);
				int damage = attackStats.damage;
				float knockback = attackStats.knockback;
				player.PerformAttack(DamageType.Ranged, ref actionStats, ref damage, ref knockback);
				stats.damage = damage;
				stats.knockback = knockback;

				player.GetWorld().ProjectileManager.Add(new ProjectileManager.Projectile(player, player.Position,
					Vector3.Normalize(facing) * Cube.CUBE_SCALE * 25, Cube.CUBE_SCALE * 10, GlobalState.Registry.ProjectileRegistry.Get("musketball").Id, stats, index));
				inventory.Remove(ammoIndex, 1);
				return true;
			}

			actionStats = new ActionStats();
			return false;
		}
	}
}
