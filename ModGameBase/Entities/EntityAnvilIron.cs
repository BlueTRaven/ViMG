using Engine.Items;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG.UIs;

namespace ViMG.Entities
{
	[EntitySerializable(EntitySerializableAttribute.SerializationType.All)]
	[EntityMeta(1, 0)]
	public class EntityAnvilIron : Entity, ICubeTracker
	{
		public CubePosition TrackedPosition { get; private set; }

		private InventoryManager.InventoryReference inventory;

		public EntityAnvilIron()
        {
        }

		public EntityAnvilIron(CubePosition position)
		{
			this.TrackedPosition = position;
			this.Position = position.InWorldSpace();

		}

        public override void Initialize(World world)
        {
            base.Initialize(world);
			inventory = world.InventoryManager.Add(new Inventory.InventoryConfig(8));
        }

        public override void OnUnload()
        {
            base.OnUnload();

			world.InventoryManager.Unload(inventory);
        }

		public void TrackingCubeUpdated(World world, ChunkManager manager, Player? player, ushort updatedId)
		{
			world.EntityManager.Kill(this);
		}

		public bool OnInteract(Player player)
		{
			if (player.IsLocalPlayer)
			{
				Main.gameStateManager.GetCurrentGameState().PushMenu(new MenuAnvil(Main.gameStateManager, world.EntityManager.GetReference(player), world.EntityManager.GetReference(this), player.inventory, player.heldInventory, inventory));
			}

			return true;
		}

		public override void OnSave(List<byte> saveBytes)
		{
			base.OnSave(saveBytes);

			SaveHelper.SaveCubePosition(saveBytes, TrackedPosition);
			world.InventoryManager.Get(inventory)!.Save(saveBytes);
		}

        public override void OnLoad(World world, byte[] loadBytes, in int version)
        {
            base.OnLoad(world, loadBytes, version);

            int index = 0;

			TrackedPosition = SaveHelper.LoadCubePosition(loadBytes, ref index);
			Position = TrackedPosition.InWorldSpace();

			world.InventoryManager.Get(inventory).Load(loadBytes, ref index);
		}
	}
}
