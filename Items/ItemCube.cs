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

		public ItemCube(Cube cube, int cubeId)
		{
			this.cubeId = cubeId;
			Texture = Main.assetsManager.GetAsset<Texture2D>("cubes_textures");
			SourceRect = cube.GetSourceRect(MeshHelper.CubeFace.FRONT);
		}

		public override bool RightClick(Player player, Vector3 facing)
		{
			base.RightClick(player, facing);

			var lookAtResult = player.GetWorld().Raycast(-Main.camera.Position, -Main.camera.Position - Main.camera.Forward * Player.INTERACT_DISTANCE,
			(Vector3 pos) =>
			{
				return player.GetWorld().IsInWorldBounds(pos) && player.GetWorld().GetRaw(pos) != 0;
			});

			if (lookAtResult.hasHit)
			{
				if (player.GetWorld().IsInWorldBounds(lookAtResult.hit))
				{
					var placeAtPos = CubePosition.FromWorldSpace(lookAtResult.hit + CubePosition.ToWorldSpaceV3(lookAtResult.normal));

					if (player.GetWorld().IsInWorldBounds(placeAtPos) && Main.inputManager.JustPressed(A1r.Input.MouseInput.RightButton))
					{
						Chunk chunk = player.GetWorld().GetChunkManager().GetChunk(placeAtPos);
						chunk.GetData().SetCube(placeAtPos, cubeId);

						return true;
					}
				}
			}

			return false;
		}

		public override void Draw(GraphicsDevice device, Player player, Vector3 facing)
		{
			base.Draw(device, player, facing);

			Cube cube = player.GetWorld().CubeRegistry.Get(cubeId);
			var mesh = cube.GetMesh(device);

			mesh.Draw(device, Main.CubeEffect, player.GetHeldMatrix());
		}
	}
}
