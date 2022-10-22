using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViMG.Items
{
    public class ItemRunicBoneSword : Item
    {
        private static AttackStats stats = new AttackStats(1.25f, 16, 1);

        public ItemRunicBoneSword() : base("sword_runic_bone", Main.assetsManager.GetAsset<Texture2D>("swrod"), new RectangleF(176, 112, 32, 32))
        {
            name = "Runic Bone Sword";
            description = "A massive sword intricately carved in bone. Despite being made of such a brittle material, it cuts just as well as any other sword - perhaps even better.\n" +
                stats.GetTooltip() +
                "Hitting enemies results in a small explosion of bones. (Unimplemented)";
        }

        public override bool LeftClick(Player player, Inventory inventory, int index, Vector3 facing, out float itemCooldownTime)
        {
            base.LeftClick(player, inventory, index, facing, out itemCooldownTime);

            player.SpawnHitbox(stats.damage, Player.PlayerDamageType.Melee, -Main.camera.Forward, stats.knockback);
            itemCooldownTime = stats.cooldownTime;
            player.PerformAttack(Player.PlayerDamageType.Melee, ref itemCooldownTime);

            return true;
        }

        public override void OnDealDamage(Player player, Inventory inventory, int index, IHitboxOwner hit)
        {
            base.OnDealDamage(player, inventory, index, hit);

            //TODO bone explosion
            //Batch spawn some projectiles in a random direction with +y velocity.
            //These projectiles should ignore the hit entity. (Will this already happen since the entity should be in invuln frames anyway?)
        }
    }
}
