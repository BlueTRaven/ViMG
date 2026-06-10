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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="world"></param>
        /// <param name="cm"></param>
        /// <param name="playerWhoInitiated">The player that initiated this update. May be null.</param>
        /// <param name="updatedPosition"></param>
        /// <param name="updatedId"></param>
        /// <param name="updatedTime"></param>
        public void TrackingCubeUpdated(World world, ChunkManager cm, Player? playerWhoInitiated, CubePosition updatedPosition, ushort updatedId, double updatedTime);
    }
}
