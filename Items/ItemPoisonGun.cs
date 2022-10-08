using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Buffs;
using ViMG.Cubes;
using ViMG.Entities;

namespace ViMG.Items
{
    public class ItemPoisonGun : Item
    {
		private static Buff.BuffInstance[] applyBuffs = new Buff.BuffInstance[1]
		{
			new Buff.BuffInstance(Main.Registry.BuffRegistry.Get("poisoned"), 10)
		};

		private static ProjectileManager.ProjectileVisStats visStats = new ProjectileManager.ProjectileVisStats(Main.assetsManager.GetAsset<Texture2D>("projectiles"),
			new RectangleF(16, 0, 16, 16), Cube.CUBE_SCALE);
		private static ProjectileManager.ProjectileStats stats = new ProjectileManager.ProjectileStats(HitboxManager.Group.PLAYER_DEAL, 1,
			Cube.CUBE_SCALE * 0.5f, Cube.CUBE_SCALE, false, 0, true, applyBuffs);

		public ItemPoisonGun() : base("poison_gun", Main.assetsManager.GetAsset<Texture2D>("swrod"), new RectangleF(112, 128, 16, 16))
        {
            name = "Poison Gun";
            description = "Musketballs are converted into poisonous gobs of poison, which inflict the poisoned debuff on enemies.";
			flipXInHand = true;
        }

		public override bool LeftClick(Player player, Inventory inventory, int index, Vector3 facing, out float itemCooldownTime)
		{
			if (inventory.FindTag("ammo_bullet", out int ammoIndex).valid)
			{
				int projectile = player.GetWorld().ProjectileManager.Add(new ProjectileManager.Projectile(player, player.Position,
					Vector3.Normalize(facing) * Cube.CUBE_SCALE * 15, Cube.CUBE_SCALE * 10, visStats, stats),
					new Rectangle3D(new Vector3(-Cube.CUBE_SCALE / 10f), new Vector3(Cube.CUBE_SCALE / 5f)));
				if (projectile != -1)
				{
					itemCooldownTime = 1.125f;
					inventory.Remove(ammoIndex, 1);
					return true;
				}
			}

			itemCooldownTime = 0;
			return false;
		}
	}
}
