using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Cubes;
using ViMG.Entities;

namespace ViMG.Items
{
	public class ItemGlowNode : Item
	{
		public ItemGlowNode() : base("glow_node", Main.assetsManager.GetAsset<Texture2D>("glow_node"), new RectangleF(0, 0, 16, 16))
		{
			Name = "Glow Node";
			Description = "A chunk of wood coated in glowdust. It shimmers brightly, no matter the time of day.";
		}

		public override bool RightClick(Player player, Inventory inventory, int index, Vector3 facing)
		{
			var lookAtResult = player.GetWorld().Raycast(-Main.camera.Position, -Main.camera.Position - Main.camera.Forward * Player.INTERACT_DISTANCE,
			(Vector3 pos) =>
			{
				return player.GetWorld().GetChunkManager().IsInWorldBounds(pos) && player.GetWorld().GetChunkManager().GetRaw(pos) != 0;
			});

			if (lookAtResult.hasHit)
			{
				if (player.GetWorld().GetChunkManager().IsInWorldBounds(lookAtResult.hit))
				{
					var placeAtPos = CubePosition.FromWorldSpace(lookAtResult.hit + CubePosition.ToWorldSpaceV3(lookAtResult.normal));

					if (player.GetWorld().GetChunkManager().IsInWorldBounds(placeAtPos) && Main.inputManager.JustPressed(A1r.Input.MouseInput.RightButton))
					{
						player.GetWorld().EntityManager.Add(new GlowNode(placeAtPos.InWorldSpace(null) + new Vector3(Cube.CUBE_SCALE / 2, Cube.CUBE_SCALE, Cube.CUBE_SCALE / 2f), 100, 16, Color.White));

						/*Chunk chunk = player.GetWorld().GetChunkManager().GetChunk(placeAtPos);
						chunk.GetData().SetCube(placeAtPos, cubeId);*/
						inventory.Remove(index, 1);

						return true;
					}
				}
			}

			return false;
		}
	}
}
