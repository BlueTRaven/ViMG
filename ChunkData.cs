using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Cubes;

namespace ViMG
{
	public class ChunkData
	{
		// Kinda hacky
		public bool IsThreadedLoad;

		private int[,,] cubes;
		private Cube.CubeVisualInstance[,,] cubeVisualInstances;

		private Chunk chunk;

		public ChunkData(Chunk chunk)
		{
			this.chunk = chunk;

			cubes = new int[Chunk.CHUNK_SIZE, Chunk.CHUNK_SIZE, Chunk.CHUNK_SIZE];

			cubeVisualInstances = new Cube.CubeVisualInstance[Chunk.CHUNK_SIZE, Chunk.CHUNK_SIZE, Chunk.CHUNK_SIZE];

			for (int x = 0; x < Chunk.CHUNK_SIZE; x++)
			{
				for (int y = 0; y < Chunk.CHUNK_SIZE; y++)
				{
					for (int z = 0; z < Chunk.CHUNK_SIZE; z++)
					{
						cubes[x, y, z] = 0;
						cubeVisualInstances[x, y, z] = Cube.CubeVisualInstance.CreateDirty();
					}
				}
			}
		}

		public void SetChunk(Chunk chunk)
		{
			this.chunk = chunk;
		}

		public static int ChunkUpdate = 0;

		public Cube.CubeVisualInstance GetVisual(CubePosition position)
		{
			if (position.Coord == CubePosition.CoordinateSpace.CubeSpace)
				position = position.InChunkSpace(chunk);

			if (cubeVisualInstances[position.X, position.Y, position.Z].dirty)
			{
				ChunkUpdate++;
				DirtyCubeUpdate(position);
			}

			return cubeVisualInstances[position.X, position.Y, position.Z];
		}

		public Cube.CubeVisualInstance GetVisual(int x, int y, int z)
		{
			return GetVisual(new CubePosition(x, y, z, CubePosition.CoordinateSpace.ChunkSpace));
		}

		public int GetRaw(CubePosition position)
		{
			if (position.Coord == CubePosition.CoordinateSpace.CubeSpace)
				position = position.InChunkSpace(chunk);

			return cubes[position.X, position.Y, position.Z];
		}

		public int GetRaw(int x, int y, int z)
		{
			return cubes[x, y, z];
		}

		public Cube.CubeInstance GetCube(CubePosition position)
		{
			if (position.Coord == CubePosition.CoordinateSpace.CubeSpace)
				position = position.InChunkSpace(chunk);

			if (IsInChunkBounds(position))
				return new Cube.CubeInstance(chunk, position, cubes[position.X, position.Y, position.Z]);
			else return new Cube.CubeInstance();
		}

		public Cube.CubeInstance GetCube(int x, int y, int z)
		{
			CubePosition position = new CubePosition(x, y, z, CubePosition.CoordinateSpace.ChunkSpace);

			if (IsInChunkBounds(position))
				return new Cube.CubeInstance(chunk, position, cubes[x, y, z]);
			else return new Cube.CubeInstance();
		}

		public bool IsInChunkBounds(CubePosition position)
		{
			return position.X >= 0 && position.X < Chunk.CHUNK_SIZE &&
				position.Y >= 0 && position.Y < Chunk.CHUNK_SIZE &&
				position.Z >= 0 && position.Z < Chunk.CHUNK_SIZE;
		}

		public Cube.CubeVisualInstance DirtyCubeUpdate(CubePosition position)
		{
			CubePosition cubeSpacePos = position.Coord == CubePosition.CoordinateSpace.CubeSpace ? position : position.InCubeSpace(chunk);
			CubePosition chunkSpacePos = position.Coord == CubePosition.CoordinateSpace.ChunkSpace ? position : position.InChunkSpace(chunk);

			Cube.CubeVisualInstance clean = Cube.CubeVisualInstance.CreateClean();

			clean.clearSides = GetClearSides(chunkSpacePos);

			cubeVisualInstances[chunkSpacePos.X, chunkSpacePos.Y, chunkSpacePos.Z] = clean;
			return clean;
		}

