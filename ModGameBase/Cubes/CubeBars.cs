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
    public class CubeBars : Cube
    {
        public CubeBars() : base("bars", 7)
        {
            Transparency = TransparencyValue.Transparent;
        }

        public override ClientCube ClientInit()
        {
            return new(this, new RectangleF(128, 0, 16, 16), Color.White);
        }

        public override void GetDrops(List<ItemInstance> itemsToDrop)
        {
            base.GetDrops(itemsToDrop);

            DropSelf(itemsToDrop);
        }
    }
}
