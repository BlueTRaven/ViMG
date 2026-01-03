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
    public class CubeSeaStone : Cube
    {
        public CubeSeaStone() : base("stone_sea", 3, 2)
        {
            Name = "Sea Stone";
            Description = "Smooth and oddly wet to the touch. It emits a faint smell not unlike seawater, while not tasting in the least bit salty.";

            Client = new(this, new RectangleF(144, 96, 16, 16), Color.White);
        }

        public override void GetDrops(List<ItemInstance> itemsToDrop)
        {
            base.GetDrops(itemsToDrop);

            DropSelf(itemsToDrop);
        }
    }
}
