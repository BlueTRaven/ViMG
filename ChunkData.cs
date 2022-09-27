using BrUtility;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using ViMG.Cubes;

namespace ViMG
{
	public class ChunkData : IPoolable
	{
		public enum GenerationStep
		{
			Broad,
			Detail,
			Done
		}

		public enum ThreadStates
		{
			GenerationThread,
			MainThread
		}

		public GenerationStep GenStep;
		public ThreadStates ThreadState;

		private ushort[] cubes;
		private Cube.CubeVisualInstance[] cubeVisualInstances;

		public int Density = -1; //Number of solid cubes in this chunk.
		public int Sparsity => Chunk.NUM_CUBES_IN_CHUNK - Density;

		public bool Empty => Density == 0;
		public bool Filled => Density == Chunk.NUM_CUBES_IN_CHUNK;

		private Chunk chunk;

		public bool IsDefault;

		public bool IsUsed { get; set; }
		public int PoolIndex { get; set; }

		public ChunkData()
		{
			cubes = new ushort[(int)Math.Pow(Chunk.CHUNK_SIZE, 3)]; //new int[Chunk.CHUNK_SIZE, Chunk.CHUNK_SIZE, Chunk.CHUNK_SIZE];

			cubeVisualInstances = new Cube.CubeVisualInstance[(int)Math.Pow(Chunk.CHUNK_SIZE, 3)];//[Chunk.CHUNK_SIZE, Chunk.CHUNK_SIZE, Chunk.CHUNK_SIZE];
		}

		public void CloneFrom(ChunkData data)
		{
			if (IsDefault)
				throw new Exception("Cannot clone sentinel chunk data.");

			for (ushort i = 0; i < cubes.Length; i++)
			{
				cubes[i] = data.GetRaw(i);
			}

			GenStep = data.GenStep;
		}

		public void SetChunk(Chunk chunk)
		{
			if (IsDefault)
				throw new Exception("Cannot set chunk for sentinel chunk data.");

			this.chunk = chunk;
		}

		public Chunk GetChunk()
		{
			return chunk;
		}

		public static int ChunkUpdate = 0;

		public Cube.CubeVisualInstance GetVisual(CubePosition position, bool forceUpdate = false)
		{
			if (IsDefault)
				throw new Exception("Cannot get visual for sentinel chunk data.");

			if (chunk.Position.X == -1 && chunk.Position.Y == -1 && chunk.Position.Z == -1)
				return Cube.CubeVisualInstance.CreateClean();

			if (position.Coord == CubePosition.CoordinateSpace.CubeSpace)
				position = position.InChunkSpace(chunk);

			if (forceUpdate || cubeVisualInstances[position.X + Chunk.CHUNK_SIZE * (position.Y + Chunk.CHUNK_SIZE * position.Z)].IsDirty)
			{
				ChunkUpdate++;
				DirtyCubeUpdate(position, chunk.GetWorld());
			}

			return cubeVisualInstances[position.X + Chunk.CHUNK_SIZE * (position.Y + Chunk.CHUNK_SIZE * position.Z)];
		}

		public Cube.CubeVisualInstance GetVisual(int x, int y, int z)
		{
			return GetVisual(new CubePosition(x, y, z, CubePosition.CoordinateSpace.ChunkSpace));
		}

		public Span<ushort> GetAllAsSpan()
		{
			return cubes;
		}

		public ushort[] GetAll()
		{
			return cubes;
		}

		public Cube.CubeVisualInstance[] GetAllVisuals()
        {
			return cubeVisualInstances;
        }

		public ushort GetRaw(CubePosition position)
		{
			if (IsDefault)
				return 0;

			if (chunk.Position.X == -1 && chunk.Position.Y == -1 && chunk.Position.Z == -1)
				return 0;

			if (position.Coord == CubePosition.CoordinateSpace.CubeSpace)
				position = position.InChunkSpace(chunk);

			return cubes[position.X + Chunk.CHUNK_SIZE * (position.Y + Chunk.CHUNK_SIZE * position.Z)];
		}

		public ushort GetRaw(int index)
		{
			if (IsDefault)
				throw new Exception("Cannot get data from sentinel chunk data.");

			return cubes[index];
		}

