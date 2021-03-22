using System;
using System.Collections.Generic;
using System.Text;
using ViMG.UIs;

namespace ViMG.Entities
{
	public class EntityFurnace : Entity, ICubeTracker
	{
		public CubePosition TrackedPosition { get; private set; }

		private Inventory inventory;

		public EntityFurnace(CubePosition position)
		{
			this.TrackedPosition = position;

			inventory = new Inventory(5);
		}

		public void TrackingCubeDestroyed()
		{
			world.EntityManager.Remove(this);
		}

		public bool OnInteract(Player player)
		{
			player.OpenUI(new UIInventoryFurnace(player, player.GetInventory(), inventory));

			return true;
		}
	}
}
