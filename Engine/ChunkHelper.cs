using BrUtility.Src;
using Engine;
using Engine.Items;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Cubes;
using ViMG.Entities;
using ViMG.Generation;

namespace ViMG
{
	//This probably needs to be broken up into two versions;
	//one which is used for world generation (and uses InitializerView)
	//and one that is used for threaded situations (and uses ThreadedView).
    public static class ChunkHelper
    {
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
		public static void PlaceStructureWithBlacklist(ChunkManager manager, Structure structure, CubePosition pos,
			Span<ushort> overwriteWorldBlacklist, Span<ushort> dontwriteStructureBlacklist, bool markDirty)
		{
			//TODO (IMPORTANT) Performance
			//This is used often in world generation - it's important that it's fast!
			for (int z = 0; z < structure.size.Z; z++)
			{
				for (int y = 0; y < structure.size.Y; y++)
				{
					for (int x = 0; x < structure.size.X; x++)
					{
						Util.ThreeDToOneD(new ValuePoint3D(x, y, z), new ValuePoint3D(structure.size.X, structure.size.Y, structure.size.Z), out int i);
						CubePosition realPos = new CubePosition(pos.X + x, pos.Y + y, pos.Z + z, pos.Coord);

						if (manager.IsInWorldBounds(realPos))
						{
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
								int overwritingId = manager.CubeView.GetCube(realPos).GetOrDefault(GlobalState.Registry.CubeRegistry.Air).Id;

								for (int j = 0; j < overwriteWorldBlacklist.Length; j++)
								{
									if (overwriteWorldBlacklist[j] == overwritingId)
										canWrite = false;
								}
							}

							if (canWrite)
								manager.CubeView.SetCube(realPos, structure.data[i], markDirty);
						}
					}
				}
			}
		}

		public delegate bool ShouldWriteFn(EntityManager entityManager, InventoryManager inventoryManager, ChunkManager chunkManager, CubePosition position, Structure structure, int structureIndex, ref ushort id);

		public static void PlaceStructureWithBlacklist(EntityManager entityManager, InventoryManager inventoryManager, ChunkManager chunkManager, Structure structure, CubePosition pos,
			Span<ushort> overwriteWorldBlacklist, ShouldWriteFn shouldWrite, bool markDirty)
		{
			for (int z = 0; z < structure.size.Z; z++)
			{
				for (int y = 0; y < structure.size.Y; y++)
				{
					for (int x = 0; x < structure.size.X; x++)
					{
						Util.ThreeDToOneD(new ValuePoint3D(x, y, z), new ValuePoint3D(structure.size.X, structure.size.Y, structure.size.Z), out int i);
						CubePosition realPos = new CubePosition(pos.X + x, pos.Y + y, pos.Z + z, pos.Coord);

						if (chunkManager.IsInWorldBounds(realPos))
						{
							bool canWrite = true;
							ushort placeId = structure.data[i];
							if (shouldWrite != null && !shouldWrite(entityManager, inventoryManager, chunkManager, realPos, structure, i, ref placeId))
								canWrite = false;

							//Allow world cube to be overwritten by structure
							if (!overwriteWorldBlacklist.IsEmpty)
							{
								int overwritingId = chunkManager.CubeView.GetCube(realPos).GetOrDefault(GlobalState.Registry.CubeRegistry.Air).Id;

								for (int j = 0; j < overwriteWorldBlacklist.Length; j++)
								{
									if (overwriteWorldBlacklist[j] == overwritingId)
										canWrite = false;
								}
							}

							if (canWrite)
								chunkManager.CubeView.SetCube(realPos, placeId, markDirty);
						}
					}
				}
			}
		}

		public static Cube ChooseShrine(Random random)
        {
			int num = random.Next(0, 6);

			switch (num)
            {
				case 0:
					return GlobalState.Registry.CubeRegistry.Get("shrine_shimu");
				case 1:
					return GlobalState.Registry.CubeRegistry.Get("shrine_irat");
				case 2:
					return GlobalState.Registry.CubeRegistry.Get("shrine_adrath");
				case 3:
					return GlobalState.Registry.CubeRegistry.Get("shrine_akkat");
				case 4:
					return GlobalState.Registry.CubeRegistry.Get("shrine_gidamu");
				case 5:
					return GlobalState.Registry.CubeRegistry.Get("shrine_arat");
				default: return null;
			}
        }

