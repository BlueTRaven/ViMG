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
			cube.GetSourceRect(Cube.RenderPass.Opaque, null, new CubePosition(), MeshHelper.CubeFace.FRONT))
		{
			this.cubeId = cubeId;

			name = Main.Registry.CubeRegistry.Get(cubeId).Name;
			description = Main.Registry.CubeRegistry.Get(cubeId).Description;
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
					Cube cube = Main.Registry.CubeRegistry.Get(cubeId);
					var placeAtPos = CubePosition.FromWorldSpace(lookAtResult.hit + CubePosition.ToWorldSpaceV3(lookAtResult.normal));

					if (player.GetWorld().GetChunkManager().IsInWorldBounds(placeAtPos) && cube.CanPlace(player.GetWorld(), player.GetWorld().GetChunkManager(), placeAtPos))
					{
						Chunk chunk = player.GetWorld().GetChunkManager().GetChunk(placeAtPos);
						chunk.GetData().SetCube(placeAtPos, cubeId);
						inventory.Remove(index, 1);

						cube.OnPlayerPlaced(player, placeAtPos);

						//cubes can be placed as fast as possible
						itemCooldownTime = 0.25f;

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
			var mesh = cube.GetHeldMesh(device);

			Main.Renderer.DrawsPassGBuffer.Add(new Rendering.RendererDeferred.GBufferDraw(mesh.texture, DrawHelper.BlackPixel, DrawHelper.BlackPixel,
				mesh.VBO, mesh.IBO, transform, cube.GetHeldSourceRect(world)));
		}
	}
}