		public ushort GetRaw(int x, int y, int z)
		{
			if (IsDefault)
				throw new Exception("Cannot get data from sentinel chunk data.");

			return cubes[x + Chunk.CHUNK_SIZE * (y + Chunk.CHUNK_SIZE * z)];
		}

		public ushort GetRawOrAdjacent(CubePosition position, World world)
		{
			if (IsDefault)
				throw new Exception("Cannot get data from sentinel chunk data.");

			if (IsInChunkBounds(position))
				return GetRaw(position.X, position.Y, position.Z);
			else return world.GetChunkManager().GetRaw(position.Coord == CubePosition.CoordinateSpace.CubeSpace ? position : position.InCubeSpace(chunk));
		}

		public Optional<Cube> GetCube(CubePosition position)
		{
			if (IsDefault)
				return new Optional<Cube>();

			if (position.Coord == CubePosition.CoordinateSpace.CubeSpace)
				position = position.InChunkSpace(chunk);

			if (IsInChunkBounds(position))
				return new Optional<Cube>(Main.Registry.CubeRegistry.Get(cubes[position.X + Chunk.CHUNK_SIZE * (position.Y + Chunk.CHUNK_SIZE * position.Z)]));
			else return new Optional<Cube>();
		}

		public Optional<Cube> GetCube(int x, int y, int z)
		{
			if (IsDefault)
				throw new Exception("Cannot get data from sentinel chunk data.");

			return new Optional<Cube>(Main.Registry.CubeRegistry.Get(cubes[x + Chunk.CHUNK_SIZE * (y + Chunk.CHUNK_SIZE * z)]));
		}

		public Optional<Cube> GetCubeOrAdjacent(int x, int y, int z, World world)
		{
			if (IsDefault)
				throw new Exception("Cannot get data from sentinel chunk data.");

			CubePosition position = new CubePosition(x, y, z, CubePosition.CoordinateSpace.ChunkSpace);

			if (IsInChunkBounds(x, y, z))
				return GetCube(x, y, z);
			else return world.GetChunkManager().GetCube(position.InCubeSpace(chunk));
		}

		public Optional<Cube> GetCubeOrAdjacent(CubePosition position, World world)
		{
			if (IsDefault)
				throw new Exception("Cannot get data from sentinel chunk data.");

			if (position.Coord == CubePosition.CoordinateSpace.CubeSpace)
				position.InChunkSpace(chunk);

			if (IsInChunkBounds(position))
				return GetCube(position);
			else return world.GetChunkManager().GetCube(position.InCubeSpace(chunk));
		}

		public Cube.CubeInstance GetCubeInstance(CubePosition position)
		{
			if (IsDefault)
				throw new Exception("Cannot get data from sentinel chunk data.");

			if (position.Coord == CubePosition.CoordinateSpace.CubeSpace)
				position = position.InChunkSpace(chunk);

			if (IsInChunkBounds(position))
				return new Cube.CubeInstance(chunk, position, cubes[position.X + Chunk.CHUNK_SIZE * (position.Y + Chunk.CHUNK_SIZE * position.Z)]);
			else return new Cube.CubeInstance();
		}

		public Cube.CubeInstance GetCubeInstance(int x, int y, int z)
		{
			if (IsDefault)
				throw new Exception("Cannot get data from sentinel chunk data.");

			CubePosition position = new CubePosition(x, y, z, CubePosition.CoordinateSpace.ChunkSpace);

			if (IsInChunkBounds(x, y, z))
				return new Cube.CubeInstance(chunk, position, cubes[x + Chunk.CHUNK_SIZE * (y + Chunk.CHUNK_SIZE * z)]);
			else return new Cube.CubeInstance();
		}

		public Cube.CubeInstance GetCubeInstanceOrAdjacent(int x, int y, int z, World world)
		{
			if (IsDefault)
				throw new Exception("Cannot get data from sentinel chunk data.");

			CubePosition position = new CubePosition(x, y, z, CubePosition.CoordinateSpace.ChunkSpace);

			if (IsInChunkBounds(x, y, z))
				return GetCubeInstance(x, y, z);//new Cube.CubeInstance(chunk, position, cubes[x + Chunk.CHUNK_SIZE * (y + Chunk.CHUNK_SIZE * z)]);
			else return world.GetChunkManager().GetCubeInstance(position.InCubeSpace(chunk));
		}

