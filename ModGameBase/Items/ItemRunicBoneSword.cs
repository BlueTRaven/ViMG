using BrUtility;
using Engine;
using Engine.Entities;
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
    public class ItemRunicBoneSword : Item
    {
        private static ProjectileManager.ProjectileStats stats;
        private static ProjectileManager.ProjectileVisStats visStats;
        private static ProjectileManager.ProjectileBatchStats batchStats;
        private static MeleeAttackStats meleeStats = 
            new MeleeAttackStats(new AttackStats(DamageType.Melee, new ActionStats() 
            {
                useTime = 1.85f,
                useAnimTime = 20f / 60f,
                preUseTime = 12f / 60f
            }, 16, Cube.CUBE_SCALE * 1.25f), Cube.CUBE_SCALE * 1.75f);

        public ItemRunicBoneSword() : base("sword_runic_bone")
        {
            name = "Runic Bone Sword";
            description = "A massive sword intricately carved in bone. Despite being made of such a brittle material, it cuts just as well as any other sword - perhaps even better.\n" +
                meleeStats.GetTooltip() +
                "Hitting enemies results in a small explosion of bones.";

            batchStats = new ProjectileManager.ProjectileBatchStats(4, new Vector2(-180, 180), new Vector2(-45, 45));

            stats = new ProjectileManager.ProjectileStats(
                HitboxManager.Group.PLAYER_DEAL, 4, 1f, Cube.CUBE_SCALE / 8, Cube.CUBE_SCALE, 1, true, 1f, true);
        }

        protected override ClientItem ClientInit()
        {
            return new ClientItem(this, new RectangleF(176, 112, 32, 32), scale: 2f);
        }

        public override bool LeftClick(Player player, Inventory inventory, int index, Vector3 facing, out ActionStats actionStats)
        {
            base.LeftClick(player, inventory, index, facing, out actionStats);

            actionStats = new ActionStats(meleeStats.attackStats);
            int damage = meleeStats.attackStats.damage;
            float knockback = meleeStats.attackStats.knockback;
            player.PerformAttack(DamageType.Melee, ref actionStats, ref damage, ref knockback);

            player.SpawnHitboxLater(index, damage, DamageType.Melee, -(player as IRotatable).Forward, knockback, meleeStats.range);

            actionStats.animationType = UseAnimationType.SwingHorizontal;

            return true;
        }

        public override void OnDealDamage(Player player, Inventory inventory, int index, HitboxManager.Hitbox otherHitbox)
        {
            base.OnDealDamage(player, inventory, index, otherHitbox);

            player.world.ProjectileManager.AddBatch(player, new Vector3(otherHitbox.bounds.Center.X,
                otherHitbox.bounds.Top, otherHitbox.bounds.Center.Z), Vector3.Up * Cube.CUBE_SCALE * 8, 4,
                batchStats, GlobalState.Registry.ProjectileRegistry.Get("bone").Id, stats);
        }
    }
}
