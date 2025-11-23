using Engine.Items;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Cubes;
using ViMG.Rendering;

namespace ViMG.Items
{
    public class ItemPickaxeBase : Item
	{
		public ItemPickaxeBase() : base("pickaxe_base", new BrUtility.RectangleF(16, 0, 16, 16))
		{
		}

		public override bool LeftClick(Player player, Inventory inventory, int index, Vector3 facing, out ActionStats actionStats)
		{
			base.LeftClick(player, inventory, index, facing, out actionStats);
			
			//TODO check solidity not id != 0
			var lookAtResult = player.GetWorld().Raycast(player.Position, player.Position + facing * Player.INTERACT_DISTANCE,
			(Vector3 pos) =>
			{
				return player.GetWorld().ChunkManager.IsInWorldBounds(pos) && 
					player.GetWorld().ChunkManager.CubeView.GetCube(CubePosition.FromWorldSpace(pos)).GetOrDefault(Main.Registry.CubeRegistry.Air).Solid;
			});

			if (lookAtResult.hasHit)
			{
				var lookAtPos = CubePosition.FromWorldSpace(lookAtResult.hit);

				for (int x = -1; x <= 1; x++)
				{
					for (int y = -1; y <= 1; y++)
					{
						for (int z = -1; z <= 1; z++)
						{
							CubePosition minePos = lookAtPos;
							minePos.X += x;
							minePos.Y += y;
							minePos.Z += z;

							if (player.GetWorld().ChunkManager.IsInWorldBounds(minePos))
							{
								player.GetWorld().TryMineCube(player, minePos, 0, 1);
							}
						}
					}
				}

				//player.GetWorld().MineCube(lookAtPos);
			}

			return true;
		}
	}
}
