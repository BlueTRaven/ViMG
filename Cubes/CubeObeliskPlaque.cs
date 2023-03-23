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

            return new RectangleF(48, 192, 16, 16);
        }
    }
}
