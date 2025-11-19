using LiteNetLib.Utils;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using ViMG.Cubes;

namespace ViMG
{
	public struct CubePosition : INetSerializable
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

		public CubePosition(CubePosition other, CoordinateSpace coord)
		{
			this.X = other.X;
			this.Y = other.Y;
			this.Z = other.Z;

			this.Coord = coord;
		}

		public CubePosition(Point3D point, CoordinateSpace coord = CoordinateSpace.CubeSpace) : 
			this(point.X, point.Y, point.Z, coord)
        {

        }

		public CubePosition InChunkSpace(ChunkPosition position)
        {
			if (Coord == CoordinateSpace.ChunkSpace)
			{
				return this;
			}
			else
			{
				return new CubePosition(X - position.X * Chunk.CHUNK_SIZE,
					Y - position.Y * Chunk.CHUNK_SIZE,
					Z - position.Z * Chunk.CHUNK_SIZE, CoordinateSpace.ChunkSpace);
			}
		}

		//Rounds to chunk space
		public CubePosition InChunkSpace()
        {
			int csx = X & (Chunk.CHUNK_SIZE - 1);
			int csy = Y & (Chunk.CHUNK_SIZE - 1);
			int csz = Z & (Chunk.CHUNK_SIZE - 1);
			return new CubePosition(csx, csy, csz, CoordinateSpace.ChunkSpace);
		}

		public CubePosition InCubeSpace(ChunkPosition position)
		{
			if (Coord == CoordinateSpace.CubeSpace)
			{
				return this;
			}
			else
			{
				return new CubePosition(position.X * Chunk.CHUNK_SIZE + X,
					position.Y * Chunk.CHUNK_SIZE + Y,
					position.Z * Chunk.CHUNK_SIZE + Z, CoordinateSpace.CubeSpace);
			}
		}

		//Assumes this is in cube-space.
		public Vector3 InWorldSpace()
		{
			return new Vector3(X * Cube.CUBE_SCALE, Y * Cube.CUBE_SCALE, Z * Cube.CUBE_SCALE);
		}

		public Vector3 InWorldSpaceCenter()
		{
			return InWorldSpace() + new Vector3(Cube.CUBE_SCALE / 2f);
		}

		public Vector3 InWorldSpace(ChunkPosition chunk)
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
			return new Vector3(
				MathF.Floor(position.X / Cube.CUBE_SCALE),
				MathF.Floor(position.Y / Cube.CUBE_SCALE),
				MathF.Floor(position.Z / Cube.CUBE_SCALE));
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
			return new Rectangle3D(position.InWorldSpace(), new Vector3(Cube.CUBE_SCALE));
		}

        public void Serialize(NetDataWriter writer)
        {
			writer.Put(X);
            writer.Put(Y);
            writer.Put(Z);
        }

        public void Deserialize(NetDataReader reader)
        {
			X = reader.GetInt();
            Y = reader.GetInt();
            Z = reader.GetInt();
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
			Debug.Assert(posA.Coord == posB.Coord);
			return new CubePosition(posA.X + posB.X, posA.Y + posB.Y, posA.Z + posB.Z, posA.Coord);
		}

		public static CubePosition operator -(CubePosition posA, CubePosition posB)
		{
            Debug.Assert(posA.Coord == posB.Coord);
			return new CubePosition(posA.X - posB.X, posA.Y - posB.Y, posA.Z - posB.Z, posA.Coord);
		}
	}
}
