using Engine.ChunkStuff;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViMG.Items
{
    public interface IHasAreaEffect
    {
        ref readonly ItemPickaxeHead.PickaxeStats GetStats(ItemInstance item);

        CubePosition[] GetAffectedPositions(ICubeGetter cubeView, ItemInstance item, Vector3 standingPosition, Vector3 hit, Vector3 normal, out int num);

        bool CanPredictAir()
        {
            return false;
        }
    }
}
