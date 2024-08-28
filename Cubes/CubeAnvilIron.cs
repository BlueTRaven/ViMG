using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG.ChunkStuff;
using ViMG.Entities;
using ViMG.Items;
using ViMG.Recipes;
using ViMG.UIs;
using ViMG.VertexDeclarations;

namespace ViMG.Cubes
{
    public class CubeAnvilIron : Cube
	{
		public CubeAnvilIron() : base("anvil_iron", new CubeFacingLayout(new RectangleF(144, 48, 16, 16), new RectangleF(160, 48, 16, 16), new RectangleF(176, 48, 16, 16)), Color.White, 6)
		{
			Transparency = TransparencyValue.Transparent;
		}

		public override void OnPlayerPlaced(Player player, CubePosition position)
		{
			base.OnPlayerPlaced(player, position);

			player.GetWorld().EntityManager.Add(new EntityAnvilIron(position));
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

        public override void MakeCubeVerts(RenderPass pass, CopiedChunkData data, ChunkRenderMesher.CubeMeshingParameters parameters, FastList<VertexCube> vertices, List<int> indices, int vertexOffset = 0)
        {
			/*CubePosition cp = CubePosition.FromWorldSpace(pos);

			Vector3 l_t_n = new Vector3(min.X, min.Y, min.Z);
			Vector3 r_t_n = new Vector3(max.X, min.Y, min.Z);
			Vector3 r_b_n = new Vector3(max.X, max.Y, min.Z);
			Vector3 l_b_n = new Vector3(min.X, max.Y, min.Z);
			Vector3 l_t_f = new Vector3(min.X, min.Y, max.Z);
			Vector3 r_t_f = new Vector3(max.X, min.Y, max.Z);
			Vector3 r_b_f = new Vector3(max.X, max.Y, max.Z);
			Vector3 l_b_f = new Vector3(min.X, max.Y, max.Z);

			Vector3 leftOffset = new Vector3(PIXEL_SCALE, 0, 0);
			Vector3 backOffset = new Vector3(0, 0, PIXEL_SCALE);
			//A little hacky, but DOWN corresponds to the trim responsible for making this look 3d.
			ChunkMesher.MakeQuadVerts(pass, world, cp, l_t_n + backOffset, r_t_n + backOffset, r_b_n + backOffset, l_b_n + backOffset, new Vector3(0, 0, -1), MeshHelper.CubeFace.DOWN, this, vertices, indices);

			ChunkMesher.MakeQuadVerts(pass, world, cp, r_t_n - leftOffset, r_t_f - leftOffset, r_b_f - leftOffset, r_b_n - leftOffset, new Vector3(1, 0, 0), MeshHelper.CubeFace.DOWN, this, vertices, indices);

			ChunkMesher.MakeQuadVerts(pass, world, cp, r_t_f - backOffset, l_t_f - backOffset, l_b_f - backOffset, r_b_f - backOffset, new Vector3(0, 0, 1), MeshHelper.CubeFace.DOWN, this, vertices, indices);

			ChunkMesher.MakeQuadVerts(pass, world, cp, l_t_f + leftOffset, l_t_n + leftOffset, l_b_n + leftOffset, l_b_f + leftOffset, new Vector3(-1, 0, 0), MeshHelper.CubeFace.DOWN, this, vertices, indices);

			max.Y -= PIXEL_SCALE * 5;

			ChunkMesher.MakeQuadVerts(pass, world, cp, new Vector3(max.X, max.Y, max.Z), new Vector3(min.X, max.Y, max.Z),
				new Vector3(min.X, max.Y, min.Z), new Vector3(max.X, max.Y, min.Z), new Vector3(0, 1, 0), MeshHelper.CubeFace.UP, this, vertices, indices);

			max.Y -= PIXEL_SCALE;

			ChunkMesher.MakeQuadVerts(pass, world, cp, new Vector3(min.X, max.Y, max.Z), new Vector3(max.X, max.Y, max.Z),
				new Vector3(max.X, max.Y, min.Z), new Vector3(min.X, max.Y, min.Z), new Vector3(0, -1, 0), MeshHelper.CubeFace.UP, this, vertices, indices);*/

            parameters.positionWS += new Vector3(CUBE_SCALE / 2f, 0, CUBE_SCALE / 2f);
            DrawHelper3D.MakeXMeshVerts(pass, data, parameters, Vector3.One, vertices, indices, vertexOffset);
        }
    }
}
