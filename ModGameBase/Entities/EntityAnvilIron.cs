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

		private Inventory inventory;

		public EntityAnvilIron()
        {
            inventory = new Inventory(0, 8);
        }

		public EntityAnvilIron(CubePosition position)
		{
			this.TrackedPosition = position;
			this.Position = position.InWorldSpace();

			inventory = new Inventory(0, 8);
		}

		public void TrackingCubeUpdated(World world, ChunkManager manager, Player? player, ushort updatedId)
		{
			world.EntityManager.Kill(this);
		}

		public bool OnInteract(Player player)
		{
			if (player.IsLocalPlayer)
				Main.gameStateManager.GetCurrentGameState().PushMenu(new MenuAnvil(Main.gameStateManager, player, this, player.GetInventory(), player.GetHeldInventory(), inventory));

			return true;
		}

		public override void OnSave(List<byte> saveBytes)
		{
			base.OnSave(saveBytes);

			SaveHelper.SaveCubePosition(saveBytes, TrackedPosition);
			inventory.Save(saveBytes);
		}

		public override void OnLoad(byte[] loadBytes, in int version)
		{
			base.OnLoad(loadBytes, version);

			int index = 0;

			TrackedPosition = SaveHelper.LoadCubePosition(loadBytes, ref index);
			Position = TrackedPosition.InWorldSpace();

			inventory.Load(loadBytes, ref index);

			if (version == 0)
				inventory = new Inventory(0, 8);
		}
	}
}
