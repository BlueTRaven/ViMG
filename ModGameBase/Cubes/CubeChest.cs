using BepuPhysics.Constraints;
using BrUtility;
using Engine.ChunkStuff;
using Engine.Clients;
using Engine.Items;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG.ChunkStuff;
using ViMG.Entities;
using ViMG.Items;
using ViMG.UIs;
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

        public override bool CanRightClick(CubePosition position)
        {
			return true;
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
			var entity = data.GetEntity(parameters.position);

			if (face == (MeshHelper.CubeFace)entity.state) 
				return new RectangleF(128, 16, 16, 16);
            // TODO GetEntityMeshingData
            //var meshingData = data.GetEntityMeshingData<EntityChest.MeshingData>(parameters.position);

            //if (face == meshingData.facing)
            //return new RectangleF(128, 16, 16, 16);

            return base.GetSourceRect(pass, data, parameters, face);
        }

        public override void OnRightClick(ClientStates client, int playerId, CubePosition position)
        {
            base.OnRightClick(client, playerId, position);

			var tracker = client.ChunkManager.CubeTrackers.Get(ChunkPosition.CubeChunk(position)).Get(position.InChunkSpace());
			if (playerId == client.LocalPlayerIndex)
			{
				var ent = client.Current().entities.GetByRef(ref tracker);
				var invRef = new InventoryManager.InventoryReference((ushort)ent.counters[2], (short)ent.counters[3]);
				var playerRef = client.Current().entities.GetPlayerRef(playerId);
				var player = client.Current().entities.GetByRef(playerRef);
				var playerExtra = player.GetExtra<Player.PlayerExtraState>();
                Main.gameStateManager.GetCurrentGameState().PushMenu(new MenuChest(Main.gameStateManager, playerRef, tracker, playerExtra.inventory, playerExtra.heldInventory, invRef, ent.counters[0], ent.counters[1]));
            }
        }
    }
}
