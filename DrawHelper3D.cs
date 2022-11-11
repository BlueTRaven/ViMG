using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Cubes;

namespace ViMG
{
	public static class DrawHelper3D
	{
		private static VertexBuffer vboQuad;
		private static IndexBuffer iboQuad;

		public static void DrawFullscreenQuad(GraphicsDevice device, Effect effect)
		{
			if (vboQuad == null)
			{
				VertexPositionTexture[] vpt = new VertexPositionTexture[4]
				{
					new VertexPositionTexture(new Vector3(-1, 1, 0), new Vector2(0, 0)),
					new VertexPositionTexture(new Vector3(1, 1, 0), new Vector2(1, 0)),
					new VertexPositionTexture(new Vector3(1, -1, 0), new Vector2(1, 1)),
					new VertexPositionTexture(new Vector3(-1, -1, 0), new Vector2(0, 1)),
				};

				uint[] indices = new uint[6]
				{
					0, 1, 2,
					2, 3, 0,
				};

				vboQuad = new VertexBuffer(device, typeof(VertexPositionTexture), vpt.Length, BufferUsage.WriteOnly);
				vboQuad.SetData(vpt);

				iboQuad = new IndexBuffer(device, typeof(uint), 6, BufferUsage.WriteOnly);
				iboQuad.SetData(indices);
			}

			device.SetVertexBuffer(vboQuad);
			device.Indices = iboQuad;

			foreach (var pass in effect.CurrentTechnique.Passes)
			{
				pass.Apply();
				device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, iboQuad.IndexCount / 3);
			}
		}

