using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Cubes;
using ViMG.VertexDeclarations;

namespace ViMG
{
    public static class MeshHelper
	{
		private static Color[] faceColors = new Color[6]
		{
			Color.Red,
			Color.Green,
			Color.Yellow,
			Color.Blue,
			Color.Pink,
			Color.Purple
		};

		public static (VertexBuffer VBO, IndexBuffer IBO) MakeCubemap(GraphicsDevice device, Vector3 min, Vector3 max)
        {
			List<VertexCube> vertices = new List<VertexCube>();
			List<int> indices = new List<int>();

			Vector3 l_t_f = new Vector3(min.X, min.Y, max.Z);
			Vector3 r_t_f = new Vector3(max.X, min.Y, max.Z);
			Vector3 r_t_n = new Vector3(max.X, min.Y, min.Z);
			Vector3 l_t_n = new Vector3(min.X, min.Y, min.Z);

			Vector3 l_b_n = new Vector3(min.X, max.Y, min.Z);
			Vector3 r_b_n = new Vector3(max.X, max.Y, min.Z);
			Vector3 r_b_f = new Vector3(max.X, max.Y, max.Z);
			Vector3 l_b_f = new Vector3(min.X, max.Y, max.Z);

			indices.Add(0);
			indices.Add(1);
			indices.Add(3);
			indices.Add(1);
			indices.Add(2);
			indices.Add(3);

			vertices.Add(new VertexCube(l_t_n, faceColors[0], new Vector2(1, 1), new Vector3(0, 1, 0)));
			vertices.Add(new VertexCube(r_t_n, faceColors[0], new Vector2(0, 1), new Vector3(0, 1, 0)));
			vertices.Add(new VertexCube(r_t_f, faceColors[0], new Vector2(0, 0), new Vector3(0, 1, 0)));
			vertices.Add(new VertexCube(l_t_f, faceColors[0], new Vector2(1, 0), new Vector3(0, 1, 0)));

			int offset = vertices.Count;
			indices.Add(offset + 0);
			indices.Add(offset + 1);
			indices.Add(offset + 3);
			indices.Add(offset + 1);
			indices.Add(offset + 2);
			indices.Add(offset + 3);

			vertices.Add(new VertexCube(r_t_n, faceColors[1], new Vector2(1, 1), new Vector3(0, 0, 1)));
			vertices.Add(new VertexCube(l_t_n, faceColors[1], new Vector2(0, 1), new Vector3(0, 0, 1)));
			vertices.Add(new VertexCube(l_b_n, faceColors[1], new Vector2(0, 0), new Vector3(0, 0, 1)));
			vertices.Add(new VertexCube(r_b_n, faceColors[1], new Vector2(1, 0), new Vector3(0, 0, 1)));

			offset = vertices.Count;
			indices.Add(offset + 0);
			indices.Add(offset + 1);
			indices.Add(offset + 3);
			indices.Add(offset + 1);
			indices.Add(offset + 2);
			indices.Add(offset + 3);

			vertices.Add(new VertexCube(r_t_f, faceColors[2], new Vector2(1, 1), new Vector3(-1, 0, 0)));
			vertices.Add(new VertexCube(r_t_n, faceColors[2], new Vector2(0, 1), new Vector3(-1, 0, 0)));
			vertices.Add(new VertexCube(r_b_n, faceColors[2], new Vector2(0, 0), new Vector3(-1, 0, 0)));
			vertices.Add(new VertexCube(r_b_f, faceColors[2], new Vector2(1, 0), new Vector3(-1, 0, 0)));

			offset = vertices.Count;
			indices.Add(offset + 0);
			indices.Add(offset + 1);
			indices.Add(offset + 3);
			indices.Add(offset + 1);
			indices.Add(offset + 2);
			indices.Add(offset + 3);

			vertices.Add(new VertexCube(l_t_f, faceColors[3], new Vector2(1, 1), new Vector3(0, 0, -1)));
			vertices.Add(new VertexCube(r_t_f, faceColors[3], new Vector2(0, 1), new Vector3(0, 0, -1)));
			vertices.Add(new VertexCube(r_b_f, faceColors[3], new Vector2(0, 0), new Vector3(0, 0, -1)));
			vertices.Add(new VertexCube(l_b_f, faceColors[3], new Vector2(1, 0), new Vector3(0, 0, -1)));

			offset = vertices.Count;
			indices.Add(offset + 0);
			indices.Add(offset + 1);
			indices.Add(offset + 3);
			indices.Add(offset + 1);
			indices.Add(offset + 2);
			indices.Add(offset + 3);

			vertices.Add(new VertexCube(l_t_n, faceColors[4], new Vector2(1, 1), new Vector3(1, 0, 0)));
			vertices.Add(new VertexCube(l_t_f, faceColors[4], new Vector2(0, 1), new Vector3(1, 0, 0)));
			vertices.Add(new VertexCube(l_b_f, faceColors[4], new Vector2(0, 0), new Vector3(1, 0, 0)));
			vertices.Add(new VertexCube(l_b_n, faceColors[4], new Vector2(1, 0), new Vector3(1, 0, 0)));

			offset = vertices.Count;
			indices.Add(offset + 0);
			indices.Add(offset + 1);
			indices.Add(offset + 3);
			indices.Add(offset + 1);
			indices.Add(offset + 2);
			indices.Add(offset + 3);

			vertices.Add(new VertexCube(l_b_f, faceColors[5], new Vector2(1, 1), new Vector3(0, -1, 0)));
			vertices.Add(new VertexCube(r_b_f, faceColors[5], new Vector2(0, 1), new Vector3(0, -1, 0)));
			vertices.Add(new VertexCube(r_b_n, faceColors[5], new Vector2(0, 0), new Vector3(0, -1, 0)));
			vertices.Add(new VertexCube(l_b_n, faceColors[5], new Vector2(1, 0), new Vector3(0, -1, 0)));

			return MakeSimplerMesh(device, vertices.ToVertexOpaquePass(), indices);
		}

