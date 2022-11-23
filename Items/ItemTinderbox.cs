using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Cubes;

namespace ViMG.Items
{
    public class ItemTinderbox : Item
    {
        public ItemTinderbox() : base("tinderbox", Main.assetsManager.GetAsset<Texture2D>("swrod"), new RectangleF(32, 32, 16, 16))
        {
            name = "Tinderbox";
            description = "A tinderbox, as well as a set of flint and steel, used to light fires. Right-click on the ground to create a fire that should last you for some time.";
        }

		public override bool RightClick(Player player, Inventory inventory, int index, Vector3 facing, out float itemCooldownTime)
		{
			base.RightClick(player, inventory, index, facing, out itemCooldownTime);

			//TODO check solidity not id != 0
			var lookAtResult = player.GetWorld().Raycast(Main.camera.Position, Main.camera.Position - Main.camera.Forward * Player.INTERACT_DISTANCE,
			(Vector3 pos) =>
			{
				return player.GetWorld().ChunkManager2.IsInWorldBounds(pos) && player.GetWorld().ChunkManager2.GetCubeId(CubePosition.FromWorldSpace(pos)) != 0;
			});

			if (lookAtResult.hasHit)
			{
				if (player.GetWorld().ChunkManager2.IsInWorldBounds(lookAtResult.hit))
				{
					Cube cube = Main.Registry.CubeRegistry.Get("campfire");
					var placeAtPos = CubePosition.FromWorldSpace(lookAtResult.hit + CubePosition.ToWorldSpaceV3(lookAtResult.normal));

					if (player.GetWorld().ChunkManager2.IsInWorldBounds(placeAtPos) && cube.CanPlace(player.GetWorld(), player.GetWorld().ChunkManager2, placeAtPos)
						&& Main.inputManager.JustPressed(A1r.Input.MouseInput.RightButton))
					{
						player.GetWorld().ChunkManager2.SetCube(placeAtPos, cube.Id);

						cube.OnPlayerPlaced(player, placeAtPos);

						//cubes can be placed as fast as possible
						itemCooldownTime = 0;

						return true;
					}
				}
			}

			return false;
		}
	}
}
