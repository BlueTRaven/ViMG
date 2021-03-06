using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Cubes;

namespace ViMG.Items
{
	public class ItemCube : Item
	{
		private int cubeId;

		public ItemCube(Cube cube, int cubeId) : base("item_" + cube.Identifier, Main.assetsManager.GetAsset<Texture2D>("cubes_textures"), cube.GetSourceRect(MeshHelper.CubeFace.FRONT))
		{
			this.cubeId = cubeId;
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
						Chunk chunk = player.GetWorld().GetChunkManager().GetChunk(placeAtPos);
						chunk.GetData().SetCube(placeAtPos, cubeId);
						inventory.Remove(index, 1);

						return true;
					}
				}
			}

			return false;
		}

		public override void Draw(GraphicsDevice device, Matrix transform)
		{
			base.Draw(device, transform);

			Cube cube = Main.Registry.CubeRegistry.Get(cubeId);
			var mesh = cube.GetMesh(device);

			mesh.Draw(device, Main.CubeEffect, transform);
		}
	}
}
