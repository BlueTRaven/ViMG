using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Cubes;
using ViMG.Entities.Renderers;

namespace ViMG.Items
{
    public class ItemLantern : Item
    {
        private int light = -1;
        private bool shadowmapped;
        private static Vector4 color;

        static ItemLantern()
        {
            color = Color.Orange.ToVector4();
            color.W = 1.5f;
        }

        public ItemLantern() : base("lantern", StaticMaterials.Items, new RectangleF(32, 64, 16, 16))
        {
        }

        public override void Hold(Player player, Inventory inventory, int index)
        {
            base.Hold(player, inventory, index);

            if (light == -1)
            {
                player.world.LightManager.AddShadowmapped(player.Position, Cube.CUBE_SCALE * 4, Cube.CUBE_SCALE * 16, color, out light, out shadowmapped);
            }
            else
            {
                if (shadowmapped)
                {
                    player.world.LightManager.UpdateShadowmapped(light, player.Position, Cube.CUBE_SCALE * 4, Cube.CUBE_SCALE * 16, color, true);
                }
                else
                {
                    player.world.LightManager.Update(light, player.Position, Cube.CUBE_SCALE * 4, Cube.CUBE_SCALE * 16, color);
                }
            }

            /*if (light != -1)
            {
                player.GetWorld().LightManager.Remove(light);
                light = -1;
            }

            light = player.GetWorld().LightManager.Add(player.Position, Cube.CUBE_SCALE * 4, Cube.CUBE_SCALE * 8, Color.Orange.ToVector4());*/
        }

        public override void EndHold(Player player, Inventory inventory, int newIndex)
        {
            base.EndHold(player, inventory, newIndex);

            if (light != -1)
            {
                if (shadowmapped)
                    player.GetWorld().LightManager.RemoveShadowmapped(light);
                else player.GetWorld().LightManager.Remove(light);
                light = -1;
            }
        }
    }
}
