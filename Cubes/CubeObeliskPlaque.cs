using BrUtility;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.ChunkStuff;

namespace ViMG.Cubes
{
    public class CubeObeliskPlaque : Cube
    {
        public CubeObeliskPlaque() : base("obelisk_plaque", new RectangleF(64, 176, 16, 16), Color.White, 0, 4)
        {
        }

        public override bool CanRightClick(World world, CubePosition position)
        {
            return true;
        }

        public override void OnRightClick(World world, CubePosition position)
        {
            base.OnRightClick(world, position);

            world.GameStateManager.TheIsland.PushMenu(world.MenuDialogue);

            world.MenuDialogue.StartDialogue("Here lies our sins\r\n" +
                "Wicked were we, and so our guilt lies\r\n" +
                "Buried deep below in vast vaults\r\n" +
                "Do not delve deeper; heed our warning\r\n" +
                "Do not speak His Name\r\n" +
                "For underneath is buried death");
        }

        public override RectangleF GetSourceRect(RenderPass pass, CopiedChunkData data, ChunkMesher.CubeMeshingParameters parameters, MeshHelper.CubeFace face)
        {
            CubePosition opposite;
            switch (face)
            {
                case MeshHelper.CubeFace.RIGHT:
                    opposite = new CubePosition(1, 0, 0);
                    break;
                case MeshHelper.CubeFace.LEFT:
                    opposite = new CubePosition(-1, 0, 0);
                    break;
                case MeshHelper.CubeFace.FRONT:
                    opposite = new CubePosition(0, 0, 1);
                    break;
                case MeshHelper.CubeFace.BACK:
                    opposite = new CubePosition(0, 0, -1);
                    break;
                default:
                    opposite = new CubePosition();
                    break;
            }

            if (data != null && data.GetId(parameters.position + opposite) == Main.Registry.CubeRegistry.Get("obelisk").Id)
                return new RectangleF(64, 176, 16, 16);

            return new RectangleF(160, 208, 16, 16);
        }
    }
}
