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
		private readonly bool dropsSelf;

		public CubeAncientAltar(bool dropsSelf) : base(dropsSelf ? "ancient_altar_placeable" : "ancient_altar_generated", new CubeFacingLayout(new RectangleF(80, 16, 16, 16), new RectangleF(96, 16, 16, 16), new RectangleF(96, 16, 16, 16)), Color.White, 12, dropsSelf ? 0 : 1)
		{
            this.dropsSelf = dropsSelf;
        }

		public override void GetDrops(List<ItemInstance> itemsToDrop)
		{
			base.GetDrops(itemsToDrop);

			if (dropsSelf)
				DropSelf(itemsToDrop);
			else itemsToDrop.Add(new ItemInstance(Main.Registry.ItemRegistry.Get("altar_dust"), 1, 1));
		}

		public override void PostChunkGen(World world, ChunkManager manager, CubePosition position)
		{
			base.PostChunkGen(world, manager, position);

			world.EntityManager.Add(new AncientAltar(position, CUBE_SCALE * 10));
		}

		public override void OnPlayerPlaced(Player player, CubePosition position)
		{
			base.OnPlayerPlaced(player, position);

			player.GetWorld().EntityManager.Add(new AncientAltar(position, CUBE_SCALE * 10));
		}
	}
}
