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

		public override bool LeftClick(Player player, Inventory inventory, int index, Vector3 facing, out float itemCooldownTime)
		{
			base.LeftClick(player, inventory, index, facing, out itemCooldownTime);

			if (player.Magic < 1)
				return false;

			var lookAtResult = player.GetWorld().Raycast(Main.camera.Position, Main.camera.Position - Main.camera.Forward * Player.INTERACT_DISTANCE,
			(Vector3 pos) =>
			{
				return player.GetWorld().GetChunkManager().IsInWorldBounds(pos) && player.GetWorld().GetChunkManager().GetRaw(pos) != 0;
			});

			Vector3 hitPos = lookAtResult.hasHit ? lookAtResult.hit : lookAtResult.end;
			Vector3 placeOffset = lookAtResult.hasHit ? CubePosition.ToWorldSpaceV3(lookAtResult.normal) : Vector3.Zero;

			if (player.GetWorld().GetChunkManager().IsInWorldBounds(hitPos))
			{
				Cube cube = Main.Registry.CubeRegistry.Get("flame");
				var placePosCS = CubePosition.FromWorldSpace(hitPos + placeOffset);

				if (player.GetWorld().GetChunkManager().IsInWorldBounds(placePosCS) && cube.CanPlace(player.GetWorld(), player.GetWorld().GetChunkManager(), placePosCS)
					&& Main.inputManager.JustPressed(A1r.Input.MouseInput.LeftButton))
				{
					Chunk chunk = player.GetWorld().GetChunkManager().GetChunk(placePosCS);
					chunk.GetData().SetCube(placePosCS, cube.Id);

					cube.OnPlayerPlaced(player, placePosCS);

					player.Magic -= 1;

					itemCooldownTime = 0.25f;

					return true;
				}
			}

			return false;
		}
	}
}
