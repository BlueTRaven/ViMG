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
        private readonly RectangleF sourceRect;
        private readonly Color color;

        public CubeDebug(string identifier, RectangleF sourceRect, Color color, int mineProgressRequirement) : base(identifier, mineProgressRequirement)
        {
            this.sourceRect = sourceRect;
            this.color = color;
        }

        public override ClientCube ClientInit()
        {
            return new(this, sourceRect, color);
        }

        public override void GetDrops(List<ItemInstance> itemsToDrop)
        {
            base.GetDrops(itemsToDrop);

            DropSelf(itemsToDrop);
        }
    }
}
