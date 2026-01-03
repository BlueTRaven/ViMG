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
    public class CubeBundledWood : Cube
    {
        public CubeBundledWood() : base("bundled_wood", 4)
        {
            Client = new(this, new CubeFacingLayout(new RectangleF(48, 32, 16, 16), new RectangleF(48, 16, 16, 16)),
            Color.White);
        }

        public override void GetDrops(List<ItemInstance> itemsToDrop)
        {
            base.GetDrops(itemsToDrop);

            DropSelf(itemsToDrop);
        }
    }
}
