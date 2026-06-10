using BrUtility;
using Engine.ChunkStuff;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.ChunkStuff;
using ViMG.Items;
using ViMG.VertexDeclarations;

namespace ViMG.Cubes
{
    public class CubeMushroomOrangeSmall : Cube
    {
        public CubeMushroomOrangeSmall() : base("mushroom_orange_small", 1)
        {
            Transparency = TransparencyValue.Transparent | TransparencyValue.InvisibleOnDepth;
            Collision = CollisionValue.None;
        }

        public override ClientCube ClientInit()
        {
            return new(this, new RectangleF(96, 96, 16, 16), Color.White);
        }

        public override void GetDrops(List<ItemInstance> itemsToDrop)
        {
            base.GetDrops(itemsToDrop);

            DropSelf(itemsToDrop);
        }

        public override bool ShouldMeshPass(RenderPass pass)
        {
            return pass == RenderPass.Opaque;
        }

        public override void MakeCubeVerts(RenderPass pass, CopiedChunkManager.CopiedChunkData data, ChunkRenderMesher.CubeMeshingParameters parameters, FastList<VertexCube> vertices, List<int> indices, int vertexOffset = 0)
        {
            parameters.positionWS += new Vector3(CUBE_SCALE / 2f, 0, CUBE_SCALE / 2f);
            DrawHelper3D.MakeXMeshVerts(pass, data, parameters, Vector3.One, vertices, indices, vertexOffset);
        }
    }
}