		public static void MakeXMeshVerts(Cube.RenderPass pass, Cube cube, World world, Vector3 pos, Vector3 scale, List<VertexCube> vertices, List<int> indices)
        {
			int verticesStart = vertices.Count;

			const int textureWidth = 1024;
			const int textureHeight = 1024;

			const float texelX = 1f / textureWidth;
			const float texelY = 1f / textureHeight;

			//face doesn't matter, any works
			RectangleF sourceRect = cube.GetSourceRect(pass, world, CubePosition.FromWorldSpace(pos));
			//convert source rect to texture space (0-1 instead of 0-width/height in pixels)
			sourceRect = new RectangleF(sourceRect.x * texelX, sourceRect.y * texelY, sourceRect.width * texelX, sourceRect.height * texelY);

			MakeXMeshRaw(vertices, indices, pos, scale, sourceRect);
			/*Vector3 xMin = -new Vector3(Cube.CUBE_SCALE / 2, 0, -Cube.CUBE_SCALE / 2);
			Vector3 xMax = new Vector3(Cube.CUBE_SCALE / 2, Cube.CUBE_SCALE, Cube.CUBE_SCALE / 2);

			const int textureWidth = 1024;
			const int textureHeight = 1024;

			const float cubeSideWidth = 1f / textureWidth;
			const float cubeSideHeight = 1f / textureHeight;

			//face doesn't matter, any works
			RectangleF sourceRect = cube.GetSourceRect(pass, world, CubePosition.FromWorldSpace(pos));

			Vector2 uvNear = new Vector2(sourceRect.x * cubeSideWidth, sourceRect.y * cubeSideHeight);
			Vector2 uvFar = new Vector2((sourceRect.x + sourceRect.width) * cubeSideWidth, (sourceRect.y + sourceRect.height) * cubeSideHeight);

			Matrix rotFirstPlane = Matrix.CreateRotationY(MathHelper.ToRadians(45));
			Matrix rotSecondPlane = Matrix.CreateRotationY(MathHelper.ToRadians(90 + 45));

			Vector3 a = new Vector3(xMax.X, xMin.Y, xMax.Z);
			Vector3 b = new Vector3(xMin.X, xMin.Y, xMax.Z);
			Vector3 c = new Vector3(xMin.X, xMax.Y, xMax.Z);
			Vector3 d = new Vector3(xMax.X, xMax.Y, xMax.Z);

			Vector3 nrmFirstPlaneMin = new Vector3(0, 0, 1);
			Vector3 nrmFirstPlaneMax = new Vector3(0, 0, -1);
			Vector3 nrmSecondPlaneMin = new Vector3(-1, 0, 0);
			Vector3 nrmSecondPlaneMax = new Vector3(1, 0, 0);

			nrmFirstPlaneMin = Vector3.Transform(nrmFirstPlaneMin, rotFirstPlane);
			nrmFirstPlaneMax = Vector3.Transform(nrmFirstPlaneMax, rotFirstPlane);
			nrmSecondPlaneMin = Vector3.Transform(nrmSecondPlaneMin, rotSecondPlane);
			nrmSecondPlaneMax = Vector3.Transform(nrmSecondPlaneMax, rotSecondPlane);

			a = Vector3.Transform(a, rotFirstPlane);
			b = Vector3.Transform(b, rotFirstPlane);
			c = Vector3.Transform(c, rotFirstPlane);
			d = Vector3.Transform(d, rotFirstPlane);

			a += pos;
			b += pos;
			c += pos;
			d += pos;

			Vector3 e = new Vector3(xMin.X, xMin.Y, xMax.Z) + new Vector3(-xMax.X, 0, -xMax.Z);
			Vector3 f = new Vector3(xMax.X, xMin.Y, xMax.Z) + new Vector3(-xMax.X, 0, -xMax.Z);
			Vector3 g = new Vector3(xMax.X, xMax.Y, xMax.Z) + new Vector3(-xMax.X, 0, -xMax.Z);
			Vector3 h = new Vector3(xMin.X, xMax.Y, xMax.Z) + new Vector3(-xMax.X, 0, -xMax.Z);

			e = Vector3.Transform(e, rotSecondPlane);
			f = Vector3.Transform(f, rotSecondPlane);
			g = Vector3.Transform(g, rotSecondPlane);
			h = Vector3.Transform(h, rotSecondPlane);

			e += pos;
			f += pos;
			g += pos;
			h += pos;

			Vector2 atx = new Vector2(uvFar.X, uvFar.Y);
			Vector2 btx = new Vector2(uvNear.X, uvFar.Y);
			Vector2 ctx = new Vector2(uvNear.X, uvNear.Y);
			Vector2 dtx = new Vector2(uvFar.X, uvNear.Y);

			int offset = vertices.Count;
			indices.Add(offset + 0);
			indices.Add(offset + 1);
			indices.Add(offset + 3);
			indices.Add(offset + 1);
			indices.Add(offset + 2);
			indices.Add(offset + 3);

			vertices.Add(new VertexCube(a, Color.White, atx, nrmFirstPlaneMin));
			vertices.Add(new VertexCube(b, Color.White, btx, nrmFirstPlaneMin));
			vertices.Add(new VertexCube(c, Color.White, ctx, nrmFirstPlaneMin));
			vertices.Add(new VertexCube(d, Color.White, dtx, nrmFirstPlaneMin));

			offset = vertices.Count;
			indices.Add(offset + 0);
			indices.Add(offset + 1);
			indices.Add(offset + 3);
			indices.Add(offset + 1);
			indices.Add(offset + 2);
			indices.Add(offset + 3);

			vertices.Add(new VertexCube(b, Color.White, btx, nrmFirstPlaneMax));
			vertices.Add(new VertexCube(a, Color.White, atx, nrmFirstPlaneMax));
			vertices.Add(new VertexCube(d, Color.White, dtx, nrmFirstPlaneMax));
			vertices.Add(new VertexCube(c, Color.White, ctx, nrmFirstPlaneMax));

			offset = vertices.Count;
			indices.Add(offset + 0);
			indices.Add(offset + 1);
			indices.Add(offset + 3);
			indices.Add(offset + 1);
			indices.Add(offset + 2);
			indices.Add(offset + 3);

			vertices.Add(new VertexCube(e, Color.White, atx, nrmSecondPlaneMin));
			vertices.Add(new VertexCube(f, Color.White, btx, nrmSecondPlaneMin));
			vertices.Add(new VertexCube(g, Color.White, ctx, nrmSecondPlaneMin));
			vertices.Add(new VertexCube(h, Color.White, dtx, nrmSecondPlaneMin));

			offset = vertices.Count;
			indices.Add(offset + 0);
			indices.Add(offset + 1);
			indices.Add(offset + 3);
			indices.Add(offset + 1);
			indices.Add(offset + 2);
			indices.Add(offset + 3);

			vertices.Add(new VertexCube(f, Color.White, btx, nrmSecondPlaneMax));
			vertices.Add(new VertexCube(e, Color.White, atx, nrmSecondPlaneMax));
			vertices.Add(new VertexCube(h, Color.White, dtx, nrmSecondPlaneMax));
			vertices.Add(new VertexCube(g, Color.White, ctx, nrmSecondPlaneMax));*/

			int verticesEnd = vertices.Count;

			ApplyCubeAnim(pass, world, CubePosition.FromWorldSpace(pos), cube, MeshHelper.CubeFace.ALL, vertices, verticesStart, verticesEnd);
		}

