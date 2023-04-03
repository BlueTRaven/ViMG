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
			cube.GetHeldSourceRect())
		{
			this.cubeId = cubeId;

			name = Main.Registry.CubeRegistry.Get(cubeId).Name;
			description = Main.Registry.CubeRegistry.Get(cubeId).Description;
		}

		public override bool RightClick(Player player, Inventory inventory, int index, Vector3 facing, out Player.ActionStats actionStats)
		{
			base.RightClick(player, inventory, index, facing, out actionStats);

			if (player.IsLooking && player.CanPlace)
			{
				if (player.world.ChunkLoadManager.IsLoaded(ChunkPosition.CubeChunk(player.PlaceAtPos)))
				{
					player.world.ChunkManager.ThreadedView.SetCube(player.PlaceAtPos, cubeId);
					inventory.Remove(index, 1);
					Cube cube = Main.Registry.CubeRegistry.Get(cubeId);
					cube.OnPlayerPlaced(player, player.PlaceAtPos);
					//player.world.ChunkManager2.SetCube(player.PlaceAtPos, cubeId);

					//player.world.ChunkLoadManager.ReloadChunk(player.world, ChunkPosition.CubeChunk(player.PlaceAtPos));

					//cubes can be placed as fast as possible
					actionStats.useTime = 0.25f;
					actionStats.useAnimTime = 0.25f;

					return true;
				}
			}

			return false;
		}

		public override void DrawInWorld(GraphicsDevice device, World world, ItemInstance item, Matrix transform)
		{
			//base.Draw(device, transform);

			Cube cube = Main.Registry.CubeRegistry.Get(cubeId);
			var mesh = cube.GetHeldMesh(device);

			Matrix scaled = Matrix.CreateScale(0.35f) * transform;
			if (mesh.VBO != null)
				Main.Renderer.DrawsPassGBuffer.Add(new Rendering.RendererDeferred.GBufferDraw(Texture, DrawHelper.BlackPixel, DrawHelper.BlackPixel,
					mesh.VBO, mesh.IBO, scaled, cube.GetHeldSourceRect(world)));
		}
	}
}
