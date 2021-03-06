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
			Solid = false;
			Transparency = TransparencyValue.TransparentOccludesSiblings;
		}
	}
}