		public static void MakeXMeshRaw(List<VertexCube> vertices, List<int> indices, Vector3 pos, Vector3 scale, RectangleF sourceRect)
        {
			Vector3 min = -new Vector3(Cube.CUBE_SCALE / 2 * scale.X, 0, Cube.CUBE_SCALE / 2 * scale.Z);
			Vector3 max = new Vector3(Cube.CUBE_SCALE / 2 * scale.X, Cube.CUBE_SCALE * scale.Y, Cube.CUBE_SCALE / 2 * scale.Z);

			Vector3 a = pos + new Vector3(min.X, min.Y, min.Z);
			Vector3 b = pos + new Vector3(min.X, max.Y, min.Z);
			Vector3 c = pos + new Vector3(max.X, max.Y, min.Z);
			Vector3 d = pos + new Vector3(max.X, min.Y, min.Z);

			Vector3 e = pos + new Vector3(min.X, min.Y, max.Z);
			Vector3 f = pos + new Vector3(min.X, max.Y, max.Z);
			Vector3 g = pos + new Vector3(max.X, max.Y, max.Z);
			Vector3 h = pos + new Vector3(max.X, min.Y, max.Z);

			Vector3 anrm = new Vector3(0.5f, 0, 0.5f);
			Vector3 bnrm = new Vector3(0.5f, 0, 0.5f);
			Vector3 cnrm = new Vector3(-0.5f, 0, 0.5f);
			Vector3 dnrm = new Vector3(-0.5f, 0, 0.5f);

			Vector3 enrm = new Vector3(-0.5f, 0, 0.5f);
			Vector3 fnrm = new Vector3(-0.5f, 0, 0.5f);
			Vector3 gnrm = new Vector3(0.5f, 0, 0.5f);
			Vector3 hnrm = new Vector3(0.5f, 0, 0.5f);

			int offset = vertices.Count;

			vertices.Add(new VertexCube(a, Color.White, new Vector2(sourceRect.x, sourceRect.y + sourceRect.height), anrm));
			vertices.Add(new VertexCube(b, Color.White, new Vector2(sourceRect.x, sourceRect.y), bnrm));
			vertices.Add(new VertexCube(c, Color.White, new Vector2(sourceRect.x + sourceRect.width, sourceRect.y), cnrm));
			vertices.Add(new VertexCube(d, Color.White, new Vector2(sourceRect.x + sourceRect.width, sourceRect.y + sourceRect.height), dnrm));
														
			vertices.Add(new VertexCube(e, Color.White, new Vector2(sourceRect.x, sourceRect.y + sourceRect.height), enrm));
			vertices.Add(new VertexCube(f, Color.White, new Vector2(sourceRect.x, sourceRect.y), fnrm));
			vertices.Add(new VertexCube(g, Color.White, new Vector2(sourceRect.x + sourceRect.width, sourceRect.y), gnrm));
			vertices.Add(new VertexCube(h, Color.White, new Vector2(sourceRect.x + sourceRect.width, sourceRect.y + sourceRect.height), hnrm));

			indices.Add(offset + 0);
			indices.Add(offset + 1);
			indices.Add(offset + 6);
			indices.Add(offset + 6);
			indices.Add(offset + 7);
			indices.Add(offset + 0);

			indices.Add(offset + 4);
			indices.Add(offset + 5);
			indices.Add(offset + 2);
			indices.Add(offset + 2);
			indices.Add(offset + 3);
			indices.Add(offset + 4);

			offset = vertices.Count;

			vertices.Add(new VertexCube(a, Color.White, new Vector2(sourceRect.x, sourceRect.y + sourceRect.height), -anrm));
			vertices.Add(new VertexCube(b, Color.White, new Vector2(sourceRect.x, sourceRect.y), -bnrm));
			vertices.Add(new VertexCube(c, Color.White, new Vector2(sourceRect.x + sourceRect.width, sourceRect.y), -cnrm));
			vertices.Add(new VertexCube(d, Color.White, new Vector2(sourceRect.x + sourceRect.width, sourceRect.y + sourceRect.height), -dnrm));

			vertices.Add(new VertexCube(e, Color.White, new Vector2(sourceRect.x, sourceRect.y + sourceRect.height), -enrm));
			vertices.Add(new VertexCube(f, Color.White, new Vector2(sourceRect.x, sourceRect.y), -fnrm));
			vertices.Add(new VertexCube(g, Color.White, new Vector2(sourceRect.x + sourceRect.width, sourceRect.y), -gnrm));
			vertices.Add(new VertexCube(h, Color.White, new Vector2(sourceRect.x + sourceRect.width, sourceRect.y + sourceRect.height), -hnrm));

			indices.Add(offset + 6);
			indices.Add(offset + 1);
			indices.Add(offset + 0);
			indices.Add(offset + 0);
			indices.Add(offset + 7);
			indices.Add(offset + 6);

			indices.Add(offset + 2);
			indices.Add(offset + 5);
			indices.Add(offset + 4);
			indices.Add(offset + 4);
			indices.Add(offset + 3);
			indices.Add(offset + 2);


			//face doesn't matter, any works
			/*Vector2 uvNear = new Vector2(sourceRect.x, sourceRect.y);
			Vector2 uvFar = new Vector2(sourceRect.x + sourceRect.width, sourceRect.y + sourceRect.height);

			Matrix rotFirstPlane = Matrix.CreateRotationY(MathHelper.ToRadians(0));
			Matrix rotSecondPlane = Matrix.CreateRotationY(MathHelper.ToRadians(90 + 0));

			Vector3 a = new Vector3(max.X, min.Y, min.Z);
			Vector3 b = new Vector3(min.X, min.Y, min.Z);
			Vector3 c = new Vector3(min.X, max.Y, min.Z);
			Vector3 d = new Vector3(max.X, max.Y, min.Z);

			Vector3 nrmFirstPlaneMin = new Vector3(0, 0, 1);
			Vector3 nrmFirstPlaneMax = new Vector3(0, 0, -1);
			Vector3 nrmSecondPlaneMin = new Vector3(-1, 0, 0);
			Vector3 nrmSecondPlaneMax = new Vector3(1, 0, 0);

			nrmFirstPlaneMin = Vector3.Transform(nrmFirstPlaneMin, rotFirstPlane);
			nrmFirstPlaneMax = Vector3.Transform(nrmFirstPlaneMax, rotFirstPlane);
			nrmSecondPlaneMin = Vector3.Transform(nrmSecondPlaneMin, rotSecondPlane);
			nrmSecondPlaneMax = Vector3.Transform(nrmSecondPlaneMax, rotSecondPlane);

			a = Vector3.Transform(a, rotFirstPlane);
			b = Vector3.Transform(b, rotFirstPlane);
			c = Vector3.Transform(c, rotFirstPlane);
			d = Vector3.Transform(d, rotFirstPlane);

			a += pos;
			b += pos;
			c += pos;
			d += pos;

			Vector3 e = new Vector3(min.X, min.Y, min.Z) + new Vector3(-max.X, 0, -max.Z);
			Vector3 f = new Vector3(max.X, min.Y, min.Z) + new Vector3(-max.X, 0, -max.Z);
			Vector3 g = new Vector3(max.X, max.Y, min.Z) + new Vector3(-max.X, 0, -max.Z);
			Vector3 h = new Vector3(min.X, max.Y, min.Z) + new Vector3(-max.X, 0, -max.Z);

			e = Vector3.Transform(e, rotSecondPlane);
			f = Vector3.Transform(f, rotSecondPlane);
			g = Vector3.Transform(g, rotSecondPlane);
			h = Vector3.Transform(h, rotSecondPlane);

			e += pos;
			f += pos;
			g += pos;
			h += pos;

			Vector2 atx = new Vector2(uvFar.X, uvFar.Y);
			Vector2 btx = new Vector2(uvNear.X, uvFar.Y);
			Vector2 ctx = new Vector2(uvNear.X, uvNear.Y);
			Vector2 dtx = new Vector2(uvFar.X, uvNear.Y);

			int offset = vertices.Count;
			indices.Add(offset + 0);
			indices.Add(offset + 1);
			indices.Add(offset + 3);
			indices.Add(offset + 1);
			indices.Add(offset + 2);
			indices.Add(offset + 3);

			vertices.Add(new VertexCube(a, Color.White, atx, nrmFirstPlaneMin));
			vertices.Add(new VertexCube(b, Color.White, btx, nrmFirstPlaneMin));
			vertices.Add(new VertexCube(c, Color.White, ctx, nrmFirstPlaneMin));
			vertices.Add(new VertexCube(d, Color.White, dtx, nrmFirstPlaneMin));

			offset = vertices.Count;
			indices.Add(offset + 0);
			indices.Add(offset + 1);
			indices.Add(offset + 3);
			indices.Add(offset + 1);
			indices.Add(offset + 2);
			indices.Add(offset + 3);

			vertices.Add(new VertexCube(b, Color.White, btx, nrmFirstPlaneMax));
			vertices.Add(new VertexCube(a, Color.White, atx, nrmFirstPlaneMax));
			vertices.Add(new VertexCube(d, Color.White, dtx, nrmFirstPlaneMax));
			vertices.Add(new VertexCube(c, Color.White, ctx, nrmFirstPlaneMax));

			offset = vertices.Count;
			indices.Add(offset + 0);
			indices.Add(offset + 1);
			indices.Add(offset + 3);
			indices.Add(offset + 1);
			indices.Add(offset + 2);
			indices.Add(offset + 3);

			vertices.Add(new VertexCube(e, Color.White, atx, nrmSecondPlaneMin));
			vertices.Add(new VertexCube(f, Color.White, btx, nrmSecondPlaneMin));
			vertices.Add(new VertexCube(g, Color.White, ctx, nrmSecondPlaneMin));
			vertices.Add(new VertexCube(h, Color.White, dtx, nrmSecondPlaneMin));

			offset = vertices.Count;
			indices.Add(offset + 0);
			indices.Add(offset + 1);
			indices.Add(offset + 3);
			indices.Add(offset + 1);
			indices.Add(offset + 2);
			indices.Add(offset + 3);

			vertices.Add(new VertexCube(f, Color.White, btx, nrmSecondPlaneMax));
			vertices.Add(new VertexCube(e, Color.White, atx, nrmSecondPlaneMax));
			vertices.Add(new VertexCube(h, Color.White, dtx, nrmSecondPlaneMax));
			vertices.Add(new VertexCube(g, Color.White, ctx, nrmSecondPlaneMax));*/
		}

