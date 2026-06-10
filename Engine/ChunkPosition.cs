using LiteNetLib.Utils;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using ViMG.Cubes;

namespace ViMG
{
	public struct ChunkPosition : INetSerializable
	{
		public int X;
		public int Y;
		public int Z;

		public ChunkPosition(Vector3 position)
		{
			X = (int)position.X;
			Y = (int)position.Y;
			Z = (int)position.Z;
		}

		public ChunkPosition(int x, int y, int z)
		{
			this.X = x;
			this.Y = y;
			this.Z = z;
		}

		public CubePosition InCubeSpace()
		{
			return new CubePosition(X * Chunk.CHUNK_SIZE, Y * Chunk.CHUNK_SIZE, Z * Chunk.CHUNK_SIZE, CubePosition.CoordinateSpace.CubeSpace);
		}

		public Vector3 InWorldSpace()
		{
			return new Vector3(X, Y, Z) * Chunk.CHUNK_SIZE * Cube.CUBE_SCALE;
		}

		public static ChunkPosition WorldSpaceChunk(Vector3 position)
		{
			return new ChunkPosition(position / Chunk.CHUNK_SIZE / Cube.CUBE_SCALE);
		}

		public static ChunkPosition CubeChunk(CubePosition position)
		{
			Debug.Assert(position.Coord == CubePosition.CoordinateSpace.CubeSpace);
			int x = (int)MathF.Floor(position.X / (float)Chunk.CHUNK_SIZE);
			int y = (int)MathF.Floor(position.Y / (float)Chunk.CHUNK_SIZE);
			int z = (int)MathF.Floor(position.Z / (float)Chunk.CHUNK_SIZE);

			return new ChunkPosition(x, y, z);
		}

		public override string ToString()
		{
			return " X: " + X.ToString() + " Y: " + Y.ToString() + " Z: " + Z.ToString();
		}

		public override bool Equals(object obj)
		{
			return obj is ChunkPosition position &&
				   X == position.X &&
				   Y == position.Y &&
				   Z == position.Z;
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

        public static bool operator ==(ChunkPosition first, ChunkPosition second)
		{
			return first.X == second.X && first.Y == second.Y && first.Z == second.Z;
		}

		public static bool operator !=(ChunkPosition first, ChunkPosition second)
		{
			return first.X != second.X || first.Y != second.Y || first.Z != second.Z;
		}

		public static ChunkPosition operator +(ChunkPosition first, ChunkPosition second)
		{
			return new ChunkPosition(first.X + second.X, first.Y + second.Y, first.Z + second.Z);
		}
	}
}
