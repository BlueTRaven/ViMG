using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Entities;
using BrUtility;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using ViMG.ChunkStuff;

namespace ViMG.Cubes
{
    public class CubeFlame : Cube
    {
        private static SimpleMesh<VertexCube, int> heldMesh;

        public CubeFlame() : base("flame", new RectangleF(192, 0, 16, 16), Color.White, 1)
        {
            Transparency = TransparencyValue.Transparent | TransparencyValue.InvisibleOnDepth;
            Collision = CollisionValue.None;
        }

        public override SimpleMesh<VertexCube, int> GetHeldMesh(GraphicsDevice device)
        {
            if (heldMesh == null)
            {
                List<VertexCube> vertices = new List<VertexCube>();
                List<int> indices = new List<int>();

                Vector3 a = Vector3.Zero;
                Vector3 b = new Vector3(0, CUBE_SCALE / 2f, 0);
                Vector3 c = new Vector3(CUBE_SCALE / 2f, CUBE_SCALE / 2f, 0);
                Vector3 d = new Vector3(CUBE_SCALE / 2f, 0, 0);

                MeshHelper.MakeQuadVertsVertexPositionColorTextureNormal(b, c, d, a,
                    new Vector3(0, 0, 1), Color.White, vertices, indices);

                heldMesh = new SimpleMesh<VertexCube, int>(device, vertices, indices, Main.assetsManager.GetAsset<Texture2D>("cubes_textures"));
            }

            return heldMesh;
        }

        public override RectangleF GetHeldSourceRect(World world)
        {
            const float FRAME_TIME = 0.125f * 3;

            float alive = world.GetTime();

            float t = ((alive % FRAME_TIME) * 3f) / (FRAME_TIME * 3f);

            return new RectangleF((int)(t * 3) * 16f + 192, 0, 16f, 16f);
        }

        public override void OnPlayerPlaced(Player player, CubePosition position)
        {
            base.OnPlayerPlaced(player, position);

            player.GetWorld().EntityManager.Add(new EntityCubeFlame(position, player.GetWorld().GetTime() + Main.random.NextFloat(3f * 60f, 15f * 60f)));
        }

        public override void OnLoaded(World world, CubePosition position)
        {
            base.OnLoaded(world, position);
        }

        public override bool ShouldMeshPass(RenderPass pass)
        {
            return pass == RenderPass.Opaque;
        }

        public override void MakeCubeVerts(RenderPass pass, CopiedChunkData data, ChunkMesher.CubeMeshingParameters parameters, List<VertexCube> vertices, List<int> indices)
        {
            parameters.positionWS += new Vector3(CUBE_SCALE / 2f, 0, CUBE_SCALE / 2f);
            DrawHelper3D.MakeXMeshVerts(pass, data, parameters, Vector3.One, vertices, indices);
        }

        public override CubeAnimation GetAnimation(RenderPass pass, CopiedChunkData data, ChunkMesher.CubeMeshingParameters parameters, MeshHelper.CubeFace face)
        {
            return new CubeAnimation(0.125f, 3, 16);
        }
    }
}
