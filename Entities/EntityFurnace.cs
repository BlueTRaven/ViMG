using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Cubes;
using ViMG.UIs;

namespace ViMG.Entities
{
	[Serializable]
	[EntityMeta(1, 0)]
	public class EntityFurnace : Entity, ICubeTracker
	{
		public CubePosition TrackedPosition 
		{
			get;
			private set; 
		}

		private Inventory inventory;

		public EntityFurnace()
		{

		}

		public EntityFurnace(CubePosition position)
		{
			this.TrackedPosition = position;

			inventory = new Inventory(5);
		}

		public override void Initialize(World world)
		{
			base.Initialize(world);

			Optional<ICubeTracker> tracker = world.EntityManager.GetEntityTrackingPosition(TrackedPosition);

			if (!tracker.HasValue() || tracker.Get() != this)
				world.EntityManager.Remove(this);
		}

		public void TrackingCubeDestroyed(World world, ChunkManager cm)
		{
			world.EntityManager.Remove(this);
		}

		public bool OnInteract(Player player)
		{
			player.OpenUI(new UIInventoryFurnace(player, player.GetInventory(), inventory));

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

			inventory = Inventory.Load(loadBytes, ref index);
		}
	}
}
