using BrUtility;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Entities;
using ViMG.Items;

namespace ViMG.Cubes
{
	public class CubeTree : Cube
	{
		public CubeTree() : base("tree", new RectangleF(64, 0, 16, 16), Color.White, 2)
		{
			Transparency = TransparencyValue.Invisible;
		}

		public override void PostChunkInit(ChunkData chunkData, CubePosition position)
		{
			base.PostChunkInit(chunkData, position);

			int size = 0;

			// don't generate a tree if there's a tree below us. We only want the base cube to care.
			if (chunkData.GetChunk().GetWorld().GetChunkManager()
				.GetCube(position.InCubeSpace(chunkData.GetChunk()) - new CubePosition(0, 1, 0, CubePosition.CoordinateSpace.CubeSpace))
				.GetOrDefault(Main.Registry.CubeRegistry.Air) != this)
			{
				//List<CubePosition> listenPositions = new List<CubePosition>();
				for (int i = 0; i < 12; i++)
				{
					CubePosition pos = position.InCubeSpace(chunkData.GetChunk()) + new CubePosition(0, i, 0, CubePosition.CoordinateSpace.CubeSpace);
					if (chunkData.GetChunk().GetWorld().GetChunkManager()
						.GetCube(pos)
						.GetOrDefault(Main.Registry.CubeRegistry.Air) == this)
					{
						size = i;
					}
					else break;
				}

				Tree tree = new Tree(position.InWorldSpace(chunkData.GetChunk()) - new Vector3(CUBE_SCALE * 1.25f, 0, CUBE_SCALE * 1.25f), 
					size, position.InCubeSpace(chunkData.GetChunk()));
				chunkData.GetChunk().GetWorld().EntityManager.Add(tree);
			}
		}

		public override void GetDrops(List<ItemInstance> itemsToDrop)
		{
			base.GetDrops(itemsToDrop);

			itemsToDrop.Add(new ItemInstance(Main.Registry.ItemRegistry.Get("wood"), 1, 1));
		}

		public override void OnAdjacentUpdated(ChunkData parent, CubePosition position, ChunkData updatingParent, CubePosition updating, int updatedId)
		{
			base.OnAdjacentUpdated(parent, position, updatingParent, updating, updatedId);

			if (position.Y > updating.Y && updatedId == 0)
			{
				parent.GetChunk().GetWorld().MineCube(position, true);
				//parent.SetCube(position, 0);
			}
		}
	}
}
