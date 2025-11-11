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
using ViMG.Rendering;

namespace ViMG.Items
{
    public class ItemLavaCannon : Item, IProjectileEffects
    {
        private RangedAttackStats rangeAttackStats = new RangedAttackStats(new AttackStats(DamageType.Ranged, 2f, 14, 4), Cube.CUBE_SCALE * 20, 0);

        private ProjectileManager.ProjectileVisStats visStats = new ProjectileManager.ProjectileVisStats(new RectangleF(32, 16, 16, 16), Cube.CUBE_SCALE);
		private ProjectileManager.ProjectileStats stats;

        public ItemLavaCannon() : base("cannon_lavacrystal", new RectangleF(128, 112, 32, 16))
        {
            name = "Lava Cannon";
            description = "Fires a crystal of lava that explodes upon impact.\n" +
                "Consumes 4 musket balls when fired." + 
                rangeAttackStats.GetTooltip();

			stats = new ProjectileManager.ProjectileStats(HitboxManager.Group.PLAYER_DEAL, 2, 1f,
				Cube.CUBE_SCALE * 0.25f, Cube.CUBE_SCALE, 1, false, 0, true, effects: this);

			flipXInHand = true;
		}

		public override bool LeftClick(Player player, Inventory inventory, int index, Vector3 facing, out ActionStats actionStats)
		{
			var bulletItem = inventory.FindTag("ammo_bullet", out int ammoIndex);
			if (bulletItem.valid && bulletItem.num >= 4)
			{
                actionStats = new ActionStats(rangeAttackStats.attackStats);
				int damage = rangeAttackStats.attackStats.damage;
				float knockback = rangeAttackStats.attackStats.knockback;
				player.PerformAttack(DamageType.Ranged, ref actionStats, ref damage, ref knockback);
				stats.damage = damage;
				stats.knockback = knockback;

				player.GetWorld().ProjectileManager.Add(new ProjectileManager.Projectile(player, player.Position,
					Vector3.Normalize(facing) * rangeAttackStats.projectileSpeed, Cube.CUBE_SCALE * 10, visStats, stats, index),
					new Rectangle3D(new Vector3(-Cube.CUBE_SCALE / 10f), new Vector3(Cube.CUBE_SCALE / 5f)));
				
				inventory.Remove(ammoIndex, 4);

				return true;
			}

			actionStats = new ActionStats();
			return false;
		}

        public void OnProjectileDeath(World world, int projectile)
        {
			ProjectileManager.Projectile proj = world.ProjectileManager.Get(projectile);

			world.EntityManager.Add(new GenericExplosion(proj.position, stats.group, stats.damage, stats.knockback, Cube.CUBE_SCALE * 2f));
        }
    }
}
