using BepuUtilities.Memory;
using System;
using System.Collections.Generic;
using System.Text;

namespace ViMG.Entities
{
	public interface ICubeTracker
	{
		public CubePosition TrackedPosition { get; }

		public bool OnInteract(Player player);

		public void TrackingCubeUpdated(World world, ChunkManager cm, Player? playerWhoInitiated, ushort updatedId);

		public Buffer<byte> GetMeshingData(BufferPool bufferPool) { return default; }
	}
}
