using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Cubes;
using ViMG.Items;

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

        public override void MakeVerts(RenderPass pass, World world, Vector3 pos, Vector3 min, Vector3 max, CubeVisualInstance visual, Cube cube, List<VertexCube> vertices, List<int> indices)
        {
            if (pass == RenderPass.Opaque)
                return;

            DrawHelper3D.MakeXMeshVerts(pass, cube, world, pos, vertices, indices);
        }

        public override RectangleF GetHeldSourceRect(World world)
        {
            return GetSourceRect(RenderPass.Transparent, world, new CubePosition());
        }

        public override void GetDrops(List<ItemInstance> itemsToDrop)
        {
            base.GetDrops(itemsToDrop);

            DropSelf(itemsToDrop);
        }
    }
}
