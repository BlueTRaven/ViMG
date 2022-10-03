using System;
using System.Collections.Generic;
using System.Text;
using ViMG.UIs;

namespace ViMG.Entities
{
	[Serializable]
	[EntityMeta(1, 1)]
	public class EntityChest : Entity, ICubeTracker
	{
		public CubePosition TrackedPosition { get; private set; }

		private Inventory inventory;
		private int rows, columns;

		public EntityChest()
        {

        }

		public EntityChest(CubePosition position, int rows, int columns)
		{
			this.TrackedPosition = position;
			this.Position = position.InWorldSpace(null);
			this.rows = rows;
			this.columns = columns;

			inventory = new Inventory(rows * columns);
		}

		public override void Initialize(World world)
		{
			base.Initialize(world);

			Optional<ICubeTracker> tracker = world.EntityManager.GetEntityTrackingPosition(TrackedPosition);

			if (!tracker.HasValue() || tracker.Get() != this)
				world.EntityManager.Remove(this);

			world.ChunkManager.MarkDirty(ChunkPosition.CubeChunk(TrackedPosition), true);
		}

		public void TrackingCubeDestroyed(World world, ChunkManager cm)
		{
			world.EntityManager.Remove(this);
		}

		public bool OnInteract(Player player)
		{
			player.world.GameStateManager.GetCurrentGameState().PushMenu(new MenuChest(player, player.GetInventory(), inventory, rows, columns));

			return true;
		}

		public override void OnSave(List<byte> saveBytes)
		{
			base.OnSave(saveBytes);

			SaveHelper.SaveCubePosition(saveBytes, TrackedPosition);
			SaveHelper.SaveInt32(saveBytes, rows);
			SaveHelper.SaveInt32(saveBytes, columns);
			inventory.Save(saveBytes);
		}

		public override void OnLoad(byte[] loadBytes, in int version)
		{
			base.OnLoad(loadBytes, version);

			int index = 0;

			TrackedPosition = SaveHelper.LoadCubePosition(loadBytes, ref index);
			Position = TrackedPosition.InWorldSpace(null);

			rows = SaveHelper.LoadInt32(loadBytes, ref index);
			columns = SaveHelper.LoadInt32(loadBytes, ref index);

			inventory = Inventory.Load(loadBytes, ref index);
		}
	}
}
