using BrUtility;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Items;

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

        public override void MakeVerts(RenderPass pass, World world, Vector3 pos, Vector3 min, Vector3 max, MeshHelper.CubeFace faces, List<VertexCube> vertices, List<int> indices)
        {
			if (pass != RenderPass.Opaque)
				return;

			DrawHelper3D.MakeXMeshVerts(pass, this, world, pos + new Vector3(CUBE_SCALE / 2f, 0, CUBE_SCALE / 2f), Vector3.One, vertices, indices);
        }
    }
}
