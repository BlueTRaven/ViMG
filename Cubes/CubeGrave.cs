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
    public class CubeGrave : Cube
    {
        public CubeGrave() : base("grave", new CubeFacingLayout(new RectangleF(224, 64, 16, 16), new RectangleF(240, 64, 16, 16)), Color.White, 8)
        {
            Name = "Grave";
            Description = "Not obtainable";
        }

        public override RectangleF GetSourceRect(RenderPass pass, CopiedChunkData data, ChunkMesher.CubeMeshingParameters parameters, MeshHelper.CubeFace face)
        {
            if (data != null && (face & MeshHelper.CubeFace.SIDES) > 0 && data.GetId(parameters.position + new CubePosition(0, 1, 0)) == Id)
                return new RectangleF(224, 80, 16, 16);
            else return base.GetSourceRect(pass, data, parameters, face);
        }
    }
}
