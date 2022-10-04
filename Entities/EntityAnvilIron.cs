using System;
using System.Collections.Generic;
using System.Text;
using ViMG.UIs;

namespace ViMG.Entities
{
	[Serializable]
	[EntityMeta(0, 0)]
	public class EntityAnvilIron : Entity, ICubeTracker
	{
		public CubePosition TrackedPosition { get; private set; }

		private Inventory inventoryTools;
		private Inventory inventoryArmor;

		public EntityAnvilIron()
        {

        }

		public EntityAnvilIron(CubePosition position)
		{
			this.TrackedPosition = position;
			this.Position = position.InWorldSpace(null);

			inventoryTools = new Inventory(4);
			inventoryArmor = new Inventory(8);
		}

		public void TrackingCubeDestroyed(World world, ChunkManager cm)
		{
			world.EntityManager.Remove(this);
		}

		public bool OnInteract(Player player)
		{
			player.world.GameStateManager.GetCurrentGameState().PushMenu(new MenuAnvil(player, player.GetInventory(), inventoryTools, inventoryArmor));

			return true;
		}

		public override void OnSave(List<byte> saveBytes)
		{
			base.OnSave(saveBytes);

			SaveHelper.SaveCubePosition(saveBytes, TrackedPosition);
			inventoryTools.Save(saveBytes);
		}

		public override void OnLoad(byte[] loadBytes, in int version)
		{
			base.OnLoad(loadBytes, version);

			int index = 0;

			TrackedPosition = SaveHelper.LoadCubePosition(loadBytes, ref index);
			Position = TrackedPosition.InWorldSpace(null);

			inventoryTools = Inventory.Load(loadBytes, ref index);
		}
	}
}
