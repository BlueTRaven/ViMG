using BrUtility;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Entities;
using ViMG.Items;

namespace ViMG.Cubes
{
	public class CubeChest : Cube
	{
		private string tier;
		private int rows, columns;
		private int slots;

		public CubeChest(string tier, int rows, int columns) : base("chest_" + tier, 
			new CubeFacingLayout(new RectangleF(144, 16, 16, 16), new RectangleF(160, 16, 16, 16), new RectangleF(160, 16, 16, 16)), Color.White, 8)
		{
			this.tier = tier;
			this.rows = rows;
			this.columns = columns;
			this.slots = rows * columns;
		}

		public override void PostChunkGen(ChunkData chunkData, CubePosition position)
		{
			base.PostChunkGen(chunkData, position);
		}

		public override void OnPlayerPlaced(Player player, CubePosition position)
		{
			base.OnPlayerPlaced(player, position);

			player.GetWorld().EntityManager.Add(new EntityChest(position, rows, columns));
		}

		public override void GetDrops(List<ItemInstance> itemsToDrop)
		{
			base.GetDrops(itemsToDrop);

			DropSelf(itemsToDrop);
		}
	}
}
