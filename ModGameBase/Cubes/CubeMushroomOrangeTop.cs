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
using static ViMG.Cubes.Cube;

namespace ViMG.Cubes
{
    public class CubeMushroomOrangeTop : Cube
    {
        public CubeMushroomOrangeTop() : base("mushroom_orange_top", 4)
        {
            Name = "Orange Mushroom Top";

            Client = new ClientCubeMushroomOrangeTop(this);
        }

        public override void GetDrops(List<ItemInstance> itemsToDrop)
        {
            base.GetDrops(itemsToDrop);

            DropSelf(itemsToDrop);
        }
    }

    public class ClientCubeMushroomOrangeTop : ClientCube
    {
        public ClientCubeMushroomOrangeTop(Cube cube) : base(cube, new CubeFacingLayout(new RectangleF(80, 96, 16, 16), new RectangleF(48, 96, 16, 16), new RectangleF(64, 96, 16, 16)), Color.White)
        {
        }

        public override RectangleF GetSourceRect(RenderPass pass, CopiedChunkManager.CopiedChunkData data, ChunkRenderMesher.CubeMeshingParameters parameters, MeshHelper.CubeFace face)
        {
            //we're meshing one of the sides.
            if ((face & MeshHelper.CubeFace.SIDES) > 0)
            {
                //if the cube above is a mushroom block
                if (data.GetCube(parameters.position - new CubePosition(0, 1, 0, CubePosition.CoordinateSpace.ChunkSpace)).GetOrDefault(Main.Registry.CubeRegistry.Air) == cube)
                {
                    //use the same top texture instead of the ordinary side texture.
                    return new RectangleF(48, 96, 16, 16);
                }
            }

            return base.GetSourceRect(pass, data, parameters, face);
        }
    }
}
