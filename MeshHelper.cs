using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Cubes;

namespace ViMG
{
	public static class MeshHelper
	{
		public static (VertexBuffer VBO, IndexBuffer IBO) MakeSimplerMesh<TVertex, TIndex>(GraphicsDevice device, List<TVertex> vertices, List<TIndex> indices) 
			where TVertex : struct 
			where TIndex : struct
        {
			VertexBuffer VBO = new VertexBuffer(device, typeof(TVertex), vertices.Count, BufferUsage.WriteOnly);
			IndexBuffer IBO = new IndexBuffer(device, typeof(TIndex), indices.Count, BufferUsage.WriteOnly);

			VBO.SetData(vertices.ToArray());
			IBO.SetData(indices.ToArray());

			return (VBO, IBO);
        }

		public static (VertexBuffer VBO, IndexBuffer IBO) MakeEnemyQuad(GraphicsDevice device, float width, float height)
        {
			Vector3 min = -new Vector3(width / 2f, 0, 0);
			Vector3 max = new Vector3(width / 2f, height, 0);

			Vector3 a = new Vector3(max.X, min.Y, max.Z);
			Vector3 b = new Vector3(min.X, min.Y, max.Z);
			Vector3 c = new Vector3(min.X, max.Y, max.Z);
			Vector3 d = new Vector3(max.X, max.Y, max.Z);

			List<VertexCube> vertices = new List<VertexCube>();
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

			vertices.Add(new VertexCube(a, Color.White, atx, new Vector3(0, 0, 1)));
			vertices.Add(new VertexCube(b, Color.White, btx, new Vector3(0, 0, 1)));
			vertices.Add(new VertexCube(c, Color.White, ctx, new Vector3(0, 0, 1)));
			vertices.Add(new VertexCube(d, Color.White, dtx, new Vector3(0, 0, 1)));

			offset = vertices.Count;
			indices.Add(offset + 0);
			indices.Add(offset + 1);
			indices.Add(offset + 3);
			indices.Add(offset + 1);
			indices.Add(offset + 2);
			indices.Add(offset + 3);

			vertices.Add(new VertexCube(b, Color.White, btx, new Vector3(0, 0, -1)));
			vertices.Add(new VertexCube(a, Color.White, atx, new Vector3(0, 0, -1)));
			vertices.Add(new VertexCube(d, Color.White, dtx, new Vector3(0, 0, -1)));
			vertices.Add(new VertexCube(c, Color.White, ctx, new Vector3(0, 0, -1)));

			VertexBuffer vbo = new VertexBuffer(device, typeof(VertexCube), vertices.Count, BufferUsage.WriteOnly);
			IndexBuffer ibo = new IndexBuffer(device, typeof(int), indices.Count, BufferUsage.WriteOnly);

			vbo.SetData(vertices.ToArray());
			ibo.SetData(indices.ToArray());

			return (vbo, ibo);
		}

		[Flags]
		public enum CubeFace : byte
		{
			NONE = 0,
			LEFT = 1 << 0,
			RIGHT = 1 << 1,
			UP = 1 << 2,
			DOWN = 1 << 3,
			FRONT = 1 << 4,
			BACK = 1 << 5,
			ALL = LEFT | RIGHT | UP | DOWN | FRONT | BACK
		}

		public static SimpleMesh<VertexCube, int> MakeCubeVertexPositionColorTextureNormal(GraphicsDevice device, Vector3 min, Vector3 max, CubeFace faces, Color color, Texture2D texture)
		{
			List<VertexCube> vertices = new List<VertexCube>();
			List<int> indices = new List<int>();

			MakeCubeVertsVertexPositionColorTextureNormal(min, max, faces, color, vertices, indices);

			if (texture == null)
				return new SimpleMesh<VertexCube, int>(device, vertices, indices);
			else return new SimpleMesh<VertexCube, int>(device, vertices, indices, texture);
		}

		public static void MakeCubeVertsVertexPositionColorTextureNormal(Vector3 min, Vector3 max, CubeFace faces, Color color, List<VertexCube> vertices, List<int> indices)
		{
			Vector3 l_t_n = new Vector3(min.X, min.Y, min.Z);
			Vector3 r_t_n = new Vector3(max.X, min.Y, min.Z);
			Vector3 r_b_n = new Vector3(max.X, max.Y, min.Z);
			Vector3 l_b_n = new Vector3(min.X, max.Y, min.Z);
			Vector3 l_t_f = new Vector3(min.X, min.Y, max.Z);
			Vector3 r_t_f = new Vector3(max.X, min.Y, max.Z);
			Vector3 r_b_f = new Vector3(max.X, max.Y, max.Z);
			Vector3 l_b_f = new Vector3(min.X, max.Y, max.Z);

			if (faces.Has(CubeFace.BACK))
				MakeQuadVertsVertexPositionColorTextureNormal(l_t_n, r_t_n, r_b_n, l_b_n, new Vector3(0, 0, 1), color, vertices, indices);

			if (faces.Has(CubeFace.LEFT))
				MakeQuadVertsVertexPositionColorTextureNormal(r_t_n, r_t_f, r_b_f, r_b_n, new Vector3(-1, 0, 0), color, vertices, indices);

			if (faces.Has(CubeFace.FRONT))
				MakeQuadVertsVertexPositionColorTextureNormal(r_t_f, l_t_f, l_b_f, r_b_f, new Vector3(0, 0, -1), color, vertices, indices);

			if (faces.Has(CubeFace.RIGHT))
				MakeQuadVertsVertexPositionColorTextureNormal(l_t_f, l_t_n, l_b_n, l_b_f, new Vector3(1, 0, 0), color, vertices, indices);

			if (faces.Has(CubeFace.UP))
				MakeQuadVertsVertexPositionColorTextureNormal(l_t_f, r_t_f, r_t_n, l_t_n, new Vector3(0, 1, 0), color, vertices, indices);

			if (faces.Has(CubeFace.DOWN))
				MakeQuadVertsVertexPositionColorTextureNormal(r_b_f, l_b_f, l_b_n, r_b_n, new Vector3(0, -1, 0), color, vertices, indices);
		}

