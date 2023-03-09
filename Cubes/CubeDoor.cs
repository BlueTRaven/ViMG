using BrUtility;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Entities;

namespace ViMG.Cubes
{
    public class CubeDoor : Cube
    {
        public CubeDoor() : base("door", new RectangleF(32, 32, 16, 16), Color.White, 4, 0)
        {
            Name = "Door";

            Transparency = TransparencyValue.Invisible;
        }

        public override void OnPlayerPlaced(Player player, CubePosition position)
        {
            base.OnPlayerPlaced(player, position);

            MeshHelper.CubeFace face = CubeHelper.GetFaceFromPlayerPos(player, position, false);

            player.GetWorld().EntityManager.Add(new DoorWood(position, face));
        }
    }
}
