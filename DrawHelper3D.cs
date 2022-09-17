using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace ViMG
{
	public static class DrawHelper3D
	{
		public static (VertexBuffer VBO, IndexBuffer IBO) MakeUVSphere(GraphicsDevice device, float radius)
        {
			VertexBuffer vbo;
			IndexBuffer ibo;

			List<VertexPositionColorTextureNormal> vertices = new List<VertexPositionColorTextureNormal>();
			List<uint> indices = new List<uint>();

			//https://gamedev.stackexchange.com/questions/16585/how-do-you-programmatically-generate-a-sphere
			int stacks = 16;
			int slices = 16;

			for (int t = 0; t < stacks; t++)
			{
				float theta1 = ((float)(t) / stacks) * MathF.PI;
				float theta2 = ((float)(t + 1) / stacks) * MathF.PI;

				for (int p = 0; p < slices; p++) // slices are ORANGE SLICES so the count azimuth
				{
					float phi1 = ((float)(p) / slices) * 2 * MathF.PI; // azimuth goes around 0 .. 2*PI
					float phi2 = ((float)(p + 1) / slices) * 2 * MathF.PI;

					Vector3 vert1 = FromSphericalCoordinates(radius, phi1, theta1);
					Vector3 vert2 = FromSphericalCoordinates(radius, phi2, theta1);
					Vector3 vert3 = FromSphericalCoordinates(radius, phi2, theta2);
					Vector3 vert4 = FromSphericalCoordinates(radius, phi1, theta2);

					uint indicesStart = (uint)vertices.Count;

					if (t == 0)
                    {
						indices.Add(indicesStart + 0);
						indices.Add(indicesStart + 1);
						indices.Add(indicesStart + 2);

						Vector3 dir = Vector3.Cross(vert3 - vert1, vert4 - vert1);
						Vector3 norm = Vector3.Normalize(dir);

						vertices.Add(new VertexPositionColorTextureNormal(vert1, Color.White, Vector2.Zero, norm));
						vertices.Add(new VertexPositionColorTextureNormal(vert3, Color.White, Vector2.Zero, norm));
						vertices.Add(new VertexPositionColorTextureNormal(vert4, Color.White, Vector2.Zero, norm));
					}
					else if (t + 1 == stacks)
                    {
						indices.Add(indicesStart + 0);
						indices.Add(indicesStart + 1);
						indices.Add(indicesStart + 2);

						Vector3 dir = Vector3.Cross(vert1 - vert3, vert2 - vert3);
						Vector3 norm = Vector3.Normalize(dir);

						vertices.Add(new VertexPositionColorTextureNormal(vert3, Color.White, Vector2.Zero, norm));
						vertices.Add(new VertexPositionColorTextureNormal(vert1, Color.White, Vector2.Zero, norm));
						vertices.Add(new VertexPositionColorTextureNormal(vert2, Color.White, Vector2.Zero, norm));
					}
                    else
                    {
						indices.Add(indicesStart + 0);
						indices.Add(indicesStart + 1);
						indices.Add(indicesStart + 3);
						indices.Add(indicesStart + 1);
						indices.Add(indicesStart + 2);
						indices.Add(indicesStart + 3);

						Vector3 dir = Vector3.Cross(vert2 - vert1, vert4 - vert1);
						Vector3 norm = Vector3.Normalize(dir);

						vertices.Add(new VertexPositionColorTextureNormal(vert1, Color.White, Vector2.Zero, norm));
						vertices.Add(new VertexPositionColorTextureNormal(vert2, Color.White, Vector2.Zero, norm));
						vertices.Add(new VertexPositionColorTextureNormal(vert3, Color.White, Vector2.Zero, norm));
						vertices.Add(new VertexPositionColorTextureNormal(vert4, Color.White, Vector2.Zero, norm));
					}
				}
			}

			vbo = new VertexBuffer(device, typeof(VertexPositionColorTextureNormal), vertices.Count, BufferUsage.WriteOnly);
			ibo = new IndexBuffer(device, typeof(uint), indices.Count, BufferUsage.WriteOnly);

			vbo.SetData(vertices.ToArray());
			ibo.SetData(indices.ToArray());

			return (vbo, ibo);
        }

		private static Vector3 FromSphericalCoordinates(float r, float theta, float phi)
        {
			float x = r * MathF.Sin(phi) * MathF.Cos(theta);
			float y = r * MathF.Sin(phi) * MathF.Sin(theta);
			float z = r * MathF.Cos(phi);

			return new Vector3(x, y, z);
		}

		private static SimpleMesh<VertexPositionTexture, int> axesMesh;
		public static void DrawAxesImmediate(GraphicsDevice device, Vector3 position)
		{
			device.RasterizerState = Main.noCullRS;
			device.DepthStencilState = Main.nodepthDSS;

			if (axesMesh == null)
				axesMesh = MakeAxes(device, Vector3.Zero, new Vector3(5), Main.assetsManager.GetAsset<Texture2D>("axes"));

			axesMesh.DrawDebugVertexPositionTexture(device, Main.VertexPositionTextureDebugEffect, Color.White, Transform.FromTRS(position, Vector3.Zero, Vector3.One));
		}

		public static SimpleMesh<VertexPositionTexture, int> MakeAxes(GraphicsDevice device, Vector3 min, Vector3 max, Texture2D texture)
		{
			List<VertexPositionTexture> vertices = new List<VertexPositionTexture>();
			List<int> indices = new List<int>();

			MakeAxesVerts(min, max, MeshHelper.CubeFace.ALL, vertices, indices);

			if (texture == null)
				return new SimpleMesh<VertexPositionTexture, int>(device, vertices, indices);
			else return new SimpleMesh<VertexPositionTexture, int>(device, vertices, indices, texture);
		}

		private static void MakeAxesVerts(Vector3 min, Vector3 max, MeshHelper.CubeFace faces, List<VertexPositionTexture> vertices, List<int> indices)
		{
			Vector3 l_t_n = new Vector3(min.X, min.Y, min.Z);
			Vector3 r_t_n = new Vector3(max.X, min.Y, min.Z);
			Vector3 r_b_n = new Vector3(max.X, max.Y, min.Z);
			Vector3 l_b_n = new Vector3(min.X, max.Y, min.Z);
			Vector3 l_t_f = new Vector3(min.X, min.Y, max.Z);
			Vector3 r_t_f = new Vector3(max.X, min.Y, max.Z);
			Vector3 r_b_f = new Vector3(max.X, max.Y, max.Z);
			Vector3 l_b_f = new Vector3(min.X, max.Y, max.Z);

			int offset = vertices.Count;
			indices.Add(offset + 0);
			indices.Add(offset + 1);
			indices.Add(offset + 3);
			indices.Add(offset + 1);
			indices.Add(offset + 2);
			indices.Add(offset + 3);

			vertices.Add(new VertexPositionTexture(l_t_n, new Vector2(0.5f, 0.5f)));
			vertices.Add(new VertexPositionTexture(r_t_n, new Vector2(0, 0.5f)));
			vertices.Add(new VertexPositionTexture(r_b_n, new Vector2(0, 0)));
			vertices.Add(new VertexPositionTexture(l_b_n, new Vector2(0.5f, 0)));

			offset = vertices.Count;
			indices.Add(offset + 0);
			indices.Add(offset + 1);
			indices.Add(offset + 3);
			indices.Add(offset + 1);
			indices.Add(offset + 2);
			indices.Add(offset + 3);

			vertices.Add(new VertexPositionTexture(l_t_f, new Vector2(1f, 0.5f)));
			vertices.Add(new VertexPositionTexture(l_t_n, new Vector2(0.5f, 0.5f)));
			vertices.Add(new VertexPositionTexture(l_b_n, new Vector2(0.5f, 0)));
			vertices.Add(new VertexPositionTexture(l_b_f, new Vector2(1f, 0)));

			offset = vertices.Count;
			indices.Add(offset + 0);
			indices.Add(offset + 1);
			indices.Add(offset + 3);
			indices.Add(offset + 1);
			indices.Add(offset + 2);
			indices.Add(offset + 3);

			vertices.Add(new VertexPositionTexture(l_t_f, new Vector2(0.5f, 0.5f)));
			vertices.Add(new VertexPositionTexture(r_t_f, new Vector2(0, 0.5f)));
			vertices.Add(new VertexPositionTexture(r_t_n, new Vector2(0, 1f)));
			vertices.Add(new VertexPositionTexture(l_t_n, new Vector2(0.5f, 1f)));
		}

		[Obsolete]
		public static void DrawCubeImmediate(GraphicsDevice device, Vector3 position, Vector3 size, Color color, Matrix? matrix = null)
		{
			Main.BasicEffect.DiffuseColor = color.ToVector3();
			var mvp = Main.camera.GetViewMatrix() * Main.camera.GetProjectionMatrix();
			var mesh = MeshHelper.MakeCubeVertexPositionColor(device, position, position + size, MeshHelper.CubeFace.ALL, color, DrawHelper.WhitePixel);

			if (matrix.HasValue)
				mesh.Draw(device, Main.BasicEffect, matrix.Value);
			else mesh.DrawDebugVertexPositionColor(device, Main.VertexPositionColorDebugEffect, Color.White, Matrix.Identity);
			Main.BasicEffect.DiffuseColor = Color.White.ToVector3();
		}

		[Obsolete]
		public static void DrawQuadImmediate(GraphicsDevice device, Vector3 min, Vector3 max, Color color)
		{
			List<VertexPositionColor> vertices = new List<VertexPositionColor>();
			List<int> indices = new List<int>();
			
			Vector3 a = new Vector3(min.X, min.Y, min.Z);
			Vector3 b = new Vector3(max.X, min.Y, min.Z);
			Vector3 c = new Vector3(max.X, max.Y, min.Z);
			Vector3 d = new Vector3(min.X, max.Y, min.Z);

			int offset = vertices.Count;
			indices.Add(offset + 0);
			indices.Add(offset + 1);
			indices.Add(offset + 3);
			indices.Add(offset + 1);
			indices.Add(offset + 2);
			indices.Add(offset + 3);

			vertices.Add(new VertexPositionColor(a, color));
			vertices.Add(new VertexPositionColor(b, color));
			vertices.Add(new VertexPositionColor(c, color));
			vertices.Add(new VertexPositionColor(d, color));

			//MeshHelper.MakeQuadVertsVertexPositionColor(a, b, c, d, color, vertices, indices);

			new SimpleMesh<VertexPositionColor, int>(device, vertices, indices).DrawDebugVertexPositionColor(device, Main.VertexPositionColorDebugEffect, Color.White, Matrix.Identity);
		}

		[Obsolete]
		public static void DrawTexturedQuadImmediate(GraphicsDevice device, Vector3 min, Vector3 max, Matrix transform, Texture2D texture)
		{
			Vector3 a = new Vector3(max.X, min.Y, max.Z);
			Vector3 b = new Vector3(min.X, min.Y, max.Z);
			Vector3 c = new Vector3(min.X, max.Y, max.Z);
			Vector3 d = new Vector3(max.X, max.Y, max.Z);

			List<VertexPositionTexture> vertices = new List<VertexPositionTexture>();
			List<int> indices = new List<int>();

			MeshHelper.MakeQuadVertsVertexPositionTexture(a, b, c, d, new Vector2(1, 0), new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1), vertices, indices);

			new SimpleMesh<VertexPositionTexture, int>(device, vertices, indices, texture).DrawDebugVertexPositionTexture(device, Main.VertexPositionTextureDebugEffect, Color.White, transform);
		}
	}
}
