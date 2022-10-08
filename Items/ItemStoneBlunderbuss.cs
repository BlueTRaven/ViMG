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
    public class ItemStoneBlunderbuss : Item
    {
		private ProjectileManager.ProjectileBatchStats batchStatsWithMusketballs = new ProjectileManager.ProjectileBatchStats(8, new Vector2(-45, 45), new Vector2(-45, 45));
		private ProjectileManager.ProjectileVisStats visStatsWithMusketballs = new ProjectileManager.ProjectileVisStats(Main.assetsManager.GetAsset<Texture2D>("projectiles"),
			   new RectangleF(0, 16, 16, 16), Cube.CUBE_SCALE);
		private ProjectileManager.ProjectileStats statsWithMusketballs = new ProjectileManager.ProjectileStats(HitboxManager.Group.PLAYER_DEAL, 4,
			Cube.CUBE_SCALE * 0.5f, Cube.CUBE_SCALE, true, 0.3f, true);

		private ProjectileManager.ProjectileBatchStats batchStats = new ProjectileManager.ProjectileBatchStats(8, new Vector2(-25, 25), new Vector2(-25, 25));
		private ProjectileManager.ProjectileVisStats visStats = new ProjectileManager.ProjectileVisStats(Main.assetsManager.GetAsset<Texture2D>("projectiles"),
			   new RectangleF(16, 0, 16, 16), Cube.CUBE_SCALE);
		private ProjectileManager.ProjectileStats stats = new ProjectileManager.ProjectileStats(HitboxManager.Group.PLAYER_DEAL, 1,
			Cube.CUBE_SCALE * 0.5f, Cube.CUBE_SCALE, false, 0, true);

		public ItemStoneBlunderbuss() : base("stone_blunderbuss", Main.assetsManager.GetAsset<Texture2D>("swrod"), new RectangleF(80, 128, 16, 16))
        {
			name = "Stone Blunderbuss";
			description = "A blunderbuss crudely made from stone. Don't ask me how they made it.\n" +
				"Fires high-damage bullets in a large spread. Musketballs are converted into stone shards, with higher damage but an even larger spread. Consumes 4 ammo per shot.";
			flipXInHand = true;
        }

		public override bool LeftClick(Player player, Inventory inventory, int index, Vector3 facing, out float itemCooldownTime)
		{
			ItemInstance ammo = inventory.FindTag("ammo_bullet", out int ammoIndex);

			if (ammo.valid && ammo.num >= 4)
			{
				Vector3 direction = Vector3.Normalize(facing) * Cube.CUBE_SCALE * 26;
				Rectangle3D bounds = new Rectangle3D(new Vector3(-Cube.CUBE_SCALE / 5f), new Vector3(Cube.CUBE_SCALE / 2.5f));
				if (ammo.item == Main.Registry.ItemRegistry.Get("ammo_bullet_musketball"))
                {
					player.GetWorld().ProjectileManager.AddBatch(player, player.Position, direction, 1.5f, batchStatsWithMusketballs, visStatsWithMusketballs, statsWithMusketballs, bounds);
                }
                else
                {
					player.GetWorld().ProjectileManager.AddBatch(player, player.Position, direction, 1.5f, batchStats, visStats, stats, bounds);
				}
			
				itemCooldownTime = 0.95f;
				inventory.Remove(ammoIndex, 4);
				return true;
			}

			itemCooldownTime = 0;
			return false;
		}
	}
}
