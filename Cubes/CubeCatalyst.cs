using BrUtility;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace ViMG.Cubes
{
	public class CubeCatalyst : Cube
	{
		public CubeCatalyst(string identifier, RectangleF sourceRect, Color color, int mineProgressRequirement) : base(identifier, sourceRect, color, mineProgressRequirement)
		{
		}
	}
}
