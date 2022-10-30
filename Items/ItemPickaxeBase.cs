using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Cubes;

namespace ViMG.Items
{
	public class ItemPickaxeBase : Item
	{
		public ItemPickaxeBase() : base("pickaxe_base", Main.assetsManager.GetAsset<Texture2D>("swrod"), new BrUtility.RectangleF(16, 0, 16, 16))
		{
		}

		public override bool LeftClick(Player player, Inventory inventory, int index, Vector3 facing, out float itemCooldownTime)
		{
			base.LeftClick(player, inventory, index, facing, out itemCooldownTime);

			var lookAtResult = player.GetWorld().Raycast(player.Position, player.Position + facing * Player.INTERACT_DISTANCE,
			(Vector3 pos) =>
			{
				return player.GetWorld().GetChunkManager().IsInWorldBounds(pos) && player.GetWorld().GetChunkManager().GetRaw(pos) != 0;
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

							if (player.GetWorld().GetChunkManager().IsInWorldBounds(minePos))
							{
								player.GetWorld().TryMineCube(minePos, 0, 1);
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
