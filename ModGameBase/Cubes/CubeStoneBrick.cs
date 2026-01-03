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
    public class CubeStoneBrick : Cube
    {
        public CubeStoneBrick() : base("brick_stone", 6)
        {
            Transparency = TransparencyValue.Opaque;

            Client = new(this, new RectangleF(112, 0, 16, 16), Color.White);
        }

        public override void GetDrops(List<ItemInstance> itemsToDrop)
        {
            base.GetDrops(itemsToDrop);

            DropSelf(itemsToDrop);
        }
    }
}
