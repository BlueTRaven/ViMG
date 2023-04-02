using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.ChunkStuff;
using ViMG.Cubes;
using ViMG.Items;
using ViMG.VertexDeclarations;

namespace ViMG.Cubes
{
    public class CubeCrystal : Cube
    {
        private static SimpleMesh<VertexCube, int> heldMesh;

        public CubeCrystal() : base("crystal_quartz", new RectangleF(48, 64, 16, 16), Color.White, 1)
        {
            Transparency = TransparencyValue.Transparent;
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

                MeshHelper.MakeQuadVertsVertexPositionColorTextureNormal(c, b, a, d,
                    new Vector3(0, 0, -1), Color.White, vertices, indices);

                heldMesh = new SimpleMesh<VertexCube, int>(device, vertices, indices, Main.assetsManager.GetAsset<Texture2D>("cubes_textures"));
            }

            return heldMesh;
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

        public override RectangleF GetHeldSourceRect(World world)
        {
            return GetSourceRect(RenderPass.Transparent, default, new ChunkMesher.CubeMeshingParameters() { id = Id, cube = this, faces = MeshHelper.CubeFace.ALL });
        }

        public override void GetDrops(List<ItemInstance> itemsToDrop)
        {
            base.GetDrops(itemsToDrop);

            DropSelf(itemsToDrop);
        }
    }
}
