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
    public class CubeSeaStuddedStone : Cube
    {
        public CubeSeaStuddedStone() : base("stone_sea_studded", new RectangleF(160, 96, 16, 16), Color.White, 3, 2)
        {
            Name = "Studded Sea Stone";
            Description = "Ordinary sea stone studded with vibrant aquamarine.";
        }

        public override void GetDrops(List<ItemInstance> itemsToDrop)
        {
            base.GetDrops(itemsToDrop);

            DropSelf(itemsToDrop);
        }
    }
}
