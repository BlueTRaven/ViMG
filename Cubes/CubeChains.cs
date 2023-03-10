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

        public override RectangleF GetSourceRect(RenderPass pass, World world, ChunkMesher.CubeMeshingParameters parameters)
        {
            Cube aboveCube = world.ChunkManager.ThreadedView.GetCube(parameters.position + new CubePosition(0, 1, 0)).GetOrDefault(Main.Registry.CubeRegistry.Air);
            Cube belowCube = world.ChunkManager.ThreadedView.GetCube(parameters.position - new CubePosition(0, 1, 0)).GetOrDefault(Main.Registry.CubeRegistry.Air);
            //if it's solid, we're hanging from the ceiling. Use the top-attached sourceRect.
            if (aboveCube != this && aboveCube.Solid)
                return new RectangleF(80, 64, 16, 16);
            else if (!belowCube.Solid)
                return new RectangleF(96, 64, 16, 16);
            else return new RectangleF(64, 64, 16, 16);
        }

        public override bool ShouldMeshPass(RenderPass pass)
        {
            return pass == RenderPass.Opaque;
        }

        public override void MakeCubeVerts(RenderPass pass, World world, ChunkMesher.CubeMeshingParameters parameters, List<VertexCube> vertices, List<int> indices)
        {
            parameters.positionWS += new Vector3(CUBE_SCALE / 2f, 0, CUBE_SCALE / 2f);
            DrawHelper3D.MakeXMeshVerts(pass, world, parameters, Vector3.One, vertices, indices);
        }
    }
}
