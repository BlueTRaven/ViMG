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
    public class CubeWoodPlatform : Cube
    {
        public CubeWoodPlatform() : base("wood_platform", new CubeFacingLayout(new RectangleF(112, 48, 16, 16), new RectangleF(96, 48, 16, 16)), Color.White, 2, 0)
        {
            Name = "Wooden Platform";
            Description = "Press LCtrl to pass through this block while standing on it. Not solid from the bottom or sides.";

            Transparency = TransparencyValue.TransparentOccludesSiblings;
            Collision = CollisionValue.Platform;
        }

        public override void GetDrops(List<ItemInstance> itemsToDrop)
        {
            base.GetDrops(itemsToDrop);

            DropSelf(itemsToDrop);
        }
    }
}
