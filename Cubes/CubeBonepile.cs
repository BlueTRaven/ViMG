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

        public override void MakeVerts(RenderPass pass, World world, Vector3 pos, Vector3 min, Vector3 max, MeshHelper.CubeFace faces, List<VertexCube> vertices, List<int> indices)
        {
            if (pass == RenderPass.Opaque)
            {
                DrawHelper3D.MakeXMeshVerts(pass, this, world, pos + new Vector3(CUBE_SCALE / 2f, 0, CUBE_SCALE / 2f), new Vector3(2f, 1f, 2f), vertices, indices);
            }
        }
    }
}