		public static void ApplyCubeAnim(Cube.RenderPass pass, World world, CubePosition pos, Cube cube, MeshHelper.CubeFace face, List<VertexCube> vertices, int verticesStart, int verticesEnd)
        {
			Cube.CubeAnimation anim = cube.GetAnimation(face, pass, world, pos);
			if (anim.Valid)
			{
				for (int i = verticesStart; i < verticesEnd; i++)
				{
					VertexCube vert = vertices[i];

					vert.AnimFrameTime = anim.FrameTime;
					vert.NumAnimFrames = anim.NumFrames;
					vert.AnimFrameSize = anim.FrameWidth;

					vertices[i] = vert;
				}
			}
		}

		private static SimpleMesh<VertexCube, int> meshHealthbar;

		private static void MakeMeshHealthbar(GraphicsDevice device)
        {
			Vector2 atx = new Vector2(0, 1);
			Vector2 btx = new Vector2(1, 1);
			Vector2 ctx = new Vector2(1, 0);
			Vector2 dtx = new Vector2(0, 0);

			Vector3 min = Vector3.Zero;
			Vector3 max = new Vector3(Cube.CUBE_SCALE, Cube.CUBE_SCALE / 4, 0);

			Vector3 a = new Vector3(max.X, min.Y, max.Z);
			Vector3 b = new Vector3(min.X, min.Y, max.Z);
			Vector3 c = new Vector3(min.X, max.Y, max.Z);
			Vector3 d = new Vector3(max.X, max.Y, max.Z);

			var vertices = new List<VertexCube>();
			var indices = new List<int>();

			int offset = vertices.Count;
			indices.Add(offset + 0);
			indices.Add(offset + 1);
			indices.Add(offset + 3);
			indices.Add(offset + 1);
			indices.Add(offset + 2);
			indices.Add(offset + 3);

			vertices.Add(new VertexCube(a, Color.Red, atx, new Vector3(0, 0, 1)));
			vertices.Add(new VertexCube(b, Color.Red, btx, new Vector3(0, 0, 1)));
			vertices.Add(new VertexCube(c, Color.Red, ctx, new Vector3(0, 0, 1)));
			vertices.Add(new VertexCube(d, Color.Red, dtx, new Vector3(0, 0, 1)));

			offset = vertices.Count;
			indices.Add(offset + 0);
			indices.Add(offset + 1);
			indices.Add(offset + 3);
			indices.Add(offset + 1);
			indices.Add(offset + 2);
			indices.Add(offset + 3);

			vertices.Add(new VertexCube(b, Color.Red, btx, new Vector3(0, 0, -1)));
			vertices.Add(new VertexCube(a, Color.Red, atx, new Vector3(0, 0, -1)));
			vertices.Add(new VertexCube(d, Color.Red, dtx, new Vector3(0, 0, -1)));
			vertices.Add(new VertexCube(c, Color.Red, ctx, new Vector3(0, 0, -1)));

			meshHealthbar = new SimpleMesh<VertexCube, int>(device, vertices, indices, DrawHelper.WhitePixel);
		}

