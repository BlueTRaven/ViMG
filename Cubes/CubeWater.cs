using BrUtility;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace ViMG.Cubes
{
	public class CubeWater : Cube
	{
		public CubeWater() : base("water", new RectangleF(0, 16, 16, 16), Color.White, -1)
		{
			Touchable = false;
			Transparency = TransparencyValue.TransparentOccludesSiblings;
			Collision = CollisionValue.None;
		}

        public override void MakeVerts(RenderPass pass, World world, Vector3 pos, Vector3 min, Vector3 max, CubeVisualInstance visual, Cube cube, List<VertexCube> vertices, List<int> indices)
        {
			if (pass == RenderPass.Transparent)
				ChunkMesher.MakeCubeVerts(pass, world, CubePosition.FromWorldSpace(pos), min, max, visual, cube, vertices, indices);
        }
    }
}
