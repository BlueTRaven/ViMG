using BrUtility;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG.ChunkStuff;
using ViMG.Items;

namespace ViMG.Cubes
{
	public class CubeSand : Cube
	{
		public CubeSand() : base("sand", new RectangleF(176, 96, 16, 16), Color.White, 2)
		{
		}

        public override RectangleF GetSourceRect(RenderPass pass, CopiedChunkData data, ChunkRenderMesher.CubeMeshingParameters parameters, MeshHelper.CubeFace face)
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

        public override void GetDrops(List<ItemInstance> itemsToDrop)
		{
			base.GetDrops(itemsToDrop);

			DropSelf(itemsToDrop);
		}
	}
}
