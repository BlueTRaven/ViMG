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
			name = "Glow Node";
			description = "A chunk of wood coated in glowdust. It shimmers brightly, no matter the time of day.";
		}

		public override bool RightClick(Player player, Inventory inventory, int index, Vector3 facing, out float itemCooldownTime)
		{
			base.RightClick(player, inventory, index, facing, out itemCooldownTime);

			//TODO check solidity not id != 0
			var lookAtResult = player.GetWorld().Raycast(Main.camera.Position, Main.camera.Position - Main.camera.Forward * Player.INTERACT_DISTANCE,
			(Vector3 pos) =>
			{
				return player.GetWorld().ChunkManager2.IsInWorldBounds(pos) && 
					player.GetWorld().ChunkManager2.ThreadedView.GetCube(CubePosition.FromWorldSpace(pos)).GetOrDefault(Main.Registry.CubeRegistry.Air).Solid;
			});

			if (lookAtResult.hasHit)
			{
				if (player.GetWorld().ChunkManager2.IsInWorldBounds(lookAtResult.hit))
				{
					var placeAtPos = CubePosition.FromWorldSpace(lookAtResult.hit + CubePosition.ToWorldSpaceV3(lookAtResult.normal));

					if (player.GetWorld().ChunkManager2.IsInWorldBounds(placeAtPos) && Main.inputManager.JustPressed(A1r.Input.MouseInput.RightButton))
					{
						Cube glowNode = Main.Registry.CubeRegistry.Get("glow_node");
						player.world.ChunkManager2.ThreadedView.SetCube(placeAtPos, glowNode.Id);
						glowNode.OnPlayerPlaced(player, placeAtPos);
						inventory.Remove(index, 1);

						return true;
					}
				}
			}

			return false;
		}
	}
}
