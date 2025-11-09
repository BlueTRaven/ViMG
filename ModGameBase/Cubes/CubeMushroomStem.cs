using BrUtility;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Items;

namespace ViMG.Cubes
{
    public class CubeMushroomStem : Cube
    {
        public CubeMushroomStem() : base("mushroom_stem", new CubeFacingLayout(new RectangleF(32, 96, 16, 16), new RectangleF(64, 96, 16, 16)), Color.White, 4)
        {
            Name = "Mushroom Stem";
        }

        public override void GetDrops(List<ItemInstance> itemsToDrop)
        {
            base.GetDrops(itemsToDrop);

            DropSelf(itemsToDrop);
        }
    }
}
