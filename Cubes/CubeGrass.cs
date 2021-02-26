using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace ViMG.Cubes
{
	public class CubeGrass : Cube
	{
		public CubeGrass() : base(new RectangleF[6] { 
			new RectangleF(32, 0, 16, 16), new RectangleF(32, 0, 16, 16), new RectangleF(48, 0, 16, 16), 
			new RectangleF(0, 0, 16, 16), new RectangleF(32, 0, 16, 16), new RectangleF(32, 0, 16, 16) }, Color.White, 2)
		{

		}
	}
}
