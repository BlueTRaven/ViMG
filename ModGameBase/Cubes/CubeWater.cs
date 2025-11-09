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
			Collision = CollisionValue.LiquidWater;
		}

        public override bool ShouldMeshPass(RenderPass pass)
        {
            return pass == RenderPass.Transparent;
        }
    }
}
