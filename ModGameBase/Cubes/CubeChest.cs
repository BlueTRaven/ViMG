using BrUtility;
using Engine.ChunkStuff;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG.ChunkStuff;
using ViMG.Entities;
using ViMG.Items;
using static ViMG.Cubes.Cube;

namespace ViMG.Cubes
{
	public class CubeChest : Cube
	{
		private string tier;
		private int rows, columns;
		private int slots;

		public CubeChest(string tier, int rows, int columns) : base("chest_" + tier, 8)
		{
			this.tier = tier;
			this.rows = rows;
			this.columns = columns;
			this.slots = rows * columns;

			this.Client = new ClientCubeChest(this);
		}

		public override void OnPlayerPlaced(Player player, CubePosition position)
		{
			base.OnPlayerPlaced(player, position);

			MeshHelper.CubeFace face = CubeHelper.GetFaceFromPlayerPos(player, position);

			player.GetWorld().EntityManager.Add(new EntityChest(position, rows, columns, face));
		}

        
		public override void GetDrops(List<ItemInstance> itemsToDrop)
		{
			base.GetDrops(itemsToDrop);

			DropSelf(itemsToDrop);
		}
	}

	public class ClientCubeChest : ClientCube
	{
        public ClientCubeChest(Cube cube) : base(cube, new CubeFacingLayout(new RectangleF(144, 16, 16, 16), new RectangleF(160, 16, 16, 16), new RectangleF(160, 16, 16, 16)), Color.White)
        {
        }

        public override RectangleF GetSourceRect(RenderPass pass, CopiedChunkManager.CopiedChunkData data, ChunkRenderMesher.CubeMeshingParameters parameters, MeshHelper.CubeFace face)
        {
            // TODO GetEntityMeshingData
            //var meshingData = data.GetEntityMeshingData<EntityChest.MeshingData>(parameters.position);

            //if (face == meshingData.facing)
            //return new RectangleF(128, 16, 16, 16);

            return base.GetSourceRect(pass, data, parameters, face);
        }

    }
}
