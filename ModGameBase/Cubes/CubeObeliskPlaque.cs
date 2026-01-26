using BrUtility;
using Engine;
using Engine.ChunkStuff;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.ChunkStuff;
using static ViMG.Cubes.Cube;

namespace ViMG.Cubes
{
    public class CubeObeliskPlaque : Cube
    {
        public CubeObeliskPlaque() : base("obelisk_plaque", 0, 4)
        {
            Client = new ClientCubeObeliskPlaque(this);
        }

        public override bool CanRightClick(CubePosition position)
        {
            return true;
        }

        public override void OnRightClick(World world, CubePosition position)
        {
            base.OnRightClick(world, position);

            GlobalState.GameStateManager.TheIsland.PushMenu(world.MenuDialogue);

            world.MenuDialogue.StartText("Here lies our grave sins\r\n" +
                "Wicked were we, and here our guilt lies\r\n" +
                "Buried deep below in vast vaults\r\n" +
                "Do not delve deeper; heed our warning\r\n" +
                "Do not speak His name\r\n" +
                "For underneath is buried our great death");
        }
    }

    public class ClientCubeObeliskPlaque : ClientCube
    {
        public ClientCubeObeliskPlaque(Cube cube) : base(cube, new RectangleF(64, 176, 16, 16), Color.White)
        {
        }

        public override RectangleF GetSourceRect(RenderPass pass, CopiedChunkManager.CopiedChunkData data, ChunkRenderMesher.CubeMeshingParameters parameters, MeshHelper.CubeFace face)
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

            if (data.GetId(parameters.position + opposite) == GlobalState.Registry.CubeRegistry.Get("obelisk").Id)
                return new RectangleF(64, 176, 16, 16);

            return new RectangleF(160, 208, 16, 16);
        }
    }
}
