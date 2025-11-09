using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Items;

namespace ViMG.Cubes
{
	public class CubeGrass : Cube
	{
		public CubeGrass() : base("grass", new CubeFacingLayout(new RectangleF(32, 0, 16, 16), new RectangleF(48, 0, 16, 16), new RectangleF(0, 0, 16, 16)), Color.White, 2)
		{
			Name = "Grass";
		}

		public override void GetDrops(List<ItemInstance> itemsToDrop)
		{
			base.GetDrops(itemsToDrop);

			itemsToDrop.Add(new ItemInstance(Main.Registry.ItemRegistry.Get("item_dirt"), 1, 1));
		}

		private Cube dirt;
		//No down Y as that is guaranteed to be covered by this cube, and thus not valid to spread to.
		private CubePosition[] offsets = new CubePosition[13]
		{
			new CubePosition(-1, 0, 0),
			new CubePosition(1, 0, 0),
			new CubePosition(0, 1, 0),
			new CubePosition(0, 0, -1),
			new CubePosition(0, 0, 1),
			new CubePosition(-1, 1, 0),
			new CubePosition(-1, -1, 0),
			new CubePosition(1, 1, 0),
			new CubePosition(1, -1, 0),
			new CubePosition(0, 1, -1),
			new CubePosition(0, -1, -1),
			new CubePosition(0, 1, 1),
			new CubePosition(0, -1, 1),
		};
        public override void OnRandomUpdate(World world, ChunkManager manager, CubePosition position)
        {
            base.OnRandomUpdate(world, manager, position);

			if (dirt == null)
				dirt = Main.Registry.CubeRegistry.Get("dirt");

			Span<CubePosition> positions = stackalloc CubePosition[13];
			Span<ushort> ids = stackalloc ushort[13];
			int pi = 0;

			for (int i = 0; i < 13; i++)
			{
				CubePosition offsetPosition = position + offsets[i];
				if (world.ChunkManager.IsInWorldBounds(offsetPosition) && world.ChunkLoadManager.IsLoaded(ChunkPosition.CubeChunk(offsetPosition)))
				{
					positions[i] = offsetPosition;
					pi++;
				}
			}

			manager.CubeView.GetIds(positions[..pi], ids[..pi]);

			for (int i = 0; i < 13; i++)
			{
				CubePosition offsetPosition = positions[i];
				ushort id = ids[i];

				if (world.ChunkLoadManager.IsLoaded(ChunkPosition.CubeChunk(offsetPosition)))
				{
					//ushort id = manager.GetCubeId(offsetPosition);
					if (id == dirt.Id)
					{
						//check the block above; grass cannot have a block above it
						CubePosition abovePosition = offsetPosition + new CubePosition(0, 1, 0, CubePosition.CoordinateSpace.CubeSpace);
						
						if (world.ChunkLoadManager.IsLoaded(ChunkPosition.CubeChunk(abovePosition)))
						{
							if (!manager.CubeView.GetCube(abovePosition).GetOrDefault(Main.Registry.CubeRegistry.Air).Solid)
							{
								//set dirt to grass
								manager.CubeView.SetCube(offsetPosition, Id);

								//if spreading UP
								if (i == 2)
								{
									//Set self to dirt.
									//We don't need to check to see if the chunk is valid as only valid chunks have random cube updates performed in them.
									manager.CubeView.SetCube(position, dirt.Id);
								}
							}
						}
					}
				}
			}
        }

		public override void OnAdjacentUpdated(World world, ChunkManager manager, CubePosition position, CubePosition updating, int updatedId, double updatedTime)
		{
			base.OnAdjacentUpdated(world, manager, position, updating, updatedId, updatedTime);

			//top block is updating.
			if (updating.Y == position.Y + 1)
            {
				if (updatedId != 0 && Main.Registry.CubeRegistry.Get(updatedId).Solid)
                {
					manager.CubeView.SetCube(position, dirt.Id);
                }
            }
        }
    }
}
