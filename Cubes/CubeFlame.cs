using BrUtility;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Entities;
using ViMG.Items;

namespace ViMG.Cubes
{
    public class CubeFlame : Cube
    {
        public CubeFlame() : base("flame", new RectangleF(192, 16, 16, 16), Color.White, 1)
        {
            Transparency = TransparencyValue.Transparent;
            Collision = CollisionValue.None;
        }

        public override void MakeVerts(RenderPass pass, Vector3 pos, Vector3 min, Vector3 max, CubeVisualInstance visual, Cube cube, List<VertexCube> vertices, List<int> indices)
        {
			if (pass != RenderPass.Opaque)
				return;

            DrawHelper3D.MakeXMeshVerts(pass, cube, pos, vertices, indices);
        }

        public override void OnPlayerPlaced(Player player, CubePosition position)
        {
            base.OnPlayerPlaced(player, position);

            player.GetWorld().EntityManager.Add(new CubeLight(position, Color.OrangeRed.ToVector4(), new Vector2(Cube.CUBE_SCALE * 4, Cube.CUBE_SCALE * 8)));
        }

        public override CubeAnimation GetAnimation()
        {
            return new CubeAnimation(0.125f, 3);
        }
    }
}
