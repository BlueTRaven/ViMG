using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Cubes;
using ViMG.Entities;

namespace ViMG.Items
{
	public class ItemGun : Item
	{
		private ProjectileManager.ProjectileVisStats projVisStats;
		private ProjectileManager.ProjectileStats projStats;

		public ItemGun() : base("gun_base", Main.assetsManager.GetAsset<Texture2D>("swrod"), new RectangleF(32, 0, 16, 16))
		{
			projVisStats = new ProjectileManager.ProjectileVisStats()
			{ 
				texture = Main.assetsManager.GetAsset<Texture2D>("bullet"),
				scale = Cube.CUBE_SCALE
			};

			projStats = new ProjectileManager.ProjectileStats()
			{
				group = Player.GROUP_PLAYER_DEAL_SOURCE,
				damage = 1,
				dieOnCollision = true
			};
		}

		public override bool LeftClick(Player player, Inventory inventory, int index, Vector3 facing, out float itemCooldownTime)
		{
			base.LeftClick(player, inventory, index, facing, out itemCooldownTime);

			int bulletIndex = -1;
			ItemInstance item = inventory.Find(Main.Registry.ItemRegistry.Get("bullet_base"), out bulletIndex);
			if (item.valid)
			{
				inventory.Remove(bulletIndex, 1);
				player.GetWorld().ProjectileManager.Add(new ProjectileManager.Projectile(player.Position + Main.camera.Right * 4,
					Vector3.Normalize(facing) * 100, 2, projVisStats, projStats),
					new Rectangle3D(new Vector3(-Cube.CUBE_SCALE / 2f), new Vector3(Cube.CUBE_SCALE)));

				return true;
			}

			return base.LeftClick(player, inventory, index, facing, out itemCooldownTime);
		}
	}
}
