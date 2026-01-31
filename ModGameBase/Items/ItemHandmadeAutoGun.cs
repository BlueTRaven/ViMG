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
    public class ItemHandmadeAutoGun : Item
    {
        private static AttackStats attackStats = new AttackStats(DamageType.Ranged, 0.8f, 1, 1);

        private ProjectileManager.ProjectileBatchStats batchStats = new ProjectileManager.ProjectileBatchStats(2, 0, 0);
        private ProjectileManager.ProjectileVisStats visStats = new ProjectileManager.ProjectileVisStats(new RectangleF(16, 0, 16, 16), Cube.CUBE_SCALE);
        private ProjectileManager.ProjectileStats stats = new ProjectileManager.ProjectileStats(HitboxManager.Group.PLAYER_DEAL, 1, 1f,
            Cube.CUBE_SCALE * 0.5f, Cube.CUBE_SCALE, 1, false, 0, true);

        public ItemHandmadeAutoGun() : base("handmade_autogun")
        {
            name = "Handmade Automatic Gun";
            description = "May or may not blow up in your face. But hey, it fires pretty fast. Consumes two ammo per shot.\n" +
                attackStats.GetTooltip();
        }

        protected override ClientItem ClientInit()
        {
            return new ClientItem(this, new RectangleF(96, 128, 16, 16), flipXInHand: true);
        }

        public override bool LeftClick(Player player, Inventory inventory, int index, Vector3 facing, out ActionStats actionStats)
        {
            ItemInstance ammo = inventory.FindTag("ammo_bullet", out int ammoIndex);

            if (ammo.valid && ammo.num >= 2)
            {
                actionStats = new ActionStats(attackStats);
                int damage = attackStats.damage;
                float knockback = attackStats.knockback;
                player.PerformAttack(DamageType.Ranged, ref actionStats, ref damage, ref knockback);
                stats.damage = damage;
                stats.knockback = knockback;

                Vector3 direction = Vector3.Normalize(facing) * Cube.CUBE_SCALE * 26;
                batchStats.spacingYaw = GlobalState.random.NextFloat(-15, 15);
                batchStats.spacingPitch = GlobalState.random.NextFloat(-7.5f, 7.5f);

                player.GetWorld().ProjectileManager.AddBatch(player, player.Position, direction, 1.5f, batchStats, GlobalState.Registry.ProjectileRegistry.Get("musketball").Id, stats, index);

                inventory.Remove(ammoIndex, 2);
                return true;
            }

            actionStats = new ActionStats();
            return false;
        }
    }
}
