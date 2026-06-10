using BrUtility;
using Engine.ChunkStuff;
using Engine.Clients;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.ChunkStuff;
using ViMG.Entities;
using ViMG.Rendering;
using ViMG.VertexDeclarations;
using static ViMG.Cubes.Cube;

namespace ViMG.Cubes
{
    public class CubeBonfire : Cube
    {
        public CubeBonfire() : base("bonfire", 4, 0)
        {
            Transparency = TransparencyValue.Transparent | TransparencyValue.InvisibleOnDepth;

            Name = "Bonfire";
            Description = "You shouldn't really have this in your inventory...";
        }

        public override ClientCube ClientInit()
        {
            return new ClientCubeBonfire(this);
        }

        public override void OnPlayerPlaced(Player player, CubePosition position)
        {
            base.OnPlayerPlaced(player, position);

            player.GetWorld().EntityManager.Add(new EntityCubeBonfire(position));
        }

        public override bool ShouldMeshPass(RenderPass pass)
        {
            return pass == RenderPass.Opaque;
        }

        public override void MakeCubeVerts(RenderPass pass, CopiedChunkManager.CopiedChunkData data, ChunkRenderMesher.CubeMeshingParameters parameters, FastList<VertexCube> vertices, List<int> indices, int vertexOffset = 0)
        {
            parameters.positionWS += new Vector3(CUBE_SCALE / 2f, 0, CUBE_SCALE / 2f);
            DrawHelper3D.MakeXMeshVerts(pass, data, parameters, new Vector3(2), vertices, indices, vertexOffset);
        }
    }

    public class ClientCubeBonfire : ClientCube
    {
        private static VerySimpleMesh heldMesh;

        public ClientCubeBonfire(Cube cube) : base(cube, new RectangleF(240, 32, 32, 32), Color.White)
        {
        }

        public override CubeAnimation GetAnimation(RenderPass pass, CopiedChunkManager.CopiedChunkData data, ChunkRenderMesher.CubeMeshingParameters parameters, MeshHelper.CubeFace face)
        {
            return new CubeAnimation(0.125f, 4, 32);
        }

        public override VerySimpleMesh GetHeldMesh(GraphicsDevice device)
        {
            if (heldMesh.IBO == null)
            {
                FastList<VertexCube> vertices = new FastList<VertexCube>();
                List<int> indices = new List<int>();

                Vector3 a = Vector3.Zero;
                Vector3 b = new Vector3(0, CUBE_SCALE / 2f, 0);
                Vector3 c = new Vector3(CUBE_SCALE / 2f, CUBE_SCALE / 2f, 0);
                Vector3 d = new Vector3(CUBE_SCALE / 2f, 0, 0);

                MeshHelper.MakeQuadVertsVertexPositionColorTextureNormal(b, c, d, a,
                    new Vector3(0, 0, 1), Color.White, vertices, indices);

                heldMesh = VerySimpleMesh.Opaque(device, new ChunkRenderMesher.VertexAttributes(vertices, indices));
                //heldMesh = MeshHelper.MakeSimplerMesh(device, vertices.ToVertexOpaquePass(), indices);
                //heldMesh = new SimpleMesh<VertexCube, int>(device, vertices, indices, GlobalState.assetsManager.GetAsset<Texture2D>("cubes_textures"));
            }

            return heldMesh;
        }

        public override RectangleF GetHeldSourceRect(ClientStates client)
        {
            const float FRAME_TIME = 0.125f * 3;

            var prev = client.Previous(1);
            var curr = client.Current();
            float alive = (float)double.Lerp(prev.time, curr.time, client.TimeC);

            float t = ((alive % FRAME_TIME) * 3f) / (FRAME_TIME * 3f);

            return new RectangleF((int)(t * 3) * 16f + 192, 16f, 16f, 16f);
        }
    }
}
