using BrUtility;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Entities;
using ViMG.Items;

namespace ViMG.Cubes
{
	public class CubeAncientAltar : Cube
	{
		public CubeAncientAltar() : base("ancient_altar", new CubeFacingLayout(new RectangleF(80, 16, 16, 16), new RectangleF(96, 16, 16, 16), new RectangleF(96, 16, 16, 16)), Color.White, 12)
		{
		}

		public override void GetDrops(List<ItemInstance> itemsToDrop)
		{
			base.GetDrops(itemsToDrop);

			itemsToDrop.Add(new ItemInstance(Main.Registry.ItemRegistry.Get("altar_dust"), 1, 1));
		}

		public override void PostChunkGen(ChunkData chunkData, CubePosition position)
		{
			base.PostChunkGen(chunkData, position);

			chunkData.GetChunk().GetWorld().EntityManager.Add(new AncientAltar(position.InCubeSpace(chunkData.GetChunk()), CUBE_SCALE * 10));
		}

		public override void OnPlayerPlaced(Player player, CubePosition position)
		{
			base.OnPlayerPlaced(player, position);

			player.GetWorld().EntityManager.Add(new AncientAltar(position, CUBE_SCALE * 10));
		}
	}
}
