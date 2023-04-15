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
    public class ItemStoneBlunderbuss : Item
	{
		private static AttackStats attackStatsWithMusketballs = new AttackStats(Player.DamageType.Ranged, 0.95f, 4, 1);
		private static AttackStats attackStats = new AttackStats(Player.DamageType.Ranged, 0.95f, 1, 1f);

		private ProjectileManager.ProjectileBatchStats batchStatsWithMusketballs = new ProjectileManager.ProjectileBatchStats(8, new Vector2(-45, 45), new Vector2(-45, 45));
		private ProjectileManager.ProjectileVisStats visStatsWithMusketballs = new ProjectileManager.ProjectileVisStats(new RectangleF(0, 16, 16, 16), Cube.CUBE_SCALE);
		private ProjectileManager.ProjectileStats statsWithMusketballs = new ProjectileManager.ProjectileStats(HitboxManager.Group.PLAYER_DEAL, 4, 1f,
			Cube.CUBE_SCALE * 0.5f, Cube.CUBE_SCALE, 1, true, 0.3f, true);

		private ProjectileManager.ProjectileBatchStats batchStats = new ProjectileManager.ProjectileBatchStats(8, new Vector2(-25, 25), new Vector2(-25, 25));
		private ProjectileManager.ProjectileVisStats visStats = new ProjectileManager.ProjectileVisStats(new RectangleF(16, 0, 16, 16), Cube.CUBE_SCALE);
		private ProjectileManager.ProjectileStats stats = new ProjectileManager.ProjectileStats(HitboxManager.Group.PLAYER_DEAL, 1, 1f,
			Cube.CUBE_SCALE * 0.5f, Cube.CUBE_SCALE);

		public ItemStoneBlunderbuss() : base("stone_blunderbuss", StaticMaterials.Items, new RectangleF(80, 128, 16, 16))
        {
			name = "Stone Blunderbuss";
			description = "A blunderbuss crudely made from stone. Don't ask me how they made it.\n" +
				attackStats.GetTooltip() +
				"Fires high-damage bullets in a large spread. Musketballs are converted into stone shards, with higher damage but an even larger spread.\n" +
				"Consumes 4 ammo per shot.";
			flipXInHand = true;
        }

		public override bool LeftClick(Player player, Inventory inventory, int index, Vector3 facing, out Player.ActionStats actionStats)
		{
			ItemInstance ammo = inventory.FindTag("ammo_bullet", out int ammoIndex);

			if (ammo.valid && ammo.num >= 4)
			{
				Vector3 direction = Vector3.Normalize(facing) * Cube.CUBE_SCALE * 26;
				Rectangle3D bounds = new Rectangle3D(new Vector3(-Cube.CUBE_SCALE / 5f), new Vector3(Cube.CUBE_SCALE / 2.5f));
				if (ammo.item == Main.Registry.ItemRegistry.Get("ammo_bullet_musketball"))
                {
                    actionStats = new Player.ActionStats(attackStatsWithMusketballs);
					int damage = attackStatsWithMusketballs.damage;
					float knockback = attackStatsWithMusketballs.knockback;
					player.PerformAttack(Player.DamageType.Ranged, ref actionStats, ref damage, ref knockback);
					statsWithMusketballs.damage = damage;
					statsWithMusketballs.knockback = knockback;

					player.GetWorld().ProjectileManager.AddBatch(player, player.Position, direction, 1.5f, 
						batchStatsWithMusketballs, visStatsWithMusketballs, statsWithMusketballs, bounds, index);
                }
                else
                {
                    actionStats = new Player.ActionStats(attackStats);
                    int damage = attackStats.damage;
					float knockback = attackStats.knockback;
					player.PerformAttack(Player.DamageType.Ranged, ref actionStats, ref damage, ref knockback);
					stats.damage = damage;
					stats.knockback = knockback;

					player.GetWorld().ProjectileManager.AddBatch(player, player.Position, direction, 1.5f, 
						batchStats, visStats, stats, bounds, index);
				}
			
				inventory.Remove(ammoIndex, 4);
				return true;
			}

			actionStats = new Player.ActionStats();
			return false;
		}
	}
}