		public void MarkDirty(CubePosition position, bool markChunk = true)
		{
			if (position.Coord == CubePosition.CoordinateSpace.CubeSpace)
				position = position.InChunkSpace(chunk);

			cubeVisualInstances[position.X, position.Y, position.Z].dirty = true;

			if (markChunk)
				chunk.GetWorld().GetChunkManager().MarkDirty(chunk.Position);
		}

		// If a CubePosition overflows, use this to mark the correct cube as dirty.
		private void MarkOffsetChunkDirty(CubePosition position)
		{
			if (IsThreadedLoad)
				return;

			position = position.InCubeSpace(chunk);

			if (chunk.GetWorld().IsInWorldBounds(position))
			{
				Chunk offsetChunk = chunk.GetWorld().GetChunkManager().GetChunk(position.InCubeSpace(chunk));
				offsetChunk.GetData().MarkDirty(position.InChunkSpace(offsetChunk));
			}
		}

		private CubePosition[] offsets = new CubePosition[6]
		{
			new CubePosition(-1, 0, 0),
			new CubePosition(1, 0, 0),
			new CubePosition(0, -1, 0),
			new CubePosition(0, 1, 0),
			new CubePosition(0, 0, -1),
			new CubePosition(0, 0, 1)
		};

		public void MarkAdjacentsDirty(CubePosition position)
		{
			if (position.Coord == CubePosition.CoordinateSpace.CubeSpace)
				position = position.InChunkSpace(chunk);

			for (int i = 0; i < 6; i++)
			{
				ref CubePosition offset = ref offsets[i];

				CubePosition newPos = new CubePosition(position.X + offset.X, position.Y + offset.Y, position.Z + offset.Z, CubePosition.CoordinateSpace.ChunkSpace);

				if (newPos.X < 0 || newPos.X >= Chunk.CHUNK_SIZE || newPos.Y < 0 || newPos.Y >= Chunk.CHUNK_SIZE || newPos.Z < 0 || newPos.Z >= Chunk.CHUNK_SIZE)
					MarkOffsetChunkDirty(newPos);
				else MarkDirty(newPos, false);
			}
		}

		public void SetCube(CubePosition position, int id, bool markDirty = true)
		{
			if (position.Coord == CubePosition.CoordinateSpace.CubeSpace)
				position = position.InChunkSpace(chunk);
			
			cubes[position.X, position.Y, position.Z] = id;

			if (markDirty)
			{
				MarkDirty(position);
				MarkAdjacentsDirty(position);
			}
		}

		public MeshHelper.CubeFace GetClearSides(CubePosition position)
		{
			if (GetRaw(position) == 0)
				return MeshHelper.CubeFace.ALL;

			MeshHelper.CubeFace faces = MeshHelper.CubeFace.NONE;

			//TODO handle overflow into other chunks
			if (GetCube(position.X - 1, position.Y, position.Z).cubeId == 0)
				faces |= MeshHelper.CubeFace.LEFT;
			if (GetCube(position.X + 1, position.Y, position.Z).cubeId == 0)
				faces |= MeshHelper.CubeFace.RIGHT;

			if (GetCube(position.X, position.Y - 1, position.Z).cubeId == 0)
				faces |= MeshHelper.CubeFace.DOWN;
			if (GetCube(position.X, position.Y + 1, position.Z).cubeId == 0)
				faces |= MeshHelper.CubeFace.UP;

			if (GetCube(position.X, position.Y, position.Z - 1).cubeId == 0)
				faces |= MeshHelper.CubeFace.FRONT;
			if (GetCube(position.X, position.Y, position.Z + 1).cubeId == 0)
				faces |= MeshHelper.CubeFace.BACK;

			return faces;
		}
	}
}
