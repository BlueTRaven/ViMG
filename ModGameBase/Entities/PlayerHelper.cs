using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using ViMG.Items;

namespace ViMG.Entities
{
    public static class PlayerHelper
    {
        public static void MeleeWeaponLeftClick(Player player, int inventorySlot, Items.Item.MeleeAttackStats attackStats, out ActionStats actionStats)
        {
            actionStats = new ActionStats(attackStats.attackStats);
            int damage = attackStats.attackStats.damage;
            float knockback = attackStats.attackStats.knockback;
            player.PerformAttack(DamageType.Melee, ref actionStats, ref damage, ref knockback);

            player.SpawnHitboxLater(inventorySlot, damage, DamageType.Melee, -Main.camera.Forward, knockback, attackStats.range);

            actionStats.animationType = UseAnimationType.SwingHorizontal;
        }
    }
}
