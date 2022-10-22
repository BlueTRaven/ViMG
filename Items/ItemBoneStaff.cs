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
    public class ItemBoneStaff : Item
    {
        private static MagicAttackStats magicStats = new MagicAttackStats(new AttackStats(0.68f, 6, 1), 3);

        private ProjectileManager.ProjectileVisStats visStats;
        private ProjectileManager.ProjectileStats stats;

        public ItemBoneStaff() : base("magic_bone_staff", Main.assetsManager.GetAsset<Texture2D>("swrod"), new RectangleF(208, 124, 19, 20))
        {
            name = "Runic Bone Staff";
            description = "A staff crafted from finely-carved bone.\n" +
                magicStats.GetTooltip();

            visStats = new ProjectileManager.ProjectileVisStats(Main.assetsManager.GetAsset<Texture2D>("skullhead"), new RectangleF(80, 128, 32, 32), Cube.CUBE_SCALE);
            stats = new ProjectileManager.ProjectileStats(HitboxManager.Group.PLAYER_DEAL, magicStats.attackStats.damage,
                Cube.CUBE_SCALE / 2f, Cube.CUBE_SCALE);
        }

        public override bool LeftClick(Player player, Inventory inventory, int index, Vector3 facing, out float itemCooldownTime)
        {
            if (magicStats.CanUse(player))
            {
                player.GetWorld().ProjectileManager.Add(new ProjectileManager.Projectile(player, player.Position,
                    Vector3.Normalize(facing) * Cube.CUBE_SCALE * 6f, Cube.CUBE_SCALE * 10, visStats, stats),
                    new Rectangle3D(new Vector3(-Cube.CUBE_SCALE / 10f), new Vector3(Cube.CUBE_SCALE / 5f)));

                magicStats.Use(player);

                itemCooldownTime = magicStats.attackStats.cooldownTime;
                player.PerformAttack(Player.PlayerDamageType.Magic, ref itemCooldownTime);
                return true;
            }

            itemCooldownTime = 0;
            return false;
        }
    }
}
