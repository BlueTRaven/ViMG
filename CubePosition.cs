using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Cubes;

namespace ViMG
{
	public struct CubePosition
	{
		public enum CoordinateSpace
		{
			// Relative to the parent chunk.
			ChunkSpace,
			// Relative to the world.
			CubeSpace,
		}

		public int X;
		public int Y;
		public int Z;

		public readonly CoordinateSpace Coord;

		public CubePosition(int x, int y, int z, CoordinateSpace coord = CoordinateSpace.CubeSpace)
		{
			this.X = x;
			this.Y = y;
			this.Z = z;

			this.Coord = coord;
		}

		public CubePosition InChunkSpace(Chunk chunk)
		{
			if (Coord == CoordinateSpace.ChunkSpace)
			{
				return this;
			}
			else
			{
				return new CubePosition(X - chunk.Position.X * Chunk.CHUNK_SIZE,
					Y - chunk.Position.Y * Chunk.CHUNK_SIZE,
					Z - chunk.Position.Z * Chunk.CHUNK_SIZE, CoordinateSpace.ChunkSpace);
			}
		}

		public CubePosition InCubeSpace(Chunk chunk)
		{
			if (Coord == CoordinateSpace.CubeSpace)
			{
				return this;
			}
			else
			{
				return new CubePosition(chunk.Position.X * Chunk.CHUNK_SIZE + X,
					chunk.Position.Y * Chunk.CHUNK_SIZE + Y,
					chunk.Position.Z * Chunk.CHUNK_SIZE + Z, CoordinateSpace.CubeSpace);
			}
		}

		public Vector3 InWorldSpace(Chunk chunk)
		{
			CubePosition pos = this;
			if (Coord == CoordinateSpace.ChunkSpace)
				pos = pos.InCubeSpace(chunk);

			return new Vector3(pos.X * Cube.CUBE_SCALE, pos.Y * Cube.CUBE_SCALE, pos.Z * Cube.CUBE_SCALE);
		}

		public Vector3 InWorldSpace(out bool ok)
		{
			CubePosition pos = this;
			if (Coord == CoordinateSpace.ChunkSpace)
			{
				ok = false;
				return Vector3.Zero;
			}

			ok = true;
			return new Vector3(pos.X * Cube.CUBE_SCALE, pos.Y * Cube.CUBE_SCALE, pos.Z * Cube.CUBE_SCALE);
		}

		public override string ToString()
		{
			return base.ToString() + " X: " + X.ToString() + " Y: " + Y.ToString() + " Z: " + Z.ToString();
		}

		public static CubePosition FromWorldSpace(Vector3 position)
		{
			Vector3 pos = FromWorldSpaceV3(position);
			return new CubePosition((int)pos.X, (int)pos.Y, (int)pos.Z);
		}

		public static Vector3 FromWorldSpaceV3(Vector3 position)
		{
			return position / Cube.CUBE_SCALE;
		}

		public static Vector3 ToWorldSpaceV3(Vector3 position)
		{
			return position * Cube.CUBE_SCALE;
		}

		// Stay in world space, but round to cube space.
		public static Vector3 RoundToCubeSpace(Vector3 position)
		{
			Vector3 cs = FromWorldSpaceV3(position);
			Vector3 rounded = new Vector3((int)cs.X, (int)cs.Y, (int)cs.Z);

			Vector3 wsRounded = ToWorldSpaceV3(rounded);

			return wsRounded;
		}

		public static Rectangle3D BoundsWorldSpace(CubePosition position)
		{
			return new Rectangle3D(position.InWorldSpace(null), new Vector3(Cube.CUBE_SCALE));
		}

		public static bool operator ==(CubePosition posA, CubePosition posB)
		{
			return posA.X == posB.X && posA.Y == posB.Y && posA.Z == posB.Z;
		}

		public static bool operator !=(CubePosition posA, CubePosition posB)
		{
			return posA.X != posB.X || posA.Y != posB.Y || posA.Z != posB.Z;
		}

		public static CubePosition operator +(CubePosition posA, CubePosition posB)
		{
			if (posA.Coord == posB.Coord)
			{
				return new CubePosition(posA.X + posB.X, posA.Y + posB.Y, posA.Z + posB.Z, posA.Coord);
			}
			else return new CubePosition();
		}
	}
}
