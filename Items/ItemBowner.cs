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
    public class ItemBowner : Item
    {
        private static AttackStats attackStats = new AttackStats(1.1f, 12, 1);

		private ProjectileManager.ProjectileVisStats visStats;
		private ProjectileManager.ProjectileStats stats;
		private ProjectileManager.ProjectileBatchStats batchStats;

        public ItemBowner() : base("bow_bowner", Main.assetsManager.GetAsset<Texture2D>("swrod"), new RectangleF(160, 128, 16, 16))
        {
            name = "Bowner";
            description = "A bow crafted from finely-carved bone.\n" +
                attackStats.GetTooltip() +
                "Shoots two arrows at an angle. Consumes two arrows at a time.";

			visStats = new ProjectileManager.ProjectileVisStats(Main.assetsManager.GetAsset<Texture2D>("projectiles"), new RectangleF(16, 0, 16, 16), Cube.CUBE_SCALE);
			stats = new ProjectileManager.ProjectileStats(HitboxManager.Group.PLAYER_DEAL, attackStats.damage,
				Cube.CUBE_SCALE / 4f, Cube.CUBE_SCALE, true, 0.5f, true);
			batchStats = new ProjectileManager.ProjectileBatchStats(2, new float[] { -7f, 7f }, null);
		}

		public override bool LeftClick(Player player, Inventory inventory, int index, Vector3 facing, out float itemCooldownTime)
		{
			var item = inventory.FindTag("ammo_arrow", out int ammoIndex);
			if (item.valid && item.num >= 2)
			{
				player.GetWorld().ProjectileManager.AddBatch(player, player.Position, Vector3.Normalize(facing) * Cube.CUBE_SCALE * 32,
					Cube.CUBE_SCALE * 10, batchStats, visStats, stats, new Rectangle3D(new Vector3(-Cube.CUBE_SCALE / 10f), new Vector3(Cube.CUBE_SCALE / 5f)));

				itemCooldownTime = attackStats.cooldownTime;
				player.PerformAttack(Player.PlayerDamageType.Range, ref itemCooldownTime);
				inventory.Remove(ammoIndex, 2);
				return true;
			}

			itemCooldownTime = 0;
			return false;
		}
	}
}
