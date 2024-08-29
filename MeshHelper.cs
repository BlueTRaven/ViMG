using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SharpDX.Direct2D1.Effects;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security;
using System.Text;
using ViMG.Cubes;
using ViMG.Rendering;
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
			FastList<VertexCube> vertices = new FastList<VertexCube>();
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

			int offset = vertices.Length;
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

			offset = vertices.Length;
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

			offset = vertices.Length;
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

			offset = vertices.Length;
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

			offset = vertices.Length;
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

		public static (VertexBuffer VBO, IndexBuffer IBO) MakeSimplerMesh<TVertex, TIndex>(GraphicsDevice device, (FastList<TVertex> vertices, List<TIndex> indices) tuple, bool bakeTangents = true)
			where TVertex : struct, IVertexType, IVertexDeclGetters
			where TIndex : struct
		{
			return MakeSimplerMesh(device, tuple.vertices, tuple.indices, bakeTangents);
        }

		public static (VertexBuffer VBO, IndexBuffer IBO) MakeSimplerMesh<TVertex, TIndex>(GraphicsDevice device, FastList<TVertex> vertices, List<TIndex> indices, bool bakeTangents = true) 
			where TVertex : struct, IVertexType, IVertexDeclGetters
            where TIndex : struct
        {
			if (vertices.Length == 0)
				return (null, null);

			if (bakeTangents)
				BakeTangents(0, vertices.Length, vertices);

			VertexBuffer VBO = new VertexBuffer(device, typeof(TVertex), vertices.Length, BufferUsage.WriteOnly);
			IndexBuffer IBO = new IndexBuffer(device, typeof(TIndex), indices.Count, BufferUsage.WriteOnly);

			VBO.SetData(vertices.Buffer, 0, vertices.Length);
			IBO.SetData(indices.ToArray());

			return (VBO, IBO);
        }

        public static unsafe void BakeTangents<T>(int start, int end, FastList<T> vertices)
			where T : struct, IVertexDeclGetters
        {
            using var zone = TracyImpl.Tracy.BeginZone();

            Debug.Assert((end - start) % 4 == 0);

				for (int i = start; i < end; i += 4)
				{
					T vert1 = vertices[i + 0];
					T vert2 = vertices[i + 1];
					T vert3 = vertices[i + 2];
					T vert4 = vertices[i + 3];

					Vector3 edge1 = vert2.GetPosition() - vert1.GetPosition();
					Vector3 edge2 = vert3.GetPosition() - vert1.GetPosition();
					Vector2 dUV1 = vert2.GetUV() - vert1.GetUV();
					Vector2 dUV2 = vert3.GetUV() - vert1.GetUV();

					float f = 1 / (dUV1.X * dUV2.Y - dUV2.X * dUV1.Y);

					Vector3 tangent = vert1.GetPosition() - vert2.GetPosition();
					/*Vector3 tangent = new Vector3(
						f * (dUV2.Y * edge1.X - dUV1.Y * edge2.X),
						f * (dUV2.Y * edge1.Y - dUV1.Y * edge2.Y),
						f * (dUV2.Y * edge1.Z - dUV1.Y * edge2.Z)
						);*/

					Vector3 bitangent = Vector3.Cross(vert1.GetNormal(), tangent);
					/*Vector3 bitangent = new Vector3(
						f * (-dUV2.X * edge1.X + dUV1.X * edge2.X),
						f * (-dUV2.X * edge1.Y + dUV1.X * edge2.Y),
						f * (-dUV2.X * edge1.Z + dUV1.X * edge2.Z)
						);*/

					vert1.SetTangent(tangent, bitangent);
					vert2.SetTangent(tangent, bitangent);
					vert3.SetTangent(tangent, bitangent);
					vert4.SetTangent(tangent, bitangent);

					vertices.Buffer[i + 0] = vert1;
					vertices.Buffer[i + 1] = vert2;
					vertices.Buffer[i + 2] = vert3;
					vertices.Buffer[i + 3] = vert4;
			}
        }

        public static VerySimpleMesh MakeUVSphere(GraphicsDevice device, float radius, bool flip = false)
        {
            FastList<VertexCube> vertices = new FastList<VertexCube>();
            List<int> indices = new List<int>();

            MakeUVSphereRaw(vertices, indices, Vector3.Zero, new RectangleF(0, 0, 1, 1), radius, 16, 16, flip);

			return VerySimpleMesh.Opaque(device, new ChunkRenderMesher.VertexAttributes(vertices, indices));
            //return MeshHelper.MakeSimplerMesh(device, vertices.ToVertexOpaquePass(), indices);
        }

        public static void MakeUVSphereRaw(FastList<VertexCube> vertices, List<int> indices, Vector3 position, RectangleF sourceRect, float radius, int stacks = 16, int slices = 16, bool flip = false, int vertexOffset = 0)
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

                    int indicesStart = vertices.Length + vertexOffset;

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

        public static VerySimpleMesh MakeQuad(GraphicsDevice device, float width, float height, Enums.Alignment alignment)
		{
			Vector2 min;
			Vector2 max;

			switch (alignment)
			{
				case Enums.Alignment.TopLeft:
					min = new Vector2();
					max = new Vector2(width, height);
					break;
				case Enums.Alignment.Top:
					min = new Vector2(-width / 2f, 0);
					max = new Vector2(width / 2, height);
					break;
				case Enums.Alignment.TopRight:
                    min = new Vector2(-width, 0);
                    max = new Vector2(0, height);
					break;
				case Enums.Alignment.Left:
					min = new Vector2(0, -height / 2f);
					max = new Vector2(width, -height / 2f);
					break;
				case Enums.Alignment.Center:
                    min = new Vector2(-width / 2f, -height / 2f);
                    max = new Vector2(width / 2f, height / 2f);
					break;
				case Enums.Alignment.Right:
                    min = new Vector2(-width, -height / 2f);
                    max = new Vector2(0, height / 2f);
					break;
				case Enums.Alignment.BottomLeft:
                    min = new Vector2(0, -height);
                    max = new Vector2(width, 0);
					break;
				case Enums.Alignment.Bottom:
                    min = new Vector2(-width / 2f, -height);
                    max = new Vector2(width / 2f, 0);
                    break;
				case Enums.Alignment.BottomRight:
                    min = new Vector2(-width, -height);
                    max = new Vector2(0, 0);
                    break;
				default: throw new Exception("???");
            }

            Vector3 a = new Vector3(max.X, min.Y, 0);
            Vector3 b = new Vector3(min.X, min.Y, 0);
            Vector3 c = new Vector3(min.X, max.Y, 0);
            Vector3 d = new Vector3(max.X, max.Y, 0);

            FastList<VertexCube> vertices = new FastList<VertexCube>(8);
            List<int> indices = new List<int>();

            Vector2 atx = new Vector2(1, 1);
            Vector2 btx = new Vector2(0, 1);
            Vector2 ctx = new Vector2(0, 0);
            Vector2 dtx = new Vector2(1, 0);

            int offset = vertices.Length;
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

            offset = vertices.Length;
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

			return VerySimpleMesh.Opaque(device, new ChunkRenderMesher.VertexAttributes(vertices, indices), true);// MeshHelper.MakeSimplerMesh(device, vertices.ToVertexOpaquePass(), indices);
        }

		// TODO: both MakeCenteredQuad and MakeEnemyQuad can be rewritten to use Enums.Alignment.
		// Produces a quad with half extents in xy.
        public static (VertexBuffer VBO, IndexBuffer IBO) MakeCenteredQuad(GraphicsDevice device, float width, float height)
		{
            Vector3 min = -new Vector3(width / 2f, height / 2f, 0);
            Vector3 max = new Vector3(width / 2f, height / 2f, 0);

            Vector3 a = new Vector3(max.X, min.Y, max.Z);
            Vector3 b = new Vector3(min.X, min.Y, max.Z);
            Vector3 c = new Vector3(min.X, max.Y, max.Z);
            Vector3 d = new Vector3(max.X, max.Y, max.Z);

            FastList<VertexCube> vertices = new FastList<VertexCube>(8);
            List<int> indices = new List<int>();

            Vector2 atx = new Vector2(1, 1);
            Vector2 btx = new Vector2(0, 1);
            Vector2 ctx = new Vector2(0, 0);
            Vector2 dtx = new Vector2(1, 0);

            int offset = vertices.Length;
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

            offset = vertices.Length;
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

		// Produces a quad that is anchored on the bottom.
		public static (VertexBuffer VBO, IndexBuffer IBO) MakeEnemyQuad(GraphicsDevice device, float width, float height)
        {
			Vector3 min = -new Vector3(width / 2f, 0, 0);
			Vector3 max = new Vector3(width / 2f, height, 0);

			Vector3 a = new Vector3(max.X, min.Y, max.Z);
			Vector3 b = new Vector3(min.X, min.Y, max.Z);
			Vector3 c = new Vector3(min.X, max.Y, max.Z);
			Vector3 d = new Vector3(max.X, max.Y, max.Z);

			FastList<VertexCube> vertices = new FastList<VertexCube>();
			List<int> indices = new List<int>();

			Vector2 atx = new Vector2(1, 1);
			Vector2 btx = new Vector2(0, 1);
			Vector2 ctx = new Vector2(0, 0);
			Vector2 dtx = new Vector2(1, 0);

			int offset = vertices.Length;
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

			offset = vertices.Length;
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

		public static void MakeXMeshVerts(FastList<VertexCube> vertices, List<int> indices, Vector3 pos, Vector3 scale, RectangleF sourceRect, int vertexOffset = 0)
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

			Vector3 crossabg = -Vector3.Cross(Vector3.Normalize(b - g), Vector3.Normalize(b - a));
			Vector3 crossefc = -Vector3.Cross(Vector3.Normalize(f - c), Vector3.Normalize(f - e));

            int offset = vertices.Length + vertexOffset;

            vertices.Add(new VertexCube(a, Color.White, new Vector2(sourceRect.x, sourceRect.y + sourceRect.height),					crossabg));
            vertices.Add(new VertexCube(b, Color.White, new Vector2(sourceRect.x, sourceRect.y),										crossabg));
            vertices.Add(new VertexCube(c, Color.White, new Vector2(sourceRect.x + sourceRect.width, sourceRect.y),						crossefc));
            vertices.Add(new VertexCube(d, Color.White, new Vector2(sourceRect.x + sourceRect.width, sourceRect.y + sourceRect.height), crossefc));
																																		
            vertices.Add(new VertexCube(e, Color.White, new Vector2(sourceRect.x, sourceRect.y + sourceRect.height),					crossefc));
            vertices.Add(new VertexCube(f, Color.White, new Vector2(sourceRect.x, sourceRect.y),										crossefc));
            vertices.Add(new VertexCube(g, Color.White, new Vector2(sourceRect.x + sourceRect.width, sourceRect.y),						crossabg));
            vertices.Add(new VertexCube(h, Color.White, new Vector2(sourceRect.x + sourceRect.width, sourceRect.y + sourceRect.height), crossabg));

            indices.Add(offset + 0);	//a
            indices.Add(offset + 1);	//b
            indices.Add(offset + 6);	//g
            indices.Add(offset + 6);	//g
            indices.Add(offset + 7);	//h
            indices.Add(offset + 0);	//a

            indices.Add(offset + 4);	//e
            indices.Add(offset + 5);	//f
            indices.Add(offset + 2);	//c
            indices.Add(offset + 2);	//c
            indices.Add(offset + 3);	//d
            indices.Add(offset + 4);	//e

            offset = vertices.Length + vertexOffset;

            vertices.Add(new VertexCube(a, Color.White, new Vector2(sourceRect.x, sourceRect.y + sourceRect.height),					-crossabg));
            vertices.Add(new VertexCube(b, Color.White, new Vector2(sourceRect.x, sourceRect.y),										-crossabg));
            vertices.Add(new VertexCube(c, Color.White, new Vector2(sourceRect.x + sourceRect.width, sourceRect.y),						-crossefc));
            vertices.Add(new VertexCube(d, Color.White, new Vector2(sourceRect.x + sourceRect.width, sourceRect.y + sourceRect.height), -crossefc));
																																		
            vertices.Add(new VertexCube(e, Color.White, new Vector2(sourceRect.x, sourceRect.y + sourceRect.height),					-crossefc));
            vertices.Add(new VertexCube(f, Color.White, new Vector2(sourceRect.x, sourceRect.y),										-crossefc));
            vertices.Add(new VertexCube(g, Color.White, new Vector2(sourceRect.x + sourceRect.width, sourceRect.y),						-crossabg));
            vertices.Add(new VertexCube(h, Color.White, new Vector2(sourceRect.x + sourceRect.width, sourceRect.y + sourceRect.height), -crossabg));

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
        }

		public static void MakeCubeVertsVertexPositionColorTextureNormal(Vector3 min, Vector3 max, CubeFace faces, Color color, FastList<VertexCube> vertices, List<int> indices)
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

		public static void MakeQuadVertsVertexPositionColorTextureNormal(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal, Color color, FastList<VertexCube> vertices, List<int> indices, RectangleF? sourceRect = null, Point? textureSize = null)
		{
			int offset = vertices.Length;
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
