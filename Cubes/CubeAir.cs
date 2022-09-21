using BrUtility;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace ViMG.Cubes
{
	public class CubeAir : Cube
	{
		public CubeAir() : base("air", new RectangleF(), Color.White, -1)
		{
			Touchable = false;
			Collision = CollisionValue.None;
			Transparency = TransparencyValue.Invisible;
		}
	}
}
