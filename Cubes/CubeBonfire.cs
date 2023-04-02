using BrUtility;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.ChunkStuff;
using ViMG.Entities;
using ViMG.VertexDeclarations;

namespace ViMG.Cubes
{
    public class CubeBonfire : Cube
    {
        public CubeBonfire() : base("bonfire", new RectangleF(240, 32, 32, 32), Color.White, 4, 0)
        {
            Transparency = TransparencyValue.Transparent | TransparencyValue.InvisibleOnDepth;

            Name = "Bonfire";
            Description = "You shouldn't really have this in your inventory...";
        }

        public override void OnPlayerPlaced(Player player, CubePosition position)
        {
            base.OnPlayerPlaced(player, position);

            player.GetWorld().EntityManager.Add(new EntityCubeBonfire(position));
        }

        public override CubeAnimation GetAnimation(RenderPass pass, CopiedChunkData data, ChunkMesher.CubeMeshingParameters parameters, MeshHelper.CubeFace face)
        {
            return new CubeAnimation(0.125f, 4, 32);
        }

        public override bool ShouldMeshPass(RenderPass pass)
        {
            return pass == RenderPass.Opaque;
        }

        public override void MakeCubeVerts(RenderPass pass, CopiedChunkData data, ChunkMesher.CubeMeshingParameters parameters, List<VertexCube> vertices, List<int> indices)
        {
            parameters.positionWS += new Vector3(CUBE_SCALE / 2f, 0, CUBE_SCALE / 2f);
            DrawHelper3D.MakeXMeshVerts(pass, data, parameters, new Vector3(2), vertices, indices);
        }
    }
}
