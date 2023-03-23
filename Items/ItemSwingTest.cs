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
    public class ItemSwingTest : Item
    {
        public ItemSwingTest() : base("swing_test", Main.assetsManager.GetAsset<Texture2D>("swrod"), new RectangleF(48, 0, 16, 16))
        {
            name = "Swing Test";
        }

        public override bool LeftClick(Player player, Inventory inventory, int index, Vector3 facing, out Player.ActionStats actionStats)
        {
            base.LeftClick(player, inventory, index, facing, out actionStats);

            actionStats.useTime = 0.5f;
            actionStats.useAnimTime = 0.5f;
            int damage = 1;
            float knockback = Cube.CUBE_SCALE;
            player.PerformAttack(Player.DamageType.Melee, ref actionStats, ref damage, ref knockback);

            player.SpawnHitboxLater(index, damage, Player.DamageType.Melee, -Main.camera.Forward, knockback, Cube.CUBE_SCALE * 4f);

            return true;
        }
    }
}
