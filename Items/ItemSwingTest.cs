using BrUtility;
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
    public class ItemSwingTest : Item
    {
        public ItemSwingTest() : base("swing_test", StaticMaterials.Items, new RectangleF(48, 0, 16, 16))
        {
            name = "Swing Test";
        }

        public override bool LeftClick(Player player, Inventory inventory, int index, Vector3 facing, out ActionStats actionStats)
        {
            base.LeftClick(player, inventory, index, facing, out actionStats);

            actionStats.useTime = 0.5f;
            actionStats.useAnimTime = 0.5f;

            actionStats.animationType = UseAnimationType.SwingHorizontal;

            return true;
        }
    }
}
