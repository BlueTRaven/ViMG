using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViMG.Items
{
    public interface IHasPickaxeStats
    {
        ref readonly ItemPickaxeHead.PickaxeStats GetStats(ItemInstance item);

        CubePosition[] GetAffectedPositions(ItemInstance item, Vector3 standingPosition, Vector3 hit, Vector3 normal);
    }
}
