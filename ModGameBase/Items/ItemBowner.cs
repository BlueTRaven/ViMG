using BrUtility;
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
    public class ItemBowner : Item
    {
        private static AttackStats attackStats = new AttackStats(DamageType.Ranged, 1.1f, 12, 1);

		private ProjectileManager.ProjectileStats stats;
		private ProjectileManager.ProjectileBatchStats batchStats;

        public ItemBowner() : base("bow_bowner")
        {
			Client = new ClientItem(this, new RectangleF(160, 128, 16, 16));

            name = "Bowner";
            description = "A bow crafted from finely-carved bone.\n" +
                attackStats.GetTooltip() +
                "Shoots two arrows at an angle. Consumes two arrows at a time.";

			stats = new ProjectileManager.ProjectileStats(HitboxManager.Group.PLAYER_DEAL, attackStats.damage, attackStats.knockback,
				Cube.CUBE_SCALE / 4f, Cube.CUBE_SCALE, 1, true, 0.5f, true);
			batchStats = new ProjectileManager.ProjectileBatchStats(2, new float[] { -7f, 7f }, null);
		}

		public override bool LeftClick(Player player, Inventory inventory, int index, Vector3 facing, out ActionStats actionStats)
		{
			var item = inventory.FindTag("ammo_arrow", out int ammoIndex);
			if (item.valid && item.num >= 2)
			{
                actionStats = new ActionStats(attackStats);
				int damage = attackStats.damage;
				float knockback = attackStats.knockback;
				player.PerformAttack(DamageType.Ranged, ref actionStats, ref damage, ref knockback);
				stats.damage = damage;
				stats.knockback = knockback;

				player.GetWorld().ProjectileManager.AddBatch(player, player.Position, Vector3.Normalize(facing) * Cube.CUBE_SCALE * 32,
					Cube.CUBE_SCALE * 10, batchStats, Main.Registry.ProjectileRegistry.Get("arrow").Id, stats, new Rectangle3D(new Vector3(-Cube.CUBE_SCALE / 10f), new Vector3(Cube.CUBE_SCALE / 5f)), index);
				
				inventory.Remove(ammoIndex, 2);
				return true;
			}

			actionStats = new ActionStats();
			return false;
		}
	}
}
