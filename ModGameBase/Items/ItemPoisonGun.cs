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
using ViMG.Buffs;
using ViMG.Cubes;
using ViMG.Entities;
using ViMG.Rendering;

namespace ViMG.Items
{
    public class ItemPoisonGun : Item
	{
		private static AttackStats attackStats = new AttackStats(DamageType.Ranged, 1.125f, 1, 1f);

		private static Buff.BuffInstance[] applyBuffs;

		private static ProjectileManager.ProjectileStats stats;

		public ItemPoisonGun() : base("poison_gun")
        {
            Client = new ClientItem(this, new RectangleF(112, 128, 16, 16), flipXInHand: true);

            name = "Poison Gun";
			//TODO normal bullets if not musketball
            description = attackStats.GetTooltip() + 
				"Musketballs are converted into gobs of poison, which inflict the poisoned debuff on enemies.";
        }

		public override bool LeftClick(Player player, Inventory inventory, int index, Vector3 facing, out ActionStats actionStats)
		{
			if (applyBuffs == null)
			{
				applyBuffs = new Buff.BuffInstance[1]
				{
					new Buff.BuffInstance(GlobalState.Registry.BuffRegistry.Get("poisoned"), 10)
				};

				stats = new ProjectileManager.ProjectileStats(HitboxManager.Group.PLAYER_DEAL, 1, 1f,
				   Cube.CUBE_SCALE * 0.25f, Cube.CUBE_SCALE, 1, false, 0, true, applyBuffs);
			}

			if (inventory.FindTag("ammo_bullet", out int ammoIndex).valid)
			{
				actionStats = new ActionStats(attackStats);
				int damage = attackStats.damage;
				float knockback = attackStats.knockback;
				player.PerformAttack(DamageType.Ranged, ref actionStats, ref damage, ref knockback);
				stats.damage = damage;
				stats.knockback = knockback;

				player.GetWorld().ProjectileManager.Add(new ProjectileManager.Projectile(player, player.Position,
					Vector3.Normalize(facing) * Cube.CUBE_SCALE * 15, Cube.CUBE_SCALE * 10, GlobalState.Registry.ProjectileRegistry.Get("musketball").Id, stats, index),
					new Rectangle3D(new Vector3(-Cube.CUBE_SCALE / 10f), new Vector3(Cube.CUBE_SCALE / 5f)));
				
				inventory.Remove(ammoIndex, 1);
				return true;
			}

			actionStats = new ActionStats();
			return false;
		}
	}
}
