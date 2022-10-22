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
    public class ItemBookWinds : Item
    {
        private static MagicAttackStats magicStats = new MagicAttackStats(new AttackStats(Player.DamageType.Magic, 2f, 0, 8), 2);

        public ItemBookWinds() : base("book_spell_winds", Main.assetsManager.GetAsset<Texture2D>("swrod"), new RectangleF(80, 32, 16, 16))
        {
            name = "Spellbook: Winds";
            description = "A spellbook with an explanation of how to cast \"Winds\".\n" +
                "Press LMB to use.\n" +
                magicStats.GetTooltip() +
                "Creates a powerful gust of wind, knocking enemies away.\n" +
                "Magic Cost: 2";

            flipXInHand = true;
        }

        public override bool LeftClick(Player player, Inventory inventory, int index, Vector3 facing, out float itemCooldownTime)
        {
            bool val = base.LeftClick(player, inventory, index, facing, out itemCooldownTime);

            if (player.Magic < 2)
                return false;

            itemCooldownTime = magicStats.attackStats.cooldownTime;
            int damage = magicStats.attackStats.damage;
            float knockback = magicStats.attackStats.knockback;
            player.PerformAttack(Player.DamageType.Magic, ref itemCooldownTime, ref damage, ref knockback);

            player.Magic -= 2;
            player.SpawnHitbox(damage, Player.DamageType.Magic, -Main.camera.ForwardYawOnly, knockback, Cube.CUBE_SCALE * 2f);
            
            return val;
        }
    }
}
