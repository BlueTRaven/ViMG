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
    public class CubeDebug : Cube
    {
        public CubeDebug(string identifier, RectangleF sourceRect, Color color, int mineProgressRequirement) : base(identifier, mineProgressRequirement)
        {
            Client = new(this, sourceRect, color);
        }

        public override void GetDrops(List<ItemInstance> itemsToDrop)
        {
            base.GetDrops(itemsToDrop);

            DropSelf(itemsToDrop);
        }
    }
}
