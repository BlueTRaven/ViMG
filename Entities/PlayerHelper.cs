using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ViMG.Entities
{
    public static class PlayerHelper
    {
        public static void MeleeWeaponLeftClick(Player player, int inventorySlot, Items.Item.MeleeAttackStats attackStats, out Player.ActionStats actionStats)
        {
            actionStats = new Player.ActionStats(attackStats.attackStats);
            int damage = attackStats.attackStats.damage;
            float knockback = attackStats.attackStats.knockback;
            player.PerformAttack(Player.DamageType.Melee, ref actionStats, ref damage, ref knockback);

            player.SpawnHitboxLater(inventorySlot, damage, Player.DamageType.Melee, -Main.camera.Forward, knockback, attackStats.range);

            actionStats.animationType = Player.UseAnimationType.SwingHorizontal;
        }
    }
}