		public bool IsInChunkBounds(Vector3 position)
		{
			return IsInChunkBounds(CubePosition.FromWorldSpace(position));
		}

		public bool IsInChunkBounds(CubePosition position)
		{
			if (position.Coord == CubePosition.CoordinateSpace.ChunkSpace)
			{
				return position.X >= 0 && position.X < Chunk.CHUNK_SIZE &&
					position.Y >= 0 && position.Y < Chunk.CHUNK_SIZE &&
					position.Z >= 0 && position.Z < Chunk.CHUNK_SIZE;
			}
			else
			{
				return position.X >= chunk.Position.X * Chunk.CHUNK_SIZE && position.X < chunk.Position.X * Chunk.CHUNK_SIZE + Chunk.CHUNK_SIZE &&
					position.Y >= chunk.Position.Y * Chunk.CHUNK_SIZE && position.Y < chunk.Position.Y * Chunk.CHUNK_SIZE + Chunk.CHUNK_SIZE &&
					position.Z >= chunk.Position.Z * Chunk.CHUNK_SIZE && position.Z < chunk.Position.Z * Chunk.CHUNK_SIZE + Chunk.CHUNK_SIZE;
			}
		}

		public bool IsInChunkBounds(int x, int y, int z)
		{
			return x >= 0 && x < Chunk.CHUNK_SIZE &&
					y >= 0 && y < Chunk.CHUNK_SIZE &&
					z >= 0 && z < Chunk.CHUNK_SIZE;
		}

		public Cube.CubeVisualInstance DirtyCubeUpdate(CubePosition position, World world)
		{
			if (IsDefault)
				throw new Exception("Cannot call dirty cube update from sentinel chunk data.");

			CubePosition cubeSpacePos = position.Coord == CubePosition.CoordinateSpace.CubeSpace ? position : position.InCubeSpace(chunk);
			CubePosition chunkSpacePos = position.Coord == CubePosition.CoordinateSpace.ChunkSpace ? position : position.InChunkSpace(chunk);

			Cube.CubeVisualInstance clean = Cube.CubeVisualInstance.CreateClean();

			clean.SetFaces(GetClearSides(chunkSpacePos, world));
			//clean.adjacents = GetAdjacentCubes(chunkSpacePos);

			cubeVisualInstances[chunkSpacePos.X + Chunk.CHUNK_SIZE * (chunkSpacePos.Y + Chunk.CHUNK_SIZE * chunkSpacePos.Z)] = clean;
			return clean;
		}

		public void MarkDirty(CubePosition position, bool markChunk = true)
		{
			if (IsDefault)
				throw new Exception("Cannot mark data as dirty in sentinel chunk data.");

			if (position.Coord == CubePosition.CoordinateSpace.CubeSpace)
				position = position.InChunkSpace(chunk);

			cubeVisualInstances[position.X + Chunk.CHUNK_SIZE * (position.Y + Chunk.CHUNK_SIZE * position.Z)].IsDirty = true;

			if (markChunk)
				chunk.GetWorld().GetChunkManager().MarkDirty(chunk.Position, true);
		}

		private bool IsInChunkBounds(in CubePosition position)
		{
			return position.X < 0 || position.X >= Chunk.CHUNK_SIZE || position.Y < 0 || position.Y >= Chunk.CHUNK_SIZE || position.Z < 0 || position.Z >= Chunk.CHUNK_SIZE;
		}

		// If a CubePosition overflows, use this to mark the correct cube as dirty.
		private void MarkOffsetChunkDirty(CubePosition position)
		{
			if (IsDefault)
				throw new Exception("Cannot mark data dirty in sentinel chunk data.");

			position = position.InCubeSpace(chunk);

			if (chunk.GetWorld().GetChunkManager().IsInWorldBounds(position))
			{
				Chunk offsetChunk = chunk.GetWorld().GetChunkManager().GetChunk(position);
				offsetChunk.GetData().MarkDirty(position.InChunkSpace(offsetChunk));
			}
		}