		public static void DrawHealthbar(GraphicsDevice device, int health, int maxHealth, Vector3 position)
        {
			if (meshHealthbar == null)
				MakeMeshHealthbar(device);

			Main.Renderer.DrawsPassGBuffer.Add(new Rendering.RendererDeferred.GBufferDraw(meshHealthbar.texture, DrawHelper.BlackPixel, meshHealthbar.texture, meshHealthbar.VBO, meshHealthbar.IBO,
				Matrix.CreateScale(new Vector3((float)health / (float)maxHealth, 1, 1)) *
				Matrix.CreateTranslation(new Vector3(-Cube.CUBE_SCALE / 2f, Cube.CUBE_SCALE * 1.5f, 0)) *
				Matrix.CreateRotationX(Math.Clamp(-Main.camera.Rotation.X, MathHelper.ToRadians(-15), MathHelper.ToRadians(15))) *
				Matrix.CreateRotationY(-Main.camera.Rotation.Y) *
				Matrix.CreateTranslation(position), null));
		}

		public static (VertexBuffer VBO, IndexBuffer IBO) MakeUVSphere(GraphicsDevice device, float radius, bool flip = false)
        {
			VertexBuffer vbo;
			IndexBuffer ibo;

			List<VertexCube> vertices = new List<VertexCube>();
			List<int> indices = new List<int>();

			MakeUVSphereRaw(vertices, indices, Vector3.Zero, new RectangleF(0, 0, 1, 1), radius, 16, 16, flip);

			vbo = new VertexBuffer(device, typeof(VertexCube), vertices.Count, BufferUsage.WriteOnly);
			ibo = new IndexBuffer(device, typeof(int), indices.Count, BufferUsage.WriteOnly);

			vbo.SetData(vertices.ToArray());
			ibo.SetData(indices.ToArray());

			return (vbo, ibo);
        }

