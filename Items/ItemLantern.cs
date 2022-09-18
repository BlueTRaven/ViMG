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
    public class ItemLantern : Item
    {
        private int light = -1;

        public ItemLantern() : base("lantern", Main.assetsManager.GetAsset<Texture2D>("swrod"), new RectangleF(32, 64, 16, 16))
        {
        }

        public override void Hold(Player player, Inventory inventory, int index)
        {
            base.Hold(player, inventory, index);

            if (light != -1)
            {
                player.GetWorld().LightManager.Remove(light);
                light = -1;
            }

            light = player.GetWorld().LightManager.Add(player.Position, Cube.CUBE_SCALE * 4, Cube.CUBE_SCALE * 8, Color.Orange);
        }

        public override void EndHold(Player player, Inventory inventory, int newIndex)
        {
            base.EndHold(player, inventory, newIndex);

            if (light != -1)
            {
                player.GetWorld().LightManager.Remove(light);
                light = -1;
            }
        }
    }
}
