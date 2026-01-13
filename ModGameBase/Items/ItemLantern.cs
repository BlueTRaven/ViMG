using BrUtility;
using Engine.Items;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Cubes;
using ViMG.Rendering;

namespace ViMG.Items
{
    public class ItemLantern : Item
    {
        private static Vector4 color;

        static ItemLantern()
        {
            color = Color.Orange.ToVector4();
            color.W = 1.5f;
        }

        public ItemLantern() : base("lantern", new RectangleF(32, 64, 16, 16))
        {
        }

        public override void Hold(Player player, Inventory inventory, int index)
        {
            base.Hold(player, inventory, index);

            player.world.LightManager2.AddShadowmapped(new Engine.Common.LightManager2.LightConfig
            {
                position = player.Position,
                min = Cube.CUBE_SCALE * 4,
                max = Cube.CUBE_SCALE * 16,
                color = new(color),
            });
        }

        public override void EndHold(Player player, Inventory inventory, int newIndex)
        {
            base.EndHold(player, inventory, newIndex);
        }
    }
}
