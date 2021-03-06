using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Cubes;

namespace ViMG.Entities
{
	public class Tree : Entity
	{
		private static SimpleMesh<VertexPositionColorTextureNormal, int> mesh;

		public Tree(Vector3 position)
		{
			this.Position = position;
		}

		public override void Draw(GraphicsDevice device)
		{
			base.Draw(device);

			if (mesh == null)
				MakeMesh(device);

			//device.RasterizerState = Main.wireframeRS;

			mesh.Draw(device, Main.CubeEffect, Matrix.CreateRotationY(MathHelper.ToRadians(45f)) * Matrix.CreateTranslation(Position));
		}

		private static void MakeMesh(GraphicsDevice device)
		{
			Vector3 min = -new Vector3(Cube.CUBE_SCALE * 5 / 2, 0, -Cube.CUBE_SCALE * 5 / 2);
			Vector3 max = new Vector3(Cube.CUBE_SCALE * 5 / 2, Cube.CUBE_SCALE * 5, Cube.CUBE_SCALE * 5 / 2);

			Vector3 a = new Vector3(max.X, min.Y, max.Z);
			Vector3 b = new Vector3(min.X, min.Y, max.Z);
			Vector3 c = new Vector3(min.X, max.Y, max.Z);
			Vector3 d = new Vector3(max.X, max.Y, max.Z);

			Vector3 e = new Vector3(min.X, min.Y, max.Z) + new Vector3(-max.X, 0, -max.Z);
			Vector3 f = new Vector3(max.X, min.Y, max.Z) + new Vector3(-max.X, 0, -max.Z);
			Vector3 g = new Vector3(max.X, max.Y, max.Z) + new Vector3(-max.X, 0, -max.Z);
			Vector3 h = new Vector3(min.X, max.Y, max.Z) + new Vector3(-max.X, 0, -max.Z);

			e = Vector3.Transform(e, Matrix.CreateRotationY(MathHelper.ToRadians(90)));
			f = Vector3.Transform(f, Matrix.CreateRotationY(MathHelper.ToRadians(90)));
			g = Vector3.Transform(g, Matrix.CreateRotationY(MathHelper.ToRadians(90)));
			h = Vector3.Transform(h, Matrix.CreateRotationY(MathHelper.ToRadians(90)));

			List<VertexPositionColorTextureNormal> vertices = new List<VertexPositionColorTextureNormal>();
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

			vertices.Add(new VertexPositionColorTextureNormal(a, Color.White, atx, new Vector3(0, 0, 1)));
			vertices.Add(new VertexPositionColorTextureNormal(b, Color.White, btx, new Vector3(0, 0, 1)));
			vertices.Add(new VertexPositionColorTextureNormal(c, Color.White, ctx, new Vector3(0, 0, 1)));
			vertices.Add(new VertexPositionColorTextureNormal(d, Color.White, dtx, new Vector3(0, 0, 1)));

			offset = vertices.Count;
			indices.Add(offset + 0);
			indices.Add(offset + 1);
			indices.Add(offset + 3);
			indices.Add(offset + 1);
			indices.Add(offset + 2);
			indices.Add(offset + 3);

			vertices.Add(new VertexPositionColorTextureNormal(b, Color.White, btx, new Vector3(0, 0, -1)));
			vertices.Add(new VertexPositionColorTextureNormal(a, Color.White, atx, new Vector3(0, 0, -1)));
			vertices.Add(new VertexPositionColorTextureNormal(d, Color.White, dtx, new Vector3(0, 0, -1)));
			vertices.Add(new VertexPositionColorTextureNormal(c, Color.White, ctx, new Vector3(0, 0, -1)));

			offset = vertices.Count;
			indices.Add(offset + 0);
			indices.Add(offset + 1);
			indices.Add(offset + 3);
			indices.Add(offset + 1);
			indices.Add(offset + 2);
			indices.Add(offset + 3);

			vertices.Add(new VertexPositionColorTextureNormal(e, Color.White, atx, new Vector3(0, 0, -1)));
			vertices.Add(new VertexPositionColorTextureNormal(f, Color.White, btx, new Vector3(0, 0, -1)));
			vertices.Add(new VertexPositionColorTextureNormal(g, Color.White, ctx, new Vector3(0, 0, -1)));
			vertices.Add(new VertexPositionColorTextureNormal(h, Color.White, dtx, new Vector3(0, 0, -1)));

			offset = vertices.Count;
			indices.Add(offset + 0);
			indices.Add(offset + 1);
			indices.Add(offset + 3);
			indices.Add(offset + 1);
			indices.Add(offset + 2);
			indices.Add(offset + 3);

			vertices.Add(new VertexPositionColorTextureNormal(f, Color.White, btx, new Vector3(0, 0, 1)));
			vertices.Add(new VertexPositionColorTextureNormal(e, Color.White, atx, new Vector3(0, 0, 1)));
			vertices.Add(new VertexPositionColorTextureNormal(h, Color.White, dtx, new Vector3(0, 0, 1)));
			vertices.Add(new VertexPositionColorTextureNormal(g, Color.White, ctx, new Vector3(0, 0, 1)));

			mesh = new SimpleMesh<VertexPositionColorTextureNormal, int>(device, vertices, indices, Main.assetsManager.GetAsset<Texture2D>("tree"));
		}
	}
}
