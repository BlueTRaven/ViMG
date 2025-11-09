using BrUtility;
using System;
using System.Collections.Generic;
using System.Text;

namespace ViMG.Cubes
{
	public class CubeFacingLayout
	{
		public RectangleF Front;
		public RectangleF Back;
		public RectangleF Left;
		public RectangleF Right;
		public RectangleF Top;
		public RectangleF Bottom;

		public CubeFacingLayout(RectangleF front, RectangleF back, RectangleF left, RectangleF right, RectangleF top, RectangleF bottom)
		{
			Front = front;
			Back = back;
			Left = left;
			Right = right;
			Top = top;
			Bottom = bottom;
		}

		public CubeFacingLayout(RectangleF sides, RectangleF top, RectangleF bottom)
		{
			this.Front = sides;
			this.Back = sides;
			this.Left = sides;
			this.Right = sides;
			this.Top = top;
			this.Bottom = bottom;
		}

		public CubeFacingLayout(RectangleF sides, RectangleF topBottom)
        {
			this.Front = sides;
			this.Back = sides;
			this.Left = sides;
			this.Right = sides;
			this.Top = topBottom;
			this.Bottom = topBottom;
		}

		public CubeFacingLayout(RectangleF allSides)
		{
			Front = allSides;
			Back = allSides;
			Left = allSides;
			Right = allSides;
			Top = allSides;
			Bottom = allSides;
		}
	}
}
