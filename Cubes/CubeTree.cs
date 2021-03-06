using BrUtility;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace ViMG.Cubes
{
	public class CubeTree : Cube
	{
		public CubeTree() : base("tree", new RectangleF(64, 0, 16, 16), Color.White, 1)
		{
			Transparency = TransparencyValue.Invisible;
		}
	}
}
