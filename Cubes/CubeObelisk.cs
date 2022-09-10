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
    public class CubeObelisk : Cube
    {
        public CubeObelisk() : base("obelisk", new CubeFacingLayout(new RectangleF(64, 32, 16, 16), new RectangleF(64, 16, 16, 16)), Color.White, -1)
        {
        }

        public override void GetDrops(List<ItemInstance> itemsToDrop)
        {
            base.GetDrops(itemsToDrop);

            DropSelf(itemsToDrop);
        }
    }
}
