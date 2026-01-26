using BrUtility;
using Engine;
using Engine.Items;
using Engine.Networking.Messages;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Cubes;
using ViMG.GameStates;
using ViMG.Rendering;

namespace ViMG.Items
{
    public class ItemCube : Item
	{
		public readonly ushort CubeId;
        private readonly Cube cube;

		public ItemCube(Cube cube, ushort cubeId) : base("item_" + cube.Identifier)
		{
			this.CubeId = cubeId;
            this.cube = cube;

			name = GlobalState.Registry.CubeRegistry.Get(cubeId).Name;
			description = GlobalState.Registry.CubeRegistry.Get(cubeId).Description;
		}

        public override ClientItem ClientInit()
        {
            return new ClientItemCube(this, cube.Client.GetHeldSourceRect());
        }

		public override bool RightClick(Player player, Inventory inventory, int index, Vector3 facing, out ActionStats actionStats)
		{
			base.RightClick(player, inventory, index, facing, out actionStats);

			if (player.IsLooking && player.CanPlace)
			{
				if (player.world.PlaceCube(player, player.PlaceAtPos, CubeId))
				{
					Console.WriteLine("placed at {0} - chunk pos {1}", player.PlaceAtPos, ChunkPosition.CubeChunk(player.PlaceAtPos));
                    inventory.Remove(index, 1);

                    actionStats.useTime = 0.25f;
                    actionStats.useAnimTime = 0.25f;

                    return true;
                }
			}

			return false;
		}
	}

    public class ClientItemCube : ClientItem
    {
        public ClientItemCube(Item item, RectangleF sourceRect) : base(item, sourceRect)
        {
        }

        public override RendererDeferred.DrawMaterial GetMaterial()
        {
            return StaticMaterials.Cubes;
        }

        public override void DrawInWorld(GraphicsDevice device, RendererDeferred renderer, ItemInstance item, Matrix transform)
        {
            //base.Draw(device, transform);

            Cube cube = GlobalState.Registry.CubeRegistry.Get((this.item as ItemCube).CubeId) ?? GlobalState.Registry.CubeRegistry.Air;

            var mesh = cube.Client.GetHeldMesh(device);

            Matrix scaled = Matrix.CreateScale(0.35f) * transform;
            if (mesh.IBO != null)
                renderer.AddOpaqueDraw(new Rendering.RendererDeferred.GBufferDraw(GetMaterial(),
                    mesh, scaled, cube.Client.GetHeldSourceRect()));
        }
    }
}
