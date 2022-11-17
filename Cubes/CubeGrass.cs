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
        public override void OnRandomUpdate(World world, ChunkManager manager, ChunkData chunkData, CubePosition position)
        {
            base.OnRandomUpdate(world, manager, chunkData, position);

			if (dirt == null)
				dirt = Main.Registry.CubeRegistry.Get("dirt");

			for (int i = 0; i < 13; i++) 
			{
				CubePosition offsetPosition = position + offsets[i];

				Chunk chunk = manager.GetChunk(offsetPosition);

				if (chunk != null && chunk.Initialized)
				{
					var instance = chunk.GetData().GetCubeInstance(offsetPosition.InCubeSpace(chunk));

					if (instance.valid)
					{
						if (instance.cubeId == dirt.Id)
						{
							//check the block above to see if 
							CubePosition abovePosition = offsetPosition + new CubePosition(0, 1, 0, CubePosition.CoordinateSpace.CubeSpace);
							Chunk aboveChunk = manager.GetChunk(abovePosition);

							if (aboveChunk != null && aboveChunk.Initialized)
                            {
								if (!aboveChunk.GetData().GetCube(abovePosition).GetOrDefault(Main.Registry.CubeRegistry.Air).Solid)
                                {
									//set dirt to grass
									chunk.GetData().SetCube(offsetPosition.InCubeSpace(chunk), Id);

									//if spreading UP
									if (i == 2)
                                    {
										//Set self to dirt.
										//We don't need to check to see if the chunk is valid as only valid chunks have random cube updates performed in them.
										manager.GetChunk(position).GetData().SetCube(position, dirt.Id);
                                    }
                                }
                            }
						}
					}
				}
			}
        }

        public override void OnAdjacentUpdated(ChunkData parent, CubePosition position, ChunkData updatingParent, CubePosition updating, int updatedId)
        {
            base.OnAdjacentUpdated(parent, position, updatingParent, updating, updatedId);

			//top block is updating.
			if (updating.Y == position.Y + 1)
            {
				if (updatedId != 0 && Main.Registry.CubeRegistry.Get(updatedId).Touchable)
                {
					parent.SetCubeFast(position.InChunkSpace(parent.GetChunk()), dirt.Id);
                }
            }
        }
    }
}
