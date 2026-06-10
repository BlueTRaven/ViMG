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
    public class CubeGlass : Cube
    {
        public CubeGlass() : base("glass", 4)
        {
            Transparency = TransparencyValue.TransparentOccludesSiblings;

            Name = "Glass";
        }

        public override ClientCube ClientInit()
        {
            return new ClientCubeGlass(this);
        }

        public override bool ShouldMeshPass(RenderPass pass)
        {
            return pass == RenderPass.Transparent || pass == RenderPass.Opaque;
        }

        public override void GetDrops(List<ItemInstance> itemsToDrop)
        {
            base.GetDrops(itemsToDrop);

            DropSelf(itemsToDrop);
        }
    }

    public class ClientCubeGlass : ClientCube
    {
        public ClientCubeGlass(Cube cube) : base(cube, RectangleF.Empty, Color.White)
        {
        }

        public override RectangleF GetSourceRect(RenderPass pass, CopiedChunkManager.CopiedChunkData data, ChunkRenderMesher.CubeMeshingParameters parameters, MeshHelper.CubeFace face)
        {
            return base.GetSourceRect(pass, data, parameters, face);
        }

        public override RectangleF GetSourceRect(RenderPass pass, CopiedChunkManager.CopiedChunkData data, ChunkRenderMesher.CubeMeshingParameters parameters)
        {
            if (pass == RenderPass.Transparent)
                return new RectangleF(24, 36, 0, 0);
            else if (pass == RenderPass.Opaque)
                return new RectangleF(0, 32, 16, 16);
            else return new RectangleF();
        }
    }
}
