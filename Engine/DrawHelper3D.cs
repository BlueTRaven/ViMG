using BrUtility;
using Engine;
using Engine.ChunkStuff;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Cubes;
using ViMG.Rendering;
using ViMG.VertexDeclarations;

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

		public static void MakeXMeshVerts(Cube.RenderPass pass, CopiedChunkManager.CopiedChunkData data, ChunkRenderMesher.CubeMeshingParameters parameters, Vector3 scale, FastList<VertexCube> vertices, List<int> indices, int vertexOffset = 0)
        {
			int verticesStart = vertices.Length;

			const int textureWidth = 1024;
			const int textureHeight = 1024;

			const float texelX = 1f / textureWidth;
			const float texelY = 1f / textureHeight;

			//face doesn't matter, any works
			RectangleF sourceRect = parameters.cube.Client.GetSourceRect(pass, data, parameters);
			//convert source rect to texture space (0-1 instead of 0-width/height in pixels)
			sourceRect = new RectangleF(sourceRect.x * texelX, sourceRect.y * texelY, sourceRect.width * texelX, sourceRect.height * texelY);

			MeshHelper.MakeXMeshVerts(vertices, indices, parameters.positionWS, scale, sourceRect, vertexOffset);

			int verticesEnd = vertices.Length;

			ApplyCubeAnim(pass, data, parameters, MeshHelper.CubeFace.ALL, vertices, verticesStart, verticesEnd);
		}

		public static void MakeXMeshRaw(FastList<VertexCube> vertices, List<int> indices, Vector3 pos, Vector3 scale, RectangleF sourceRect)
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

			int offset = vertices.Length;

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

			offset = vertices.Length;

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

		public static void ApplyCubeAnim(Cube.RenderPass pass, CopiedChunkManager.CopiedChunkData data, ChunkRenderMesher.CubeMeshingParameters parameters, MeshHelper.CubeFace face, FastList<VertexCube> vertices, int verticesStart, int verticesEnd)
        {
			Cube.CubeAnimation anim = parameters.cube.Client.GetAnimation(pass, data, parameters, parameters.faces);
			if (anim.Valid)
			{
				for (int i = verticesStart; i < verticesEnd; i++)
				{
					VertexCube vert = vertices[i];

					vert.AnimFrameTime = anim.FrameTime;
					vert.NumAnimFrames = anim.NumFrames;
					vert.AnimFrameSize = anim.FrameWidth;

					vertices.Buffer[i] = vert;
				}
			}
		}

		private static VerySimpleMesh meshHealthbar;

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

			var vertices = new FastList<VertexCube>();
			var indices = new List<int>();

			int offset = vertices.Length;
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

			offset = vertices.Length;
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

			meshHealthbar = VerySimpleMesh.Opaque(device, new ChunkRenderMesher.VertexAttributes(vertices, indices));
			//meshHealthbar = MeshHelper.MakeSimplerMesh(device, vertices.ToVertexOpaquePass(), indices);
			//meshHealthbar = new SimpleMesh<VertexCube, int>(device, vertices, indices, DrawHelper.WhitePixel);
		}

		private static RendererDeferred.DrawMaterial healthbarMaterial = new RendererDeferred.DrawMaterial(DrawHelper.WhitePixel);
		public static void DrawHealthbar(GraphicsDevice device, RendererDeferred renderer, Engine.Common.Camera camera, int health, int maxHealth, Vector3 position)
        {
			if (meshHealthbar.IBO == null)
				MakeMeshHealthbar(device);

			renderer.AddOpaqueDraw(new Rendering.RendererDeferred.GBufferDraw(
				healthbarMaterial, meshHealthbar,
				Matrix.CreateScale(new Vector3((float)health / (float)maxHealth, 1, 1)) *
				Matrix.CreateTranslation(new Vector3(-Cube.CUBE_SCALE / 2f, Cube.CUBE_SCALE * 1.5f, 0)) *
				Matrix.CreateRotationX(Math.Clamp(-camera.RotationEuler.X, MathHelper.ToRadians(-15), MathHelper.ToRadians(15))) *
				Matrix.CreateRotationY(-camera.RotationEuler.Y) *
				Matrix.CreateTranslation(position), null));
		}
		
		//Draws a line that is tiled along the vertical axis.
		public static void DrawLineTiled(RendererDeferred renderer, Engine.Common.Camera camera, Vector3 startPosition, Vector3 endPosition, float width, float tileHeight,
			RendererDeferred.DrawMaterial material, VerySimpleMesh mesh, RectangleF sourceRectangle, Color color)
		{
			Vector3 axis = endPosition - startPosition;
			float distance = axis.Length();
			axis.Normalize();

			Matrix mat = Matrix.CreateConstrainedBillboard(startPosition, camera.Position, axis, -camera.Forward, Vector3.Forward);

			int tileTimes = (int)(distance / tileHeight);
			float tileLastBit = distance % tileHeight;

			for (int i = 0; i < tileTimes; i++)
			{
				renderer.AddOpaqueDraw(new Rendering.RendererDeferred.GBufferDraw(material, mesh,
					Matrix.CreateScale(width, tileHeight, width) * mat * Matrix.CreateTranslation(axis * tileHeight * i),
					sourceRectangle, color.ToVector3()));
			}

			//The "last bit" is the part that can't be tiled.
			//In order for this not to be squished, we have to fix the source rectangle.
			//Its height needs to be calculated, and then we need to offset its y position. This is due to the fact that Y is up in world space, but down in texture space.
			float fixedHeight = (tileLastBit / tileHeight) * sourceRectangle.height;
			Vector2 fixedPosition = sourceRectangle.Position;
			fixedPosition.Y += sourceRectangle.height - fixedHeight;

			renderer.AddOpaqueDraw(new Rendering.RendererDeferred.GBufferDraw(material, mesh,
				Matrix.CreateScale(width, tileLastBit, width) * mat * Matrix.CreateTranslation(axis * tileHeight * tileTimes),
				new RectangleF(fixedPosition, sourceRectangle.width, fixedHeight),
				color.ToVector3()));
		}

		//Draws a stretched texture along a line.
		//If you want the texture to be tiled properly, use DrawLineTiled.
		public static void DrawLine(RendererDeferred renderer, Engine.Common.Camera camera, Vector3 startPosition, Vector3 endPosition, float width,
            RendererDeferred.DrawMaterial material, VerySimpleMesh mesh, RectangleF sourceRectangle, Color color)
		{
            Vector3 axis = endPosition - startPosition;
            float distance = axis.Length();
            axis.Normalize();

            Matrix mat = Matrix.CreateConstrainedBillboard(startPosition, camera.Position, axis, -camera.Forward, Vector3.Forward);

            renderer.AddOpaqueDraw(new Rendering.RendererDeferred.GBufferDraw(material, mesh,
                Matrix.CreateScale(width, distance, width) * mat,
                sourceRectangle, color.ToVector3()));
        }

		private static SimpleMesh<VertexPositionTexture, int> axesMesh;
		public static void DrawAxesImmediate(GraphicsDevice device, Vector3 position)
		{
			device.RasterizerState = Main.noCullRS;
			device.DepthStencilState = Main.nodepthDSS;

			if (axesMesh == null)
				axesMesh = MakeAxes(device, Vector3.Zero, new Vector3(5), GlobalState.assetsManager.GetAsset<Texture2D>("axes"));

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
	}
}
