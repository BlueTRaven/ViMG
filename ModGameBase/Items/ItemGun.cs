using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Cubes;
using ViMG.Entities;
using ViMG.Rendering;

namespace ViMG.Items
{
    public class ItemGun : Item
	{
		private ProjectileManager.ProjectileVisStats projVisStats;
		private ProjectileManager.ProjectileStats projStats;

		public ItemGun() : base("gun_base", new RectangleF(32, 0, 16, 16))
		{
			projVisStats = new ProjectileManager.ProjectileVisStats()
			{ 
				sourceRect = new RectangleF(0, 0, 16, 16),
				scale = Cube.CUBE_SCALE
			};

			projStats = new ProjectileManager.ProjectileStats()
			{
				group = HitboxManager.Group.PLAYER_DEAL,
				damage = 1,
				dieOnCollision = true
			};
		}

		public override bool LeftClick(Player player, Inventory inventory, int index, Vector3 facing, out ActionStats actionStats)
		{
			base.LeftClick(player, inventory, index, facing, out actionStats);

			int bulletIndex = -1;
			ItemInstance item = inventory.FindType(Main.Registry.ItemRegistry.Get("bullet_base"), out bulletIndex);
			if (item.valid)
			{
				inventory.Remove(bulletIndex, 1);
				player.GetWorld().ProjectileManager.Add(new ProjectileManager.Projectile(player, player.Position + Main.camera.Right * 4,
					Vector3.Normalize(facing) * 100, 2, projVisStats, projStats),
					new Rectangle3D(new Vector3(-Cube.CUBE_SCALE / 2f), new Vector3(Cube.CUBE_SCALE)));

				return true;
			}

			return base.LeftClick(player, inventory, index, facing, out actionStats);
		}
	}
}
