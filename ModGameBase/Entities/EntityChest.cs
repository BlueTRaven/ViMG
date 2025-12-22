using BepuUtilities.Memory;
using Engine.Items;
using Engine.Networking;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG.UIs;

namespace ViMG.Entities
{
    [EntitySerializable(EntitySerializableAttribute.SerializationType.All)]
	[EntityMeta(3, 1)]
	public class EntityChest : Entity, ICubeTracker, IHasInventory, ISyncBasicState
	{
		public struct MeshingData
		{
            public MeshHelper.CubeFace facing;
        }
		public CubePosition TrackedPosition { get; private set; }

		private InventoryManager.InventoryReference inventory;
		private int rows, columns;
		private MeshingData meshingData;
		public MeshHelper.CubeFace Facing => meshingData.facing;

        public EntityChest()
        {
			//DoesSync = false;
			MajorSyncInterval = 5;
        }

		public EntityChest(CubePosition position, int rows, int columns, MeshHelper.CubeFace facing)
		{
            //DoesSync = false;
            MajorSyncInterval = 5;

            this.TrackedPosition = position;
			this.Position = position.InWorldSpace();
			this.rows = rows;
			this.columns = columns;
			meshingData = new MeshingData()
			{
				facing = facing
			};
		}

		//A separate constructor so world gen can provide prefilled inventory.
		public EntityChest(CubePosition position, InventoryManager.InventoryReference inventory, int rows, int columns, MeshHelper.CubeFace facing)
        {
			this.TrackedPosition = position;
			this.Position = position.InWorldSpace();
			this.rows = rows;
			this.columns = columns;
            meshingData = new MeshingData()
            {
                facing = facing
            };
			this.inventory = inventory;
		}

		public override void Initialize(World world)
		{
			base.Initialize(world);

			world.InventoryManager.GetOrAdd(ref inventory, new Inventory.InventoryConfig(rows * columns));
			Optional<Entity> tracker = world.EntityManager.GetEntityTrackingPosition(TrackedPosition);

			if (tracker.HasValue())
				world.EntityManager.Kill(this);

			world.ChunkManager.ChunkMesher?.MarkChunkDirty(ChunkPosition.CubeChunk(TrackedPosition));//, true);
		}

        public override void OnUnload()
        {
            base.OnUnload();

			world.InventoryManager.Unload(inventory);
        }

        public override void Update(double deltaTime)
        {
            base.Update(deltaTime);

			var inventory = world.InventoryManager.Get(this.inventory);
            inventory.ProcessActions(this);
        }	

		public void TrackingCubeUpdated(World world, ChunkManager manager, Player? player, ushort updatedId)
		{
			world.EntityManager.Kill(this);
		}

		public bool OnInteract(Player player)
		{
			if (player.IsLocalPlayer)
				Main.gameStateManager.GetCurrentGameState().PushMenu(new MenuChest(Main.gameStateManager, player, this, player.inventory, player.heldInventory, inventory, rows, columns));

			return true;
		}

		public override void OnSave(List<byte> saveBytes)
		{
			base.OnSave(saveBytes);

			SaveHelper.SaveCubePosition(saveBytes, TrackedPosition);
			SaveHelper.SaveInt32(saveBytes, rows);
			SaveHelper.SaveInt32(saveBytes, columns);

			SaveHelper.SaveInt32(saveBytes, (int)meshingData.facing);

            var inventory = world.InventoryManager.Get(this.inventory);
            inventory.Save(saveBytes);
		}

        public override void OnLoad(World world, byte[] loadBytes, in int version)
        {
            base.OnLoad(world, loadBytes, version);

            int index = 0;

			TrackedPosition = SaveHelper.LoadCubePosition(loadBytes, ref index);
			Position = TrackedPosition.InWorldSpace();

			rows = SaveHelper.LoadInt32(loadBytes, ref index);
			columns = SaveHelper.LoadInt32(loadBytes, ref index);

			if (version >= 2)
                meshingData.facing = (MeshHelper.CubeFace)SaveHelper.LoadInt32(loadBytes, ref index);

            inventory = world.InventoryManager.Add(new Inventory.InventoryConfig(rows * columns));
			world.InventoryManager.Get(inventory).Load(loadBytes, ref index);
		}

        public unsafe Buffer<byte> GetMeshingData(BufferPool bufferPool)
        {
            bufferPool.Take(1, out Buffer<MeshingData> md);
            md.Memory->facing = meshingData.facing;

            return md.As<byte>();
        }

		public bool InventoryAction(Player? activatingPlayer, int action)
        {
			return false;
        }

        public void Get(out BasicState state)
        {
			state = new BasicState
			{
				position = Position,
				counters = { [0] = (int)meshingData.facing },
			};
        }

        public void Set(ref readonly BasicState state)
        {
			Position = state.position;
			meshingData.facing = (MeshHelper.CubeFace)state.counters[0];
        }
    }
}