		public static bool CanPlaceIfNonSolid(ChunkManager manager, CubePosition positionInCubeSpace, out Cube offsetCube)
        {
			if (manager.IsInWorldBounds(positionInCubeSpace))
			{
				offsetCube = manager.CubeView.GetCube(positionInCubeSpace).GetOrDefault(GlobalState.Registry.CubeRegistry.Air);
				if (!offsetCube.Solid)
				{
					return true;
				}
			}

			offsetCube = null;
			return false;
		}

		public static List<CubePosition> SelectAllCubesInLine(CubePosition start, CubePosition end)
		{
			List<CubePosition> positions = new List<CubePosition>();

			Vector3 startWS = start.InWorldSpace();
			Vector3 endWS = end.InWorldSpace();

			const float ONE_CUBE = Cube.CUBE_SCALE;

			float x1 = startWS.X / ONE_CUBE;
			float y1 = startWS.Y / ONE_CUBE;
			float z1 = startWS.Z / ONE_CUBE;
			float x2 = endWS.X / ONE_CUBE;
			float y2 = endWS.Y / ONE_CUBE;
			float z2 = endWS.Z / ONE_CUBE;

			int i = (int)x1;
			int j = (int)y1;
			int k = (int)z1;

			int iend = (int)x2;
			int jend = (int)y2;
			int kend = (int)z2;

			int di = ((x1 < x2) ? 1 : ((x1 > x2) ? -1 : 0));
			int dj = ((y1 < y2) ? 1 : ((y1 > y2) ? -1 : 0));
			int dk = ((z1 < z2) ? 1 : ((z1 > z2) ? -1 : 0));

			float deltatx = 1.0f / Math.Abs(x2 - x1);
			float deltaty = 1.0f / Math.Abs(y2 - y1);
			float deltatz = 1.0f / Math.Abs(z2 - z1);

			float minx = (int)x1, maxx = minx + 1;
			float tx = ((x1 > x2) ? (x1 - minx) : (maxx - x1)) * deltatx;
			float miny = (int)y1, maxy = miny + 1;
			float ty = ((y1 > y2) ? (y1 - miny) : (maxy - y1)) * deltaty;
			float minz = (int)z1, maxz = minz + 1;
			float tz = ((z1 > z2) ? (z1 - minz) : (maxz - z1)) * deltatz;

			Vector3 hitPos = new Vector3(x1 * ONE_CUBE, y1 * ONE_CUBE, z1 * ONE_CUBE);

			while (true)
			{
				positions.Add(CubePosition.FromWorldSpace(hitPos));

				if (tx <= ty && tx <= tz)
				{
					if (i == iend)
						break;
					tx += deltatx;
					i += di;

					if (di == 1) hitPos.X += ONE_CUBE;
					if (di == -1) hitPos.X -= ONE_CUBE;
				}
				else if (ty <= tz)
				{
					if (j == jend)
						break;
					ty += deltaty;
					j += dj;

					if (dj == 1) hitPos.Y += ONE_CUBE;
					if (dj == -1) hitPos.Y -= ONE_CUBE;
				}
				else
				{
					if (k == kend)
						break;
					tz += deltatz;
					k += dk;

					if (dk == 1) hitPos.Z += ONE_CUBE;
					if (dk == -1) hitPos.Z -= ONE_CUBE;
				}
			}

			return positions;
		}

		public static void SelectInArea(FastStackList<CubePosition> selected, ChunkManager manager, Rectangle3DI bounds, ushort ofType)
        {
			Debug.Assert(selected.Capacity >= bounds.Size.X * bounds.Size.Y * bounds.Size.Z);

			//List<CubePosition> selected = new List<CubePosition>();

			for (int z = bounds.Position.Z; z <= bounds.FarPosition.Z; z++)
            {
				for (int y = bounds.Position.Y; y <= bounds.FarPosition.Y; y++)
                {
					for (int x = bounds.Position.X; x <= bounds.FarPosition.X; x++)
                    {
						CubePosition pos = new CubePosition(x, y, z, CubePosition.CoordinateSpace.CubeSpace);

						if (manager.IsInWorldBounds(pos) && manager.CubeView.GetCube(pos).GetOrDefault(GlobalState.Registry.CubeRegistry.Air).Id == ofType)
                        {
							selected.Add(pos);
                        }
                    }
				}
			}
        }
	}
}
