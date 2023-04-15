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
using ViMG.Rendering;

namespace ViMG.Items
{
    public class ItemBookLavaSpout : Item
    {
        private ProjectileManager.ProjectileVisStats visStats = new ProjectileManager.ProjectileVisStats(new RectangleF(48, 16, 16, 16), Cube.CUBE_SCALE);
        private ProjectileManager.ProjectileStats stats = new ProjectileManager.ProjectileStats(HitboxManager.Group.PLAYER_DEAL, 2, 1f,
                Cube.CUBE_SCALE * 0.25f, Cube.CUBE_SCALE, 1, true, 1, true);

        private static MagicAttackStats magicStats = new MagicAttackStats(new AttackStats(Player.DamageType.Magic, 4f / 60f, 4, 1f), 1);

        public ItemBookLavaSpout() : base("book_spell_lava_spout", StaticMaterials.Items, new RectangleF(144, 96, 16, 16))
        {
            name = "Spellbook: Lava Spout";
            description = "The book's pages erupt into lava.\n" +
                magicStats.GetTooltip();
        }

        public override bool LeftClick(Player player, Inventory inventory, int index, Vector3 facing, out Player.ActionStats actionStats)
        {
            if (magicStats.CanUse(player))
            {
                magicStats.Use(player);
                actionStats = new Player.ActionStats(magicStats.attackStats);
                int damage = magicStats.attackStats.damage;
                float knockback = magicStats.attackStats.knockback;
                player.PerformAttack(Player.DamageType.Magic, ref actionStats, ref damage, ref knockback);
                stats.damage = damage;
                stats.knockback = knockback;

                player.GetWorld().ProjectileManager.Add(new ProjectileManager.Projectile(player, player.Position + Vector3.Normalize(facing) * (Cube.CUBE_SCALE / 3f),
                    Vector3.Normalize(facing) * Cube.CUBE_SCALE * 15f, Cube.CUBE_SCALE * 10, visStats, stats, index),
                    new Rectangle3D(new Vector3(-Cube.CUBE_SCALE / 10f), new Vector3(Cube.CUBE_SCALE / 5f)));

                return true;
            }

            actionStats = new Player.ActionStats();
            return false;
        }
    }
}
