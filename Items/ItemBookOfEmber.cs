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
    public class ItemBookOfEmber : Item
    {
        public ItemBookOfEmber() : base("book_spell_ember", Main.assetsManager.GetAsset<Texture2D>("swrod"), new RectangleF(64, 32, 16, 16))
        {
			name = "Spellbook: Ember";
			description = "A spellbook with an explanation of how to cast \"Ember\".\n" +
				"This spell will light a small fire on any surface in front of you.\n" +
				"Costs 1 magic.";

			flipXInHand = true;
        }

		public override bool RightClick(Player player, Inventory inventory, int index, Vector3 facing, out float itemCooldownTime)
		{
			base.RightClick(player, inventory, index, facing, out itemCooldownTime);

			if (player.Magic < 1)
				return false;

			var lookAtResult = player.GetWorld().Raycast(Main.camera.Position, Main.camera.Position - Main.camera.Forward * Player.INTERACT_DISTANCE,
			(Vector3 pos) =>
			{
				return player.GetWorld().GetChunkManager().IsInWorldBounds(pos) && player.GetWorld().GetChunkManager().GetRaw(pos) != 0;
			});

			if (lookAtResult.hasHit)
			{
				if (player.GetWorld().GetChunkManager().IsInWorldBounds(lookAtResult.hit))
				{
					Cube cube = Main.Registry.CubeRegistry.Get("flame");
					var placeAtPos = CubePosition.FromWorldSpace(lookAtResult.hit + CubePosition.ToWorldSpaceV3(lookAtResult.normal));

					if (player.GetWorld().GetChunkManager().IsInWorldBounds(placeAtPos) && cube.CanPlace(player.GetWorld(), player.GetWorld().GetChunkManager(), placeAtPos)
						&& Main.inputManager.JustPressed(A1r.Input.MouseInput.RightButton))
					{
						Chunk chunk = player.GetWorld().GetChunkManager().GetChunk(placeAtPos);
						chunk.GetData().SetCube(placeAtPos, cube.Id);

						cube.OnPlayerPlaced(player, placeAtPos);

						player.Magic -= 1;

						itemCooldownTime = 0.25f;

						return true;
					}
				}
			}

			return false;
		}
	}
}
