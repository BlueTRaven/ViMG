using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Cubes;
using ViMG.Rendering;

namespace ViMG.Items
{
	public class ItemDebugDepthTarget : Item
	{
		public RenderTarget2D DepthTarget;

		private static SimpleMesh<VertexPositionTexture, int> depthMesh;
		public ItemDebugDepthTarget() : base("debug_depth_target", new BrUtility.RectangleF(0, 0, 0, 0))
		{
			name = "DEBUG SHADOW DEPTH RENDERER";
			description = "Renders the depth buffer into your very hands.\n" +
				"Shadows are " + (Main.ENABLE_SHADOWS ? "enabled" : "disabled") + ".";
		}

        public override RendererDeferred.DrawMaterial GetMaterial()
        {
            return new RendererDeferred.DrawMaterial();
        }

        public override void DrawInWorld(GraphicsDevice device, ItemInstance item, Matrix transform)
		{
			//base.Draw(device, item, transform);

			if (depthMesh == null)
				MakeMesh(device);

			depthMesh.DrawDebugVertexPositionTexture(device, Main.VertexPositionTextureDebugEffect, Color.White, transform, DepthTarget);
		}

		public override void DrawInInventory(SpriteBatch batch, ItemInstance item, Vector2 position, float scale)
		{
			//base.DrawInInventory(batch, position, scale);
		}

		private void MakeMesh(GraphicsDevice device)
		{
			Vector3 min = Vector3.Zero;
			Vector3 max = new Vector3(Cube.CUBE_SCALE, Cube.CUBE_SCALE, Cube.CUBE_SCALE / 2f);

			Vector3 a = new Vector3(max.X, min.Y, max.Z);
			Vector3 b = new Vector3(min.X, min.Y, max.Z);
			Vector3 c = new Vector3(min.X, max.Y, max.Z);
			Vector3 d = new Vector3(max.X, max.Y, max.Z);

			List<VertexPositionTexture> vertices = new List<VertexPositionTexture>();
			List<int> indices = new List<int>();

			Vector2 atx = new Vector2(0, 1);
			Vector2 btx = new Vector2(1, 1);
			Vector2 ctx = new Vector2(1, 0);
			Vector2 dtx = new Vector2(0, 0);

			int offset = vertices.Count;
			indices.Add(offset + 0);
			indices.Add(offset + 1);
			indices.Add(offset + 3);
			indices.Add(offset + 1);
			indices.Add(offset + 2);
			indices.Add(offset + 3);

			vertices.Add(new VertexPositionTexture(a, atx));
			vertices.Add(new VertexPositionTexture(b, btx));
			vertices.Add(new VertexPositionTexture(c, ctx));
			vertices.Add(new VertexPositionTexture(d, dtx));

			/*offset = vertices.Count;
			indices.Add(offset + 0);
			indices.Add(offset + 1);
			indices.Add(offset + 3);
			indices.Add(offset + 1);
			indices.Add(offset + 2);
			indices.Add(offset + 3);

			vertices.Add(new VertexPositionTexture(b, btx));
			vertices.Add(new VertexPositionTexture(a, atx));
			vertices.Add(new VertexPositionTexture(d, dtx));
			vertices.Add(new VertexPositionTexture(c, ctx));*/

			depthMesh = new SimpleMesh<VertexPositionTexture, int>(device, vertices, indices);
		}
	}
}
