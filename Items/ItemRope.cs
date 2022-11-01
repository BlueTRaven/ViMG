using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Cubes;

namespace ViMG.Items
{
    public class ItemRope : Item
    {
		private Cube cube;

        public ItemRope() : base("rope", Main.assetsManager.GetAsset<Texture2D>("cubes_textures"), new RectangleF(112, 64, 16, 16))
        {
            name = "Rope";
            description = "Sturdy, strong rope. Use it to traverse big pits!";

			cube = Main.Registry.CubeRegistry.Get("rope");
		}

		public override bool RightClick(Player player, Inventory inventory, int index, Vector3 facing, out float itemCooldownTime)
		{
			base.RightClick(player, inventory, index, facing, out itemCooldownTime);

			var lookAtResult = player.GetWorld().Raycast(Main.camera.Position, Main.camera.Position - Main.camera.Forward * Player.INTERACT_DISTANCE,
			(Vector3 pos) =>
			{
				return player.GetWorld().GetChunkManager().IsInWorldBounds(pos) && player.GetWorld().GetChunkManager().GetRaw(pos) != 0;
			});

			if (lookAtResult.hasHit)
			{
				if (player.GetWorld().GetChunkManager().IsInWorldBounds(lookAtResult.hit))
				{
					//we're placing on a pre-existing rope block.
					if (!Main.inputManager.IsPressed(Keys.LeftControl) && player.GetWorld().GetChunkManager().GetCube(lookAtResult.hit).GetOrDefault(Main.Registry.CubeRegistry.Air) == cube)
					{
						Cube currentCube = cube;
						CubePosition nextPos = CubePosition.FromWorldSpace(lookAtResult.hit);

						while (currentCube == cube)
                        {
							nextPos -= new CubePosition(0, 1, 0);

							currentCube = player.GetWorld().GetChunkManager().GetCube(nextPos).GetOrDefault(Main.Registry.CubeRegistry.Air);

							//if not in world bounds, then we can't place it, so just return false.
							if (!player.GetWorld().GetChunkManager().IsInWorldBounds(nextPos))
								return false;
                        }

						if (!currentCube.Touchable)
                        {
							if (cube.CanPlace(player.GetWorld(), player.GetWorld().GetChunkManager(), nextPos))
							{
								Chunk chunk = player.GetWorld().GetChunkManager().GetChunk(nextPos);

								if (chunk != null && chunk.Initialized)
								{
									chunk.GetData().SetCube(nextPos, cube.Id);
									inventory.Remove(index, 1);

									cube.OnPlayerPlaced(player, nextPos);

									//cubes can be placed as fast as possible
									itemCooldownTime = 0.25f;

									return true;
								}
							}
						}
					}
					else
					{
						var placeAtPos = CubePosition.FromWorldSpace(lookAtResult.hit + CubePosition.ToWorldSpaceV3(lookAtResult.normal));

						if (player.GetWorld().GetChunkManager().IsInWorldBounds(placeAtPos) && cube.CanPlace(player.GetWorld(), player.GetWorld().GetChunkManager(), placeAtPos))
						{
							Chunk chunk = player.GetWorld().GetChunkManager().GetChunk(placeAtPos);

							if (chunk != null && chunk.Initialized)
							{
								chunk.GetData().SetCube(placeAtPos, cube.Id);
								inventory.Remove(index, 1);

								cube.OnPlayerPlaced(player, placeAtPos);

								//cubes can be placed as fast as possible
								itemCooldownTime = 0.25f;

								return true;
							}
						}
					}
				}
			}

			return false;
		}
	}
}
