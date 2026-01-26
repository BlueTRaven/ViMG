using BrUtility;
using Engine;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Entities;
using ViMG.GameStates;
using ViMG.Items;

namespace ViMG.Cubes
{
	public class CubeAncientAltar : Cube
	{
		private readonly bool dropsSelf;

		public CubeAncientAltar(bool dropsSelf) : base(dropsSelf ? "ancient_altar_placeable" : "ancient_altar_generated", 12, dropsSelf ? 0 : 1)
		{
            this.dropsSelf = dropsSelf;

            Client = new(this, new CubeFacingLayout(new RectangleF(80, 16, 16, 16), new RectangleF(96, 16, 16, 16), new RectangleF(96, 16, 16, 16)), Color.White);
        }

		public override void GetDrops(List<ItemInstance> itemsToDrop)
		{
			base.GetDrops(itemsToDrop);

			if (dropsSelf)
				DropSelf(itemsToDrop);
			else itemsToDrop.Add(new ItemInstance(GlobalState.Registry.ItemRegistry.Get("altar_dust"), 1, 1));
		}

		public override void PostChunkGen(WorldPrototype world, CubePosition position)
		{
			base.PostChunkGen(world, position);

			world.AddEntity(new AncientAltar(position, CUBE_SCALE * 10));
		}

		public override void OnPlayerPlaced(Player player, CubePosition position)
		{
			base.OnPlayerPlaced(player, position);

			player.GetWorld().EntityManager.Add(new AncientAltar(position, CUBE_SCALE * 10));
		}
	}
}
