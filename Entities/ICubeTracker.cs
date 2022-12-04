using System;
using System.Collections.Generic;
using System.Text;

namespace ViMG.Entities
{
	public interface ICubeTracker
	{
		public CubePosition TrackedPosition { get; }

		public bool OnInteract(Player player);

		public void TrackingCubeUpdated(World world, ChunkManager2 cm, ushort updatedId);
	}
}
