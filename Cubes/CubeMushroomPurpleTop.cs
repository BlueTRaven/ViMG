using BrUtility;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.ChunkStuff;
using ViMG.Items;

namespace ViMG.Cubes
{
    public class CubeMushroomPurpleTop : Cube
    {
        public CubeMushroomPurpleTop() : base("mushroom_purple_top", new CubeFacingLayout(new RectangleF(80, 112, 16, 16), new RectangleF(48, 112, 16, 16), new RectangleF(64, 96, 16, 16)), Color.White, 4)
        {
            Name = "Purple Mushroom Top";
        }

        public override void GetDrops(List<ItemInstance> itemsToDrop)
        {
            base.GetDrops(itemsToDrop);

            DropSelf(itemsToDrop);
        }

        public override RectangleF GetSourceRect(RenderPass pass, CopiedChunkData data, ChunkMesher.CubeMeshingParameters parameters, MeshHelper.CubeFace face)
        {
            if (!data.valid)
                return base.GetSourceRect(pass, data, parameters, face);

            //we're meshing one of the sides.
            if ((face & MeshHelper.CubeFace.SIDES) > 0)
            {
                //if the cube above is a mushroom block
                if (data.GetCube(parameters.position - new CubePosition(0, 1, 0)).GetOrDefault(Main.Registry.CubeRegistry.Air) == this)
                {
                    //use the same top texture instead of the ordinary side texture.
                    return new RectangleF(48, 96, 16, 16);
                }
            }

            return base.GetSourceRect(pass, data, parameters, face);
        }
    }
}
