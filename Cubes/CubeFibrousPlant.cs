using BrUtility;
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
    public class CubeFibrousPlant : Cube
    {
        public CubeFibrousPlant() : base("fibrous_plant", new RectangleF(48, 80, 16, 16), Color.White, 1)
        {
            Name = "Fibrous Plant";
            Description = "A strong, hearty, and fibrous plant. Doesn't look very edible, though.";

            Transparency = TransparencyValue.Transparent;
			Collision = CollisionValue.None;
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
        
        public override void MakeCubeVerts(RenderPass pass, CopiedChunkData data, ChunkMesher.CubeMeshingParameters parameters, List<VertexCube> vertices, List<int> indices)
        {
            parameters.positionWS += new Vector3(CUBE_SCALE / 2f, 0, CUBE_SCALE / 2f);
            DrawHelper3D.MakeXMeshVerts(pass, data, parameters, Vector3.One, vertices, indices);
        }
    }
}
