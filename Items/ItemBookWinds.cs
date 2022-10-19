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
        public ItemBookWinds() : base("book_spell_winds", Main.assetsManager.GetAsset<Texture2D>("swrod"), new RectangleF(80, 32, 16, 16))
        {
            name = "Spellbook: Winds";
            description = "A spellbook with an explanation of how to cast \"Winds\".\n" +
                "Press LMB to use.\n" +
                "This spell will create a powerful gust of wind in front of you, knocking enemies away.\n" +
                "Magic Cost: 2";

            flipXInHand = true;
        }

        public override bool LeftClick(Player player, Inventory inventory, int index, Vector3 facing, out float itemCooldownTime)
        {
            bool val = base.LeftClick(player, inventory, index, facing, out itemCooldownTime);

            if (player.Magic < 2)
                return false;

            player.Magic -= 2;
            player.SpawnHitbox(0, Player.PlayerDamageType.Magic, -Main.camera.ForwardYawOnly, 8f, Cube.CUBE_SCALE * 2f);
            itemCooldownTime = 2f;
            player.PerformAttack(Player.PlayerDamageType.Magic, ref itemCooldownTime);
            
            return val;
        }
    }
}
