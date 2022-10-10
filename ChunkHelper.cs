using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Generation;

namespace ViMG
{
    public static class ChunkHelper
    {
		private static Chunk cachedWorkingChunk;
		private static ushort[] cachedWorkingCubes;

		private static Chunk cachedAdjacentChunk;
		private static ushort[] cachedAdjacentCubes;

		public static void SetCubeOrAdjacent(ChunkManager manager, Chunk chunk, CubePosition pos, ushort id)
		{
			if (!manager.IsInWorldBounds(pos))
				return;

			// Chunks that have already been fully generated can be marked as dirty
			if (chunk.GetData().IsInChunkBounds(pos))
			{
				if (cachedWorkingChunk != chunk)
				{
					cachedWorkingChunk = chunk;
					cachedWorkingCubes = chunk.GetData().GetAll();
				}

				if (pos.Coord == CubePosition.CoordinateSpace.CubeSpace)
					pos = pos.InChunkSpace(chunk);

				cachedWorkingCubes[pos.X + Chunk.CHUNK_SIZE * (pos.Y + Chunk.CHUNK_SIZE * pos.Z)] = id;
			}
			else
			{
				//Make no attempt to cache in this case. We'll likely miss
				ChunkPosition chunkPos = ChunkPosition.CubeChunk(pos.InCubeSpace(chunk));
				Chunk adjacent = manager.GetChunk(chunkPos);

				if (cachedAdjacentChunk != adjacent)
				{
					cachedAdjacentChunk = adjacent;
					cachedAdjacentCubes = adjacent.GetData().GetAll();
				}

				if (pos.Coord == CubePosition.CoordinateSpace.CubeSpace)
					pos = pos.InChunkSpace(adjacent);

				//This should never actually be called with the current way of doing things, but still...
				//if (adjacent.GetData().GenStep == ChunkData.GenerationStep.Broad)
				//GenerateChunkBroad(adjacent);

				cachedAdjacentCubes[pos.X + Chunk.CHUNK_SIZE * (pos.Y + Chunk.CHUNK_SIZE * pos.Z)] = id;
			}
		}

		/// <summary>
		/// Places a structure at the given position in the given base chunk.
		/// Can be provided a blacklist of ids that it will not overwrite, and can be provided a blacklist of ids from the structure to not write to the world.
		/// </summary>
		/// <param name="manager"></param>
		/// <param name="baseChunk"></param>
		/// <param name="structure"></param>
		/// <param name="pos"></param>
		/// <param name="overwriteWorldBlacklist">Structure cubes will not overwrite cubes of this type in the world.</param>
		/// <param name="dontwriteStructureBlacklist">If the structure encounters a cube of this type when placing, it will not place it.
		/// For instance, if your structure is padded by air, you might not want to overwrite the world with that.</param>
		public static void PlaceStructureWithBlacklist(World world, ChunkManager manager, Chunk baseChunk, Structure structure, CubePosition pos,
			Span<ushort> overwriteWorldBlacklist, Span<ushort> dontwriteStructureBlacklist)
		{
			Chunk realBaseChunk = baseChunk;

			if (baseChunk == null)
				realBaseChunk = manager.GetChunk(pos);

			for (int x = 0; x < structure.size.X; x++)
			{
				for (int y = 0; y < structure.size.Y; y++)
				{
					for (int z = 0; z < structure.size.Z; z++)
					{
						Util.ThreeDToOneD(new ValuePoint3D(x, y, z), new ValuePoint3D(structure.size.X, structure.size.Y, structure.size.Z), out int i);
						CubePosition realPos = new CubePosition(pos.X + x, pos.Y + y, pos.Z + z, pos.Coord);

						bool canWrite = true;
						//Allow structure cube to be overwritten (rather, not written) by world.
						if (!dontwriteStructureBlacklist.IsEmpty)
						{
							for (int j = 0; j < dontwriteStructureBlacklist.Length; j++)
							{
								if (structure.data[i] == dontwriteStructureBlacklist[j])
									canWrite = false;
							}
						}

						//Allow world cube to be overwritten by structure
						if (!overwriteWorldBlacklist.IsEmpty)
						{
							int overwritingId = manager.GetCube(realPos).GetOrDefault(Main.Registry.CubeRegistry.Air).Id;

							for (int j = 0; j < overwriteWorldBlacklist.Length; j++)
							{
								if (overwriteWorldBlacklist[j] == overwritingId)
									canWrite = false;
							}
						}

						if (canWrite)
							ChunkHelper.SetCubeOrAdjacent(manager, realBaseChunk, realPos, structure.data[i]);
					}
				}
			}
		}

		public delegate bool ShouldWriteFn(World world, ChunkManager chunkManager, CubePosition position, ref ushort id);

		public static void PlaceStructureWithBlacklist(World world, ChunkManager manager, Chunk baseChunk, Structure structure, CubePosition pos,
			Span<ushort> overwriteWorldBlacklist, ShouldWriteFn shouldWrite)
		{
			Chunk realBaseChunk = baseChunk;

			if (baseChunk == null)
				realBaseChunk = manager.GetChunk(pos);

			for (int x = 0; x < structure.size.X; x++)
			{
				for (int y = 0; y < structure.size.Y; y++)
				{
					for (int z = 0; z < structure.size.Z; z++)
					{
						Util.ThreeDToOneD(new ValuePoint3D(x, y, z), new ValuePoint3D(structure.size.X, structure.size.Y, structure.size.Z), out int i);
						CubePosition realPos = new CubePosition(pos.X + x, pos.Y + y, pos.Z + z, pos.Coord);

						bool canWrite = true;
						ushort placeId = structure.data[i];
						if (shouldWrite != null && !shouldWrite(world, manager, realPos, ref placeId))
							canWrite = false;

						//Allow world cube to be overwritten by structure
						if (!overwriteWorldBlacklist.IsEmpty)
						{
							int overwritingId = manager.GetCube(realPos).GetOrDefault(Main.Registry.CubeRegistry.Air).Id;

							for (int j = 0; j < overwriteWorldBlacklist.Length; j++)
							{
								if (overwriteWorldBlacklist[j] == overwritingId)
									canWrite = false;
							}
						}

						if (canWrite)
							ChunkHelper.SetCubeOrAdjacent(manager, realBaseChunk, realPos, placeId);
					}
				}
			}
		}
	}
}
