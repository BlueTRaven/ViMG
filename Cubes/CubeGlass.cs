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
    public class CubeGlass : Cube
    {
        public CubeGlass() : base("Glass", RectangleF.Empty, Color.White, 4)
        {
            Transparency = TransparencyValue.TransparentOccludesSiblings;
        }

        public override void MakeVerts(RenderPass pass, World world, Vector3 pos, Vector3 min, Vector3 max, CubeVisualInstance visual, Cube cube, List<VertexCube> vertices, List<int> indices)
        {
            if (pass == RenderPass.Transparent)
                ChunkMesher.MakeCubeVerts(pass, world, CubePosition.FromWorldSpace(pos), min, max, visual, cube, vertices, indices);
        }

        public override RectangleF GetSourceRect(RenderPass pass, World world, CubePosition pos)
        {
            if (pass == RenderPass.Transparent)
                return new RectangleF(0, 32, 16, 16);
            //else if (pass == RenderPass.Transparent)
                //return new RectangleF(16, 32, 16, 16);

            return new RectangleF(24, 36, 0, 0);
        }

        public override RectangleF GetSourceRect(RenderPass pass, World world, CubePosition pos, MeshHelper.CubeFace face)
        {
            return this.GetSourceRect(pass, world, pos);
        }

        public override void GetDrops(List<ItemInstance> itemsToDrop)
        {
            base.GetDrops(itemsToDrop);

            DropSelf(itemsToDrop);
        }
    }
}
