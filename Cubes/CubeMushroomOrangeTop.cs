using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BrUtility;
using ViMG.Items;

namespace ViMG.Cubes
{
    public class CubeMushroomOrangeTop : Cube
    {
        public CubeMushroomOrangeTop() : base("mushroom_orange_top", new CubeFacingLayout(new RectangleF(80, 96, 16, 16), new RectangleF(48, 96, 16, 16), new RectangleF(64, 96, 16, 16)), Color.White, 4)
        {
            Name = "Orange Mushroom Top";
        }

        public override void GetDrops(List<ItemInstance> itemsToDrop)
        {
            base.GetDrops(itemsToDrop);

            DropSelf(itemsToDrop);
        }

        public override RectangleF GetSourceRect(RenderPass pass, World world, CubePosition pos, MeshHelper.CubeFace face)
        {
            if (world == null)
                return base.GetSourceRect(pass, world, pos, face);

            //we're meshing one of the sides.
            if ((face & MeshHelper.CubeFace.SIDES) > 0)
            {
                //if the cube above is a mushroom block
                if (world.ChunkManager.GetCube(new CubePosition(pos.X, pos.Y - 1, pos.Z)).GetOrDefault(Main.Registry.CubeRegistry.Air) == this)
                {
                    //use the same top texture instead of the ordinary side texture.
                    return new RectangleF(48, 96, 16, 16);
                }
            }

            return base.GetSourceRect(pass, world, pos, face);
        }
    }
}
