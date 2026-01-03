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
        public CubeDoor() : base("door", 4, 0)
        {
            Name = "Door";

            Transparency = TransparencyValue.Invisible;

            Client = new(this, new RectangleF(32, 32, 16, 16), Color.White);
        }

        public override bool CanPlace(World world, ChunkManager manager, CubePosition position)
        {
            var above = manager.CubeView.GetCube(position + new CubePosition(0, 1, 0));

            if (above.GetOrDefault(Main.Registry.CubeRegistry.Air) == Main.Registry.CubeRegistry.Air)
                return true;
            else return false;
        }

        public override void OnPlayerPlaced(Player player, CubePosition position)
        {
            base.OnPlayerPlaced(player, position);

            CubePosition top = position + new CubePosition(0, 1, 0);
            player.world.ChunkManager.CubeView.SetCube(top, Id);

            MeshHelper.CubeFace face = CubeHelper.GetFaceFromPlayerPos(player, position, false);

            player.GetWorld().EntityManager.Add(new Door((position, top), face));
        }
    }
}
