using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Cubes;
using ViMG.Entities;

namespace ViMG.Items
{
    public class ItemRunicBoneSword : Item
    {
        private static ProjectileManager.ProjectileStats stats;
        private static ProjectileManager.ProjectileVisStats visStats;
        private static ProjectileManager.ProjectileBatchStats batchStats;
        private static MeleeAttackStats meleeStats = 
            new MeleeAttackStats(new AttackStats(Player.DamageType.Melee, 1.85f, 16, Cube.CUBE_SCALE * 1.25f), Cube.CUBE_SCALE * 1.75f);

        public ItemRunicBoneSword() : base("sword_runic_bone", Main.assetsManager.GetAsset<Texture2D>("swrod"), new RectangleF(176, 112, 32, 32))
        {
            name = "Runic Bone Sword";
            description = "A massive sword intricately carved in bone. Despite being made of such a brittle material, it cuts just as well as any other sword - perhaps even better.\n" +
                meleeStats.GetTooltip() +
                "Hitting enemies results in a small explosion of bones.";

            scale = 2f;

            batchStats = new ProjectileManager.ProjectileBatchStats(4, new Vector2(-180, 180), new Vector2(-45, 45));

            stats = new ProjectileManager.ProjectileStats(
                HitboxManager.Group.PLAYER_DEAL, 4, 1f, Cube.CUBE_SCALE / 8, Cube.CUBE_SCALE, 1, true, 1f, true);
            visStats = new ProjectileManager.ProjectileVisStats(Main.assetsManager.GetAsset<Texture2D>("projectiles"),
                new RectangleF(48, 0, 16, 16), Cube.CUBE_SCALE / 3f);
        }

        public override bool LeftClick(Player player, Inventory inventory, int index, Vector3 facing, out Player.ActionStats actionStats)
        {
            base.LeftClick(player, inventory, index, facing, out actionStats);

            actionStats = new Player.ActionStats(meleeStats.attackStats);
            int damage = meleeStats.attackStats.damage;
            float knockback = meleeStats.attackStats.knockback;
            player.PerformAttack(Player.DamageType.Melee, ref actionStats, ref damage, ref knockback);

            player.SpawnHitbox(index, damage, Player.DamageType.Melee, -Main.camera.Forward, knockback, meleeStats.range);

            return true;
        }

        public override void OnDealDamage(Player player, Inventory inventory, int index, HitboxManager.Hitbox otherHitbox)
        {
            base.OnDealDamage(player, inventory, index, otherHitbox);

            player.world.ProjectileManager.AddBatch(player, new Vector3(otherHitbox.bounds.Position.X + otherHitbox.bounds.Size.X / 2f,
                otherHitbox.bounds.Top, otherHitbox.bounds.Position.Z + otherHitbox.bounds.Size.Z / 2f), Vector3.Up * Cube.CUBE_SCALE * 8, 4,
                batchStats, visStats, stats,
                new Rectangle3D(-new Vector3(Cube.CUBE_SCALE / 4), new Vector3(Cube.CUBE_SCALE / 2)));
        }
    }
}
