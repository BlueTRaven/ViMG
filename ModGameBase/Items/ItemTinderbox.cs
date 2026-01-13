using BrUtility;
using Engine.Items;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Cubes;
using ViMG.Rendering;

namespace ViMG.Items
{
    public class ItemTinderbox : Item
    {
        public ItemTinderbox() : base("tinderbox")
        {
            Client = new ClientItem(this, new RectangleF(32, 32, 16, 16));

            name = "Tinderbox";
            description = "A tinderbox, as well as a set of flint and steel, used to light fires. Right-click on the ground to create a fire that should last you for some time.";
        }

		public override bool RightClick(Player player, Inventory inventory, int index, Vector3 facing, out ActionStats actionStats)
		{
			base.RightClick(player, inventory, index, facing, out actionStats);

			//TODO check solidity not id != 0
			var lookAtResult = player.GetWorld().Raycast(Main.camera.Position, Main.camera.Position - Main.camera.Forward * Player.INTERACT_DISTANCE,
			(Vector3 pos) =>
			{
				return player.GetWorld().ChunkManager.IsInWorldBounds(pos) &&
					player.GetWorld().ChunkManager.CubeView.GetCube(CubePosition.FromWorldSpace(pos)).GetOrDefault(Main.Registry.CubeRegistry.Air).Solid;
			});

			if (lookAtResult.hasHit)
			{
				if (player.GetWorld().ChunkManager.IsInWorldBounds(lookAtResult.hit))
				{
					Cube cube = Main.Registry.CubeRegistry.Get("campfire");
					var placeAtPos = CubePosition.FromWorldSpace(lookAtResult.hit + CubePosition.ToWorldSpaceV3(lookAtResult.normal));

					if (player.GetWorld().ChunkManager.IsInWorldBounds(placeAtPos) && cube.CanPlace(player.GetWorld(), player.GetWorld().ChunkManager, placeAtPos)
						&& Main.inputManager.JustPressed(A1r.Input.MouseInput.RightButton))
					{
						player.GetWorld().ChunkManager.CubeView.SetCube(placeAtPos, cube.Id);

						cube.OnPlayerPlaced(player, placeAtPos);

						//cubes can be placed as fast as possible
						actionStats = new ActionStats();

						return true;
					}
				}
			}

			return false;
		}
	}
}