		private static CubePosition[] offsets = new CubePosition[6]
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
			if (IsDefault)
				throw new Exception("Cannot mark data dirty in sentinel chunk data.");

			if (position.Coord == CubePosition.CoordinateSpace.CubeSpace)
				position = position.InChunkSpace(chunk);

			for (int i = 0; i < 6; i++)
			{
				ref CubePosition offset = ref offsets[i];

				CubePosition newPos = new CubePosition(position.X + offset.X, position.Y + offset.Y, position.Z + offset.Z, CubePosition.CoordinateSpace.ChunkSpace);

				if (!IsInChunkBounds(newPos))
					MarkOffsetChunkDirty(newPos);
				else MarkDirty(newPos, false);
			}
		}

		private void CubeUpdate(CubePosition position, int updatedId)
		{
			if (IsDefault)
				throw new Exception("Cannot update data in sentinel chunk data.");

			//Notify adjacent cubes
			for (int i = 0; i < 6; i++)
			{
				ref CubePosition offset = ref offsets[i];
				CubePosition newPos = new CubePosition(position.X + offset.X, position.Y + offset.Y, position.Z + offset.Z, CubePosition.CoordinateSpace.ChunkSpace);

				if (!IsInChunkBounds(newPos))
				{
					CubePosition adjPos = newPos.InCubeSpace(chunk);
					Chunk adjChunk = chunk.GetWorld().GetChunkManager().GetChunk(adjPos);

					//If the chunk is null, then that means it's probably an unloaded chunk.
					if (adjChunk == null)
					{
						//Try to load it.
						chunk.GetWorld().ChunkLoadManager.LoadChunk(chunk.GetWorld(), ChunkPosition.CubeChunk(adjPos));
						adjChunk = chunk.GetWorld().GetChunkManager().GetChunk(adjPos);
					}

					if (adjChunk != null)
					{
						GetCube(position).GetOrDefault(Main.Registry.CubeRegistry.Air).OnAdjacentUpdated(adjChunk.GetData(), adjPos, this, position.InCubeSpace(chunk), updatedId);

						chunk.GetWorld().OnCubeUpdate(this, position.InCubeSpace(chunk), updatedId);
					}
				}
				else
				{
					GetCube(position).GetOrDefault(Main.Registry.CubeRegistry.Air).OnAdjacentUpdated(this, newPos.InCubeSpace(chunk), this, position.InCubeSpace(chunk), updatedId);

					chunk.GetWorld().OnCubeUpdate(this, position.InCubeSpace(chunk), updatedId);
				}
			}
		}

		private float[] uploadArr = new float[1];

