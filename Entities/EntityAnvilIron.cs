using System;
using System.Collections.Generic;
using System.Text;
using ViMG.UIs;

namespace ViMG.Entities
{
	public class EntityAnvilIron : Entity, ICubeTracker
	{
		public CubePosition TrackedPosition { get; private set; }

		private Inventory inventory;

		public EntityAnvilIron(CubePosition position)
		{
			this.TrackedPosition = position;

			inventory = new Inventory(4);
		}

		public void TrackingCubeDestroyed()
		{
			world.EntityManager.Remove(this);
		}

		public bool OnInteract(Player player)
		{
			player.OpenUI(new UIInventoryAnvil(player, player.GetInventory(), inventory));

			return true;
		}
	}
}
