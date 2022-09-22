using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace ViMG
{
	public struct Rectangle3D
	{
		public Vector3 Position;
		public Vector3 Size;

		public Vector3 FarPosition => Position + Size;

		public float Left => Position.X + Size.X;
		public float Right => Position.X;

		public float Top => Position.Y + Size.Y;
		public float Bottom => Position.Y;

		public float Front => Position.Z;
		public float Back => Position.Z + Size.Z;

		public Rectangle3D InnerBounds => new Rectangle3D(Vector3.Zero, Size);

		public Vector3 Center => Position + (Size / 2f);

		public Rectangle3D(Vector3 position, Vector3 size)
		{
			this.Position = position;
			this.Size = size;
		}

		public bool Contains(Vector3 point)
		{
			return point.X >= Position.X && point.X < Position.X + Size.X && point.Y >= Position.Y && point.Y < Position.Y + Size.Y && point.Z >= Position.Z && point.Z < Position.Z + Size.Z;
		}

		public Rectangle3D Offset(Vector3 offsetBy)
		{
			return new Rectangle3D(Position + offsetBy, Size);
		}

		public bool Intersects(Rectangle3D rect)
		{
			return rect.Left > Right && Left > rect.Right &&
				   rect.Top > Bottom && Top > rect.Bottom &&
				   rect.Front < Back && Front < rect.Back;
		}

		public Vector3 Clamp(Vector3 vec)
        {
			vec.X = MathHelper.Clamp(vec.X, Right, Left);
			vec.Y = MathHelper.Clamp(vec.Y, Bottom, Top);
			vec.Z = MathHelper.Clamp(vec.Z, Front, Back);

			return vec;
		}
	}

	public struct Rectangle3DI
	{
		public Point3D Position;
		public Point3D Size;

		public Point3D FarPosition => Position + Size;

		public int Left => Position.X + Size.X;
		public int Right => Position.X;

		public int Top => Position.Y + Size.Y;
		public int Bottom => Position.Y;

		public int Front => Position.Z;
		public int Back => Position.Z + Size.Z;

		public Rectangle3DI InnerBounds => new Rectangle3DI(Point3D.Zero, Size);

		public Point3D Center => Position + (Size / 2f);

		public Rectangle3DI(Point3D position, Point3D size)
		{
			this.Position = position;
			this.Size = size;
		}

		public bool Contains(Vector3 point)
		{
			return point.X >= Position.X && point.X < Position.X + Size.X && point.Y >= Position.Y && point.Y < Position.Y + Size.Y && point.Z >= Position.Z && point.Z < Position.Z + Size.Z;
		}

		public Rectangle3DI Offset(Point3D offsetBy)
		{
			return new Rectangle3DI(Position + offsetBy, Size);
		}

		public bool Intersects(Rectangle3DI rect)
		{
			return rect.Left > Right && Left > rect.Right &&
				   rect.Top > Bottom && Top > rect.Bottom &&
				   rect.Front < Back && Front < rect.Back;
		}

		public Point3D Clamp(Point3D vec)
		{
			vec.X = MathHelper.Clamp(vec.X, Right, Left);
			vec.Y = MathHelper.Clamp(vec.Y, Bottom, Top);
			vec.Z = MathHelper.Clamp(vec.Z, Front, Back);

			return vec;
		}
	}
}