		public void SetCube(CubePosition position, ushort id, bool markDirty = true, bool killTrackedEntities = true)
		{
			if (IsDefault)
				throw new Exception("Cannot set data in sentinel chunk data.");

			if (Thread.CurrentThread != Main.MainThread && chunk.Initialized)
				throw new Exception("Cannot set chunk outside of main thread after initialization.");

			CubePosition positionCubeSpace = position.Coord == CubePosition.CoordinateSpace.CubeSpace ? position : position.InCubeSpace(chunk);
			CubePosition positionChunkSpace = position.Coord == CubePosition.CoordinateSpace.CubeSpace ? position.InChunkSpace(chunk) : position;

			int index = positionChunkSpace.X + Chunk.CHUNK_SIZE * (positionChunkSpace.Y + Chunk.CHUNK_SIZE * positionChunkSpace.Z);
			ushort currentId = cubes[index];
			if (markDirty)
			{
				// no entities will be tracking while chunk is not initialized - skip this step
				if (chunk.Initialized && killTrackedEntities)
				{
					if (currentId != id)
					{
						var trackingEntity = chunk.GetWorld().EntityManager.GetEntityTrackingPosition(positionCubeSpace);

						if (trackingEntity.HasValue())
							trackingEntity.Get().TrackingCubeDestroyed(chunk.GetWorld(), chunk.GetChunkManager());
					}
				}

				CubeUpdate(positionChunkSpace, id);
			}

			/*int sizeInCubes = chunk.GetChunkManager().sizeInCubes;
			ref float height = ref chunk.GetChunkManager().HeightmapRaw[positionCubeSpace.Z * sizeInCubes + positionCubeSpace.X];

			if (positionCubeSpace.Y > height * sizeInCubes)
			{
				//If we're modifying a position above the height and our cube is opaque,
				//We can safely overwrite it.
				Cube cube = Main.Registry.CubeRegistry.Get(id);

				if (cube != null)
				{
					if (cube.Transparency == Cube.TransparencyValue.Opaque)
					{
						height = (float)positionCubeSpace.Y / sizeInCubes;
						chunk.GetChunkManager().Heightmap.SetData(0, new Rectangle(positionCubeSpace.X, positionCubeSpace.Z, 1, 1), 
							chunk.GetChunkManager().HeightmapRaw, positionCubeSpace.Z * sizeInCubes + positionCubeSpace.X, 1);
						//chunk.GetChunkManager().Heightmap.SetData(chunk.GetChunkManager().HeightmapRaw);
					}
				}
			}
			else if (positionCubeSpace.Y == (int)MathF.Round(height * sizeInCubes, MidpointRounding.ToEven))
            {
				int wheight = (int)MathF.Round(height * sizeInCubes, MidpointRounding.ToEven);
				//Otherwise, if we're modifying the cube at the current height, we need to look down (starting from the height) 
				//until we find the next opaque cube.
				//TODO this might look down instead of up
				for (int i = (int)wheight; i >= 0; i--)
				{
					Cube newCube = chunk.GetChunkManager().GetCube(new CubePosition(positionCubeSpace.X, i, positionCubeSpace.Z))
						.GetOrDefault(Main.Registry.CubeRegistry.Air);
					if (newCube.Transparency == Cube.TransparencyValue.Opaque)
						height = (float)i;

					chunk.GetChunkManager().Heightmap.SetData(0, new Rectangle(positionCubeSpace.X, positionCubeSpace.Z, 1, 1),
							chunk.GetChunkManager().HeightmapRaw, positionCubeSpace.Z * sizeInCubes + positionCubeSpace.X, 1);
				}
			}*/

			cubes[index] = id;

			if (markDirty)
			{
				MarkDirty(positionChunkSpace);
				MarkAdjacentsDirty(positionChunkSpace);
			}
		}

		public void SetDensity(ushort oldId, ushort newId)
        {
			Cube oldCube = Main.Registry.CubeRegistry.Get(oldId);
			Cube newCube = Main.Registry.CubeRegistry.Get(newId);

			bool oldNotSolid = oldCube == null || !oldCube.Solid;
			bool newNotSolid = newCube == null || !newCube.Solid;

			if (oldNotSolid != newNotSolid)
			{
				if (newNotSolid)
					Density--;
				else
					Density++;
			}
		}

		public void SetCubeFast(CubePosition positionInChunkSpace, ushort id)
        {
			SetCubeFast(positionInChunkSpace.X + Chunk.CHUNK_SIZE * (positionInChunkSpace.Y + Chunk.CHUNK_SIZE * positionInChunkSpace.Z), id);
		}

		public void SetCubeFast(int index, ushort id)
		{
			cubes[index] = id;
		}

