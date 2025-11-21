using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Cubes;
using ViMG.Rendering;

namespace ViMG.Items
{
    public class ItemCube : Item
	{
		private ushort cubeId;

		public ItemCube(Cube cube, ushort cubeId) : base("item_" + cube.Identifier, 
			cube.GetHeldSourceRect())
		{
			this.cubeId = cubeId;

			name = Main.Registry.CubeRegistry.Get(cubeId).Name;
			description = Main.Registry.CubeRegistry.Get(cubeId).Description;
		}

		public override bool RightClick(Player player, Inventory inventory, int index, Vector3 facing, out ActionStats actionStats)
		{
			base.RightClick(player, inventory, index, facing, out actionStats);

			if (player.IsLooking && player.CanPlace)
			{
				if (player.world.ChunkLoadManager.IsLoaded(ChunkPosition.CubeChunk(player.PlaceAtPos)))
				{
					player.world.ChunkManager.CubeView.SetCube(player.PlaceAtPos, cubeId, player);
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

        public override RendererDeferred.DrawMaterial GetMaterial()
        {
            return StaticMaterials.Cubes;
        }

        public override void DrawInWorld(GraphicsDevice device, World world, ItemInstance item, Matrix transform)
		{
			//base.Draw(device, transform);

			Cube cube = Main.Registry.CubeRegistry.Get(cubeId);
			var mesh = cube.GetHeldMesh(device);

			Matrix scaled = Matrix.CreateScale(0.35f) * transform;
			if (mesh.IBO != null)
				Main.Renderer.AddOpaqueDraw(new Rendering.RendererDeferred.GBufferDraw(GetMaterial(),
					mesh, scaled, cube.GetHeldSourceRect(world)));
		}
	}
}