		public static (VertexBuffer VBO, IndexBuffer IBO) MakeSimplerMesh<TVertex, TIndex>(GraphicsDevice device, (List<TVertex> vertices, List<TIndex> indices) tuple)
			where TVertex : struct, IVertexType
			where TIndex : struct
		{
			return MakeSimplerMesh(device, tuple.vertices, tuple.indices);
        }

		public static (VertexBuffer VBO, IndexBuffer IBO) MakeSimplerMesh<TVertex, TIndex>(GraphicsDevice device, List<TVertex> vertices, List<TIndex> indices) 
			where TVertex : struct, IVertexType
			where TIndex : struct
        {
			if (vertices.Count == 0)
				return (null, null);

			VertexBuffer VBO = new VertexBuffer(device, typeof(TVertex), vertices.Count, BufferUsage.WriteOnly);
			IndexBuffer IBO = new IndexBuffer(device, typeof(TIndex), indices.Count, BufferUsage.WriteOnly);

			VBO.SetData(vertices.ToArray());
			IBO.SetData(indices.ToArray());

			return (VBO, IBO);
        }

		public static (VertexBuffer VBO, IndexBuffer IBO) MakeCenteredQuad(GraphicsDevice device, float width, float height)
		{
            Vector3 min = -new Vector3(width / 2f, height / 2f, 0);
            Vector3 max = new Vector3(width / 2f, height / 2f, 0);

            Vector3 a = new Vector3(max.X, min.Y, max.Z);
            Vector3 b = new Vector3(min.X, min.Y, max.Z);
            Vector3 c = new Vector3(min.X, max.Y, max.Z);
            Vector3 d = new Vector3(max.X, max.Y, max.Z);

            List<VertexCube> vertices = new List<VertexCube>();
            List<int> indices = new List<int>();

            Vector2 atx = new Vector2(1, 1);
            Vector2 btx = new Vector2(0, 1);
            Vector2 ctx = new Vector2(0, 0);
            Vector2 dtx = new Vector2(1, 0);

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

            return MeshHelper.MakeSimplerMesh(device, vertices.ToVertexOpaquePass(), indices);
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

			Vector2 atx = new Vector2(1, 1);
			Vector2 btx = new Vector2(0, 1);
			Vector2 ctx = new Vector2(0, 0);
			Vector2 dtx = new Vector2(1, 0);

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

            return MeshHelper.MakeSimplerMesh(device, vertices.ToVertexOpaquePass(), indices);
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
			SIDES = LEFT | RIGHT | FRONT | BACK,
			ALL = LEFT | RIGHT | UP | DOWN | FRONT | BACK
		}

		public static bool HasFlagFast(this CubeFace face, CubeFace hasFace)
		{
			return (face & hasFace) > 0;
		}

		public static CubeFace RandomHorizontalFace(this Random random)
        {
			int val = random.Next(0, 4);

			switch (val)
            {
				case 0:
					return CubeFace.LEFT;
				case 1:
					return CubeFace.FRONT;
				case 2:
					return CubeFace.RIGHT;
				case 3:
					return CubeFace.BACK;
				default: return CubeFace.NONE;
            }
        }

		/*public static SimpleMesh<VertexCube, int> MakeCubeVertexPositionColorTextureNormal(GraphicsDevice device, Vector3 min, Vector3 max, CubeFace faces, Color color, Texture2D texture)
		{
			List<VertexCube> vertices = new List<VertexCube>();
			List<int> indices = new List<int>();

			MakeCubeVertsVertexPositionColorTextureNormal(min, max, faces, color, vertices, indices);

			if (texture == null)
				return new SimpleMesh<VertexCube, int>(device, vertices, indices);
			else return new SimpleMesh<VertexCube, int>(device, vertices, indices, texture);
		}*/

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

		public static void MakeQuadVertsVertexPositionColorTextureNormal(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal, Color color, List<VertexCube> vertices, List<int> indices, RectangleF? sourceRect = null, Point? textureSize = null)
		{
			int offset = vertices.Count;
			indices.Add(offset + 0);
			indices.Add(offset + 1);
			indices.Add(offset + 3);
			indices.Add(offset + 1);
			indices.Add(offset + 2);
			indices.Add(offset + 3);

			if (sourceRect == null)
			{
				vertices.Add(new VertexCube(a, color, new Vector2(0, 0), normal));
				vertices.Add(new VertexCube(b, color, new Vector2(1, 0), normal));
				vertices.Add(new VertexCube(c, color, new Vector2(1, 1), normal));
				vertices.Add(new VertexCube(d, color, new Vector2(0, 1), normal));
			}
			else
			{
				float minX = sourceRect.Value.x / textureSize.Value.X;
				float maxX = (sourceRect.Value.x + sourceRect.Value.width) / textureSize.Value.X;
				float minY = sourceRect.Value.y / textureSize.Value.X;
				float maxY = (sourceRect.Value.y + sourceRect.Value.height) / textureSize.Value.X;

				vertices.Add(new VertexCube(a, color, new Vector2(minX, maxY), normal));
				vertices.Add(new VertexCube(b, color, new Vector2(minX, minY), normal));
				vertices.Add(new VertexCube(c, color, new Vector2(maxX, minY), normal));
				vertices.Add(new VertexCube(d, color, new Vector2(maxX, maxY), normal));
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