		public MeshHelper.CubeFace GetClearSides(CubePosition position, World world)
		{
			Cube cube = GetCube(position).GetOrDefault(Main.Registry.CubeRegistry.Air);

			if (cube == Main.Registry.CubeRegistry.Air)
				return MeshHelper.CubeFace.ALL;
			else if (cube.Transparency == Cube.TransparencyValue.Invisible)
				return MeshHelper.CubeFace.NONE;

			MeshHelper.CubeFace faces = MeshHelper.CubeFace.NONE;

			if (HasClearSide(position.X - 1, position.Y, position.Z, cube, world))//if (GetCubeOrAdjacent(position.X - 1, position.Y, position.Z, world).GetOrDefault(Main.Registry.CubeRegistry.Air).Transparency == Cube.TransparencyValue.Transparent)
				faces |= MeshHelper.CubeFace.LEFT;
			if (HasClearSide(position.X + 1, position.Y, position.Z, cube, world))//if (GetCubeOrAdjacent(position.X + 1, position.Y, position.Z, world).GetOrDefault(Main.Registry.CubeRegistry.Air).Transparency == Cube.TransparencyValue.Transparent)
				faces |= MeshHelper.CubeFace.RIGHT;

			if (HasClearSide(position.X, position.Y - 1, position.Z, cube, world))//if (GetCubeOrAdjacent(position.X, position.Y - 1, position.Z, world).GetOrDefault(Main.Registry.CubeRegistry.Air).Transparency == Cube.TransparencyValue.Transparent)
				faces |= MeshHelper.CubeFace.DOWN;
			if (HasClearSide(position.X, position.Y + 1, position.Z, cube, world))//if (GetCubeOrAdjacent(position.X, position.Y + 1, position.Z, world).GetOrDefault(Main.Registry.CubeRegistry.Air).Transparency == Cube.TransparencyValue.Transparent)
				faces |= MeshHelper.CubeFace.UP;

			if (HasClearSide(position.X, position.Y, position.Z - 1, cube, world))//if (GetCubeOrAdjacent(position.X, position.Y, position.Z - 1, world).GetOrDefault(Main.Registry.CubeRegistry.Air).Transparency == Cube.TransparencyValue.Transparent)
				faces |= MeshHelper.CubeFace.FRONT;
			if (HasClearSide(position.X, position.Y, position.Z + 1, cube, world))//if (GetCubeOrAdjacent(position.X, position.Y, position.Z + 1, world).GetOrDefault(Main.Registry.CubeRegistry.Air).Transparency == Cube.TransparencyValue.Transparent)
				faces |= MeshHelper.CubeFace.BACK;

			return faces;
		}

		private bool HasClearSide(int x, int y, int z, Cube currentCube, World world)
		{
			/*if (!IsInChunkBounds(x, y, z))
			{
				CubePosition cpos = new CubePosition(x, y, z, CubePosition.CoordinateSpace.ChunkSpace).InCubeSpace(chunk);
				chunk.GetChunkManager().GetChunk(cpos).GetData().MarkOffsetChunkDirty(cpos);
			}*/

			Cube adjacentCube = GetCubeOrAdjacent(x, y, z, world).GetOrDefault(Main.Registry.CubeRegistry.Air);

			if (adjacentCube.Transparency == Cube.TransparencyValue.Transparent || adjacentCube.Transparency == Cube.TransparencyValue.Invisible)
				return true;
			if (adjacentCube.Transparency == Cube.TransparencyValue.TransparentOccludesSiblings)
			{
				if (currentCube == adjacentCube)
					return false;
				else return true;
			}
			else return false;
		}

		public void OnGet<T>(GenericPool<T> pool) where T : IPoolable
		{
			IsUsed = true;
			//x + WIDTH * (y + DEPTH * z)
			const int size = Chunk.CHUNK_SIZE * Chunk.CHUNK_SIZE * Chunk.CHUNK_SIZE;
			for (int i = 0; i < size; i++)
			{
				cubes[i] = 0;
				cubeVisualInstances[i] = Cube.CubeVisualInstance.CreateDirty();
			}

			/*for (int z = 0; z < Chunk.CHUNK_SIZE; z++)
			{
				for (int y = 0; y < Chunk.CHUNK_SIZE; y++)
				{
					for (int x = 0; x < Chunk.CHUNK_SIZE; x++)
					{
						cubes[x, y, z] = 0;
						cubeVisualInstances[x, y, z] = Cube.CubeVisualInstance.CreateDirty();
					}
				}
			}*/
		}

		public void OnReturned<T>(GenericPool<T> pool) where T : IPoolable
		{
			IsUsed = false;

			if (IsDefault)
				throw new Exception("Cannot return sentinel chunk data.");

			chunk = null;
			Density = 0;
			Array.Fill<Cube.CubeVisualInstance>(cubeVisualInstances, Cube.CubeVisualInstance.CreateDirty());
			Array.Fill<ushort>(cubes, 0);
		}
	}
}
