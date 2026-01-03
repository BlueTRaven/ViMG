using BrUtility;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace ViMG.Cubes
{
	public class CubeWater : Cube
	{
		public CubeWater() : base("water", -1)
		{
			Touchable = false;
			Transparency = TransparencyValue.TransparentOccludesSiblings;
			Collision = CollisionValue.LiquidWater;

            Client = new(this, new RectangleF(0, 16, 16, 16), Color.White);
        }

        public override bool ShouldMeshPass(RenderPass pass)
        {
            return pass == RenderPass.Transparent;
        }
    }
}