		public static void MakeUVSphereRaw(List<VertexCube> vertices, List<int> indices, Vector3 position, RectangleF sourceRect, float radius, int stacks = 16, int slices = 16, bool flip = false)
        {
			//https://gamedev.stackexchange.com/questions/16585/how-do-you-programmatically-generate-a-sphere
			for (int t = 0; t < stacks; t++)
			{
				float theta1 = ((float)(t) / stacks) * MathF.PI;
				float theta2 = ((float)(t + 1) / stacks) * MathF.PI;

				for (int p = 0; p < slices; p++) // slices are ORANGE SLICES so the count azimuth
				{
					float phi1 = ((float)(p) / slices) * 2 * MathF.PI; // azimuth goes around 0 .. 2*PI
					float phi2 = ((float)(p + 1) / slices) * 2 * MathF.PI;

					Vector3 vert1 = FromSphericalCoordinates(radius, phi1, theta1) + position;
					Vector3 vert2 = FromSphericalCoordinates(radius, phi2, theta1) + position;
					Vector3 vert3 = FromSphericalCoordinates(radius, phi2, theta2) + position;
					Vector3 vert4 = FromSphericalCoordinates(radius, phi1, theta2) + position;

					Vector2 uv1 = sourceRect.Size.ToVector2() * new Vector2(phi1 / 2f / MathF.PI, theta1 / 2f / MathF.PI);
					Vector2 uv2 = sourceRect.Size.ToVector2() * new Vector2(phi2 / 2f / MathF.PI, theta1 / 2f / MathF.PI);
					Vector2 uv3 = sourceRect.Size.ToVector2() * new Vector2(phi2 / 2f / MathF.PI, theta2 / 2f / MathF.PI);
					Vector2 uv4 = sourceRect.Size.ToVector2() * new Vector2(phi1 / 2f / MathF.PI, theta2 / 2f / MathF.PI);

					uv1 += sourceRect.Position;
					uv2 += sourceRect.Position;
					uv3 += sourceRect.Position;
					uv4 += sourceRect.Position;

					int indicesStart = vertices.Count;

					if (t == 0)
					{
						if (!flip)
						{
							indices.Add(indicesStart + 0);
							indices.Add(indicesStart + 1);
							indices.Add(indicesStart + 2);
						}
                        else
                        {
							indices.Add(indicesStart + 2);
							indices.Add(indicesStart + 1);
							indices.Add(indicesStart + 0);
						}

						Vector3 dir = Vector3.Cross(vert3 - vert1, vert4 - vert1);
						Vector3 norm = Vector3.Normalize(dir);

						vertices.Add(new VertexCube(vert1, Color.White, uv1, norm));
						vertices.Add(new VertexCube(vert3, Color.White, uv3, norm));
						vertices.Add(new VertexCube(vert4, Color.White, uv4, norm));
					}
					else if (t + 1 == stacks)
					{
						if (!flip)
						{
							indices.Add(indicesStart + 0);
							indices.Add(indicesStart + 1);
							indices.Add(indicesStart + 2);
						}
                        else
                        {
							indices.Add(indicesStart + 2);
							indices.Add(indicesStart + 1);
							indices.Add(indicesStart + 0);
						}

						Vector3 dir = Vector3.Cross(vert1 - vert3, vert2 - vert3);
						Vector3 norm = Vector3.Normalize(dir);

						vertices.Add(new VertexCube(vert3, Color.White, uv3, norm));
						vertices.Add(new VertexCube(vert1, Color.White, uv1, norm));
						vertices.Add(new VertexCube(vert2, Color.White, uv2, norm));
					}
					else
					{
						if (!flip)
						{
							indices.Add(indicesStart + 0);
							indices.Add(indicesStart + 1);
							indices.Add(indicesStart + 3);
							indices.Add(indicesStart + 1);
							indices.Add(indicesStart + 2);
							indices.Add(indicesStart + 3);
						}
                        else
                        {
							indices.Add(indicesStart + 3);
							indices.Add(indicesStart + 1);
							indices.Add(indicesStart + 0);
							indices.Add(indicesStart + 3);
							indices.Add(indicesStart + 2);
							indices.Add(indicesStart + 1);
						}

						Vector3 dir = Vector3.Cross(vert2 - vert1, vert4 - vert1);
						Vector3 norm = Vector3.Normalize(dir);

						vertices.Add(new VertexCube(vert1, Color.White, uv1, norm));
						vertices.Add(new VertexCube(vert2, Color.White, uv2, norm));
						vertices.Add(new VertexCube(vert3, Color.White, uv3, norm));
						vertices.Add(new VertexCube(vert4, Color.White, uv4, norm));
					}
				}
			}
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