		public static void MakeQuadVertsVertexPositionColorTextureNormal(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal, Color color, List<VertexCube> vertices, List<int> indices, int textureX = -1, int textureY = -1, int textureWidth = -1, int textureHeight = -1)
		{
			int offset = vertices.Count;
			indices.Add(offset + 0);
			indices.Add(offset + 1);
			indices.Add(offset + 3);
			indices.Add(offset + 1);
			indices.Add(offset + 2);
			indices.Add(offset + 3);

			if (textureX == -1)
			{
				vertices.Add(new VertexCube(a, color, new Vector2(0, 0), normal));
				vertices.Add(new VertexCube(b, color, new Vector2(1, 0), normal));
				vertices.Add(new VertexCube(c, color, new Vector2(1, 1), normal));
				vertices.Add(new VertexCube(d, color, new Vector2(0, 1), normal));
			}
			else
			{
				vertices.Add(new VertexCube(a, color, new Vector2(0, 0), normal));
				vertices.Add(new VertexCube(b, color, new Vector2(1, 0), normal));
				vertices.Add(new VertexCube(c, color, new Vector2(1, 1), normal));
				vertices.Add(new VertexCube(d, color, new Vector2(0, 1), normal));
			}
		}

		public static SimpleMesh<VertexPositionColor, int> MakeCubeVertexPositionColor(GraphicsDevice device, Vector3 min, Vector3 max, CubeFace faces, Color color, Texture2D texture)
		{
			List<VertexPositionColor> vertices = new List<VertexPositionColor>();
			List<int> indices = new List<int>();

			MakeCubeVertsVertexPositionColor(min, max, faces, color, vertices, indices);

			if (texture == null)
				return new SimpleMesh<VertexPositionColor, int>(device, vertices, indices);
			else return new SimpleMesh<VertexPositionColor, int>(device, vertices, indices, texture);
		}

		public static void MakeCubeVertsVertexPositionColor(Vector3 min, Vector3 max, CubeFace faces, Color color, List<VertexPositionColor> vertices, List<int> indices)
		{
			Vector3 l_t_n = new Vector3(min.X, min.Y, min.Z);
			Vector3 r_t_n = new Vector3(max.X, min.Y, min.Z);
			Vector3 r_b_n = new Vector3(max.X, max.Y, min.Z);
			Vector3 l_b_n = new Vector3(min.X, max.Y, min.Z);
			Vector3 l_t_f = new Vector3(min.X, min.Y, max.Z);
			Vector3 r_t_f = new Vector3(max.X, min.Y, max.Z);
			Vector3 r_b_f = new Vector3(max.X, max.Y, max.Z);
			Vector3 l_b_f = new Vector3(min.X, max.Y, max.Z);

			if (faces.Has(CubeFace.BACK))
				MakeQuadVertsVertexPositionColor(l_t_n, r_t_n, r_b_n, l_b_n, color, vertices, indices);

			if (faces.Has(CubeFace.LEFT))
				MakeQuadVertsVertexPositionColor(r_t_n, r_t_f, r_b_f, r_b_n, color, vertices, indices);

			if (faces.Has(CubeFace.FRONT))
				MakeQuadVertsVertexPositionColor(r_t_f, l_t_f, l_b_f, r_b_f, color, vertices, indices);

			if (faces.Has(CubeFace.RIGHT))
				MakeQuadVertsVertexPositionColor(l_t_f, l_t_n, l_b_n, l_b_f, color, vertices, indices);

			if (faces.Has(CubeFace.UP))
				MakeQuadVertsVertexPositionColor(l_t_f, r_t_f, r_t_n, l_t_n, color, vertices, indices);

			if (faces.Has(CubeFace.DOWN))
				MakeQuadVertsVertexPositionColor(r_b_f, l_b_f, l_b_n, r_b_n, color, vertices, indices);
		}

		public static void MakeQuadVertsVertexPositionColor(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color color, List<VertexPositionColor> vertices, List<int> indices)
		{
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
		}

		public static void MakeQuadVertsVertexPositionTexture(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector2 atx, Vector2 btx, Vector2 ctx, Vector2 dtx, 
			List<VertexPositionTexture> vertices, List<int> indices)
		{
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
		}
	}
}
