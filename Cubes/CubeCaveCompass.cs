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
    public class CubeCaveCompass : Cube
    {
        public CubeCaveCompass() : base("cave_compass_placed", new RectangleF(new Vector2(16f / 1024f, 32f / 1024f), new Vector2(16f / 1024f)), Color.White * 0.5f, 1)
        {
            Transparency = TransparencyValue.Transparent;
        }

        public override void OnPlayerPlaced(Player player, CubePosition position)
        {
            base.OnPlayerPlaced(player, position);

            player.GetWorld().EntityManager.Add(new EntityCaveCompass(position));
        }

        public override void MakeCubeVerts(RenderPass pass, World world, ChunkMesher.CubeMeshingParameters parameters, List<VertexCube> vertices, List<int> indices)
        {
            if (pass == RenderPass.Transparent)
                DrawHelper3D.MakeUVSphereRaw(vertices, indices, parameters.positionWS + new Vector3(CUBE_SCALE / 2f), GetSourceRect(pass, world, parameters), CUBE_SCALE / 2f);
            //base.MakeVerts(pass, world, pos, min, max, visual, cube, vertices, indices);
        }
    }
}
