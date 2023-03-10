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
        public CubeGlass() : base("glass", RectangleF.Empty, Color.White, 4)
        {
            Transparency = TransparencyValue.TransparentOccludesSiblings;
        }

        public override void MakeCubeVerts(RenderPass pass, World world, ChunkMesher.CubeMeshingParameters parameters, List<VertexCube> vertices, List<int> indices)
        {
            if (pass == RenderPass.Transparent)
                base.MakeCubeVerts(pass, world, parameters, vertices, indices);
        }

        public override RectangleF GetSourceRect(RenderPass pass, World world, ChunkMesher.CubeMeshingParameters parameters)
        {
            if (pass == RenderPass.Transparent)
                return new RectangleF(0, 32, 16, 16);
            //else if (pass == RenderPass.Transparent)
                //return new RectangleF(16, 32, 16, 16);

            return new RectangleF(24, 36, 0, 0);
        }

        public override void GetDrops(List<ItemInstance> itemsToDrop)
        {
            base.GetDrops(itemsToDrop);

            DropSelf(itemsToDrop);
        }
    }
}
