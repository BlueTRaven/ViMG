using BrUtility;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViMG.Cubes
{
    public class CubeChains : Cube
    {
        public CubeChains() : base("ceiling_chains", new RectangleF(80, 64, 16, 16), Color.White, 10)
        {
            Transparency = TransparencyValue.Transparent;
            Collision = CollisionValue.Rope;

            Name = "Rusted Steel Chains";
        }

        public override RectangleF GetSourceRect(RenderPass pass, World world, CubePosition pos)
        {
            Cube aboveCube = world.GetChunkManager().GetCube(pos + new CubePosition(0, 1, 0)).GetOrDefault(Main.Registry.CubeRegistry.Air);
            Cube belowCube = world.GetChunkManager().GetCube(pos - new CubePosition(0, 1, 0)).GetOrDefault(Main.Registry.CubeRegistry.Air);
            //if it's solid, we're hanging from the ceiling. Use the top-attached sourceRect.
            if (aboveCube != this && aboveCube.Solid)
                return new RectangleF(80, 64, 16, 16);
            else if (!belowCube.Solid)
                return new RectangleF(96, 64, 16, 16);
            else return new RectangleF(64, 64, 16, 16);
        }

        public override void MakeVerts(RenderPass pass, World world, Vector3 pos, Vector3 min, Vector3 max, CubeVisualInstance visual, List<VertexCube> vertices, List<int> indices)
        {
            if (pass == RenderPass.Opaque)
                DrawHelper3D.MakeXMeshVerts(pass, this, world, pos + new Vector3(CUBE_SCALE / 2f, 0, CUBE_SCALE / 2f), Vector3.One, vertices, indices);
        }
    }
}
