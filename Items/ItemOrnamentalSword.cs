using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Cubes;

namespace ViMG.Items
{
    public class ItemOrnamentalSword : Item
    {
        private static MeleeAttackStats meleeStats = new MeleeAttackStats(new AttackStats(Player.DamageType.Melee, 1.5f, 8, 2f), Cube.CUBE_SCALE * 2.5f);

        public ItemOrnamentalSword() : base("sword_ornamental", Main.assetsManager.GetAsset<Texture2D>("swrod"), new RectangleF(64, 128, 16, 16))
        {
            name = "Ornamental Sword";
            description = "A large sword that looks fancy but in reality is pretty flimsy.\n" +
                meleeStats.GetTooltip();

            scale = 1f;
        }

        public override bool LeftClick(Player player, Inventory inventory, int index, Vector3 facing, out float itemCooldownTime)
        {
            base.LeftClick(player, inventory, index, facing, out itemCooldownTime);

            itemCooldownTime = meleeStats.attackStats.cooldownTime;
            int damage = meleeStats.attackStats.damage;
            float knockback = meleeStats.attackStats.knockback;
            player.PerformAttack(Player.DamageType.Melee, ref itemCooldownTime, ref damage, ref knockback);

            player.SpawnHitbox(index, damage, Player.DamageType.Melee, -Main.camera.Forward, knockback, meleeStats.range);

            return true;
        }
    }
}
