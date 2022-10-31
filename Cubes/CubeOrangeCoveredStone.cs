using BrUtility;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViMG.Cubes
{
    public class CubeOrangeCoveredStone : Cube
    {
        public CubeOrangeCoveredStone() : base("stone_covered_orange", new CubeFacingLayout(new RectangleF(0, 96, 16, 16), new RectangleF(16, 96, 16, 16), new RectangleF(16, 0, 16, 16)), Color.White, 3)
        {
            Name = "Orange Mushroom Covered Stone";
        }

        public override RectangleF GetSourceRect(RenderPass pass, World world, CubePosition pos, MeshHelper.CubeFace face)
        {
            if (world == null)
                return base.GetSourceRect(pass, world, pos, face);

            //we're meshing one of the sides.
            if ((face & MeshHelper.CubeFace.SIDES) > 0)
            {
                //if the cube above is the same
                if (world.ChunkManager.GetCube(new CubePosition(pos.X, pos.Y + 1, pos.Z)).GetOrDefault(Main.Registry.CubeRegistry.Air) == this)
                {
                    //use the stone texture for the sides
                    return new RectangleF(16, 0, 16, 16);
                }
            }

            return base.GetSourceRect(pass, world, pos, face);
        }
    }
}
