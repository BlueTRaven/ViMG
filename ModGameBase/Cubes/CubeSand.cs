using BrUtility;
using Engine.ChunkStuff;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG.ChunkStuff;
using ViMG.Items;
using static ViMG.Cubes.Cube;

namespace ViMG.Cubes
{
	public class CubeSand : Cube
	{
		public CubeSand() : base("sand", 2)
		{
            Client = new ClientCubeSand(this);
		}

        public override void GetDrops(List<ItemInstance> itemsToDrop)
		{
			base.GetDrops(itemsToDrop);

			DropSelf(itemsToDrop);
		}
	}

	public class ClientCubeSand : ClientCube
	{
        public ClientCubeSand(Cube cube) : base(cube, new RectangleF(176, 96, 16, 16), Color.White)
        {
        }

        public override RectangleF GetSourceRect(RenderPass pass, CopiedChunkManager.CopiedChunkData data, ChunkRenderMesher.CubeMeshingParameters parameters, MeshHelper.CubeFace face)
        {
            RectangleF sourceRect = base.GetSourceRect(pass, data, parameters, face);

            if (face == MeshHelper.CubeFace.LEFT || face == MeshHelper.CubeFace.RIGHT)
            {
                if (parameters.position.Z % 2 == 1)
                    sourceRect.x += 16;
                if (parameters.position.Y % 2 == 1)
                    sourceRect.y += 16;
            }
            if (face == MeshHelper.CubeFace.FRONT || face == MeshHelper.CubeFace.BACK)
            {
                if (parameters.position.X % 2 == 1)
                    sourceRect.x += 16;
                if (parameters.position.Y % 2 == 1)
                    sourceRect.y += 16;
            }
            if (face == MeshHelper.CubeFace.UP || face == MeshHelper.CubeFace.DOWN)
            {
                if (parameters.position.X % 2 == 1)
                    sourceRect.x += 16;
                if (parameters.position.Z % 2 == 1)
                    sourceRect.y += 16;
            }

            return sourceRect;
        }
    }
}
