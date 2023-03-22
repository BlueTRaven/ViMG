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
    public class ItemFlaskMagicPotion1 : Item
    {
        public ItemFlaskMagicPotion1() : base("flask_magicpotion1", Main.assetsManager.GetAsset<Texture2D>("swrod"), new RectangleF(48, 96, 16, 16))
        {
            name = "Magic Potion 1";
            description = "A potion that restores magic power. It smells like chalk and tastes like it too.";
        }

        public override bool RightClick(Player player, Inventory inventory, int index, Vector3 facing, out Player.ActionStats actionStats)
        {
            player.Magic += 5;

            if (player.Magic > player.MaxMagic)
                player.Magic = player.MaxMagic;

            inventory.Remove(index, 1);

            return base.RightClick(player, inventory, index, facing, out actionStats);
        }
    }
}
