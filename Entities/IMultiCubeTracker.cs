using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViMG.Entities
{
    public interface IMultiCubeTracker
    {
        public IEnumerable<CubePosition> TrackedPositions { get; }

        public bool OnInteract(Player player);

        public void TrackingCubeUpdated(World world, ChunkManager cm, ushort updatedId);
    }
}
