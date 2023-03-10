using BrUtility;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViMG.Cubes
{
    public class CubeBonepile : Cube
    {
        public CubeBonepile() : base("bonepile", new RectangleF(64, 48, 32, 16), Color.White, 4, 0)
        {
            Transparency = TransparencyValue.Transparent;

            Name = "Bone Pile";
            Description = "A motley pile of bones.";
        }

        public override bool ShouldMeshPass(RenderPass pass)
        {
            return pass == RenderPass.Opaque;
        }

        public override void MakeCubeVerts(RenderPass pass, World world, ChunkMesher.CubeMeshingParameters parameters, List<VertexCube> vertices, List<int> indices)
        {
            parameters.positionWS += new Vector3(CUBE_SCALE / 2f, 0, CUBE_SCALE / 2f);
            DrawHelper3D.MakeXMeshVerts(pass, world, parameters, new Vector3(2f, 1f, 2f), vertices, indices);
        }
    }
}
