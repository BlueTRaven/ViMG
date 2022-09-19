using BrUtility;
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
		private ushort cubeId;

		public ItemCube(Cube cube, ushort cubeId) : base("item_" + cube.Identifier, Main.assetsManager.GetAsset<Texture2D>("cubes_textures"), 
			cube.GetSourceRect(Cube.RenderPass.Opaque, MeshHelper.CubeFace.FRONT))
		{
			this.cubeId = cubeId;
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
					var placeAtPos = CubePosition.FromWorldSpace(lookAtResult.hit + CubePosition.ToWorldSpaceV3(lookAtResult.normal));

					if (player.GetWorld().GetChunkManager().IsInWorldBounds(placeAtPos) && Main.inputManager.JustPressed(A1r.Input.MouseInput.RightButton))
					{
						Chunk chunk = player.GetWorld().GetChunkManager().GetChunk(placeAtPos);
						chunk.GetData().SetCube(placeAtPos, cubeId);
						inventory.Remove(index, 1);

						Main.Registry.CubeRegistry.Get(cubeId).OnPlayerPlaced(player, placeAtPos);

						//cubes can be placed as fast as possible
						itemCooldownTime = 0;

						return true;
					}
				}
			}

			return false;
		}

		public override void DrawInWorld(GraphicsDevice device, World world, ItemInstance item, Matrix transform)
		{
			//base.Draw(device, transform);

			Cube cube = Main.Registry.CubeRegistry.Get(cubeId);
			var mesh = cube.GetMesh(device);

			Main.Renderer.DrawsPassGBuffer.Add(new Rendering.RendererDeferred.GBufferDraw(mesh.texture, DrawHelper.BlackPixel, DrawHelper.BlackPixel,
				mesh.VBO, mesh.IBO, transform, null));
		}
	}
}
