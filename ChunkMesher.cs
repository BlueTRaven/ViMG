using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Cubes;

namespace ViMG
{
	public class ChunkMesher
	{
		private GraphicsDevice device;

		public ChunkMesher(GraphicsDevice device)
		{
			this.device = device;
		}

		public ChunkMesh GenerateChunk(CubeRegistry registry, Chunk chunk)
		{
			Vector3 n = new Vector3(0);
			Vector3 f = new Vector3(Cube.CUBE_SCALE);

			List<VertexPositionColorTextureNormal> vertices = new List<VertexPositionColorTextureNormal>();
			List<int> indices = new List<int>();

			ChunkData.ChunkUpdate = 0;

			for (int x = 0; x < Chunk.CHUNK_SIZE; x++)
			{
				for (int y = 0; y < Chunk.CHUNK_SIZE; y++)
				{
					for (int z = 0; z < Chunk.CHUNK_SIZE; z++)
					{
						var pos = new CubePosition(x, y, z, CubePosition.CoordinateSpace.ChunkSpace);
						pos = pos.InCubeSpace(chunk);

						Cube.CubeInstance instance = chunk.GetData().GetCube(pos);
						Cube.CubeVisualInstance visual = chunk.GetData().GetVisual(pos);

						if (instance.cubeId == 0 || visual.clearSides == MeshHelper.CubeFace.NONE)
							continue;

						Cube cube = registry.Get(instance.cubeId);

						bool ok = false;
						MakeCubeVerts(n + pos.InWorldSpace(out ok), f + pos.InWorldSpace(out ok), visual, cube, vertices, indices);
					}
				}
			}

			if (vertices.Count > 0 && indices.Count > 0)
			{
				var mesh = new ChunkMesh(device, vertices, indices);
				return mesh;
			}
			else return ChunkMesh.Empty;
			//meshes[chunk.Position.X, chunk.Position.Y, chunk.Position.Z] = mesh;
		}

		public static void MakeCubeVerts(Vector3 min, Vector3 max, Cube.CubeVisualInstance visual, Cube cube, List<VertexPositionColorTextureNormal> vertices, List<int> indices)
		{
			Vector3 l_t_n = new Vector3(min.X, min.Y, min.Z);
			Vector3 r_t_n = new Vector3(max.X, min.Y, min.Z);
			Vector3 r_b_n = new Vector3(max.X, max.Y, min.Z);
			Vector3 l_b_n = new Vector3(min.X, max.Y, min.Z);
			Vector3 l_t_f = new Vector3(min.X, min.Y, max.Z);
			Vector3 r_t_f = new Vector3(max.X, min.Y, max.Z);
			Vector3 r_b_f = new Vector3(max.X, max.Y, max.Z);
			Vector3 l_b_f = new Vector3(min.X, max.Y, max.Z);

			if (visual.clearSides.Has(MeshHelper.CubeFace.FRONT))
				MakeQuadVerts(l_t_n, r_t_n, r_b_n, l_b_n, new Vector3(0, 0, -1), MeshHelper.CubeFace.FRONT, cube, vertices, indices);

			if (visual.clearSides.Has(MeshHelper.CubeFace.RIGHT))
				MakeQuadVerts(r_t_n, r_t_f, r_b_f, r_b_n, new Vector3(1, 0, 0), MeshHelper.CubeFace.RIGHT, cube, vertices, indices);

			if (visual.clearSides.Has(MeshHelper.CubeFace.BACK))
				MakeQuadVerts(r_t_f, l_t_f, l_b_f, r_b_f, new Vector3(0, 0, 1), MeshHelper.CubeFace.BACK, cube, vertices, indices);

			if (visual.clearSides.Has(MeshHelper.CubeFace.LEFT))
				MakeQuadVerts(l_t_f, l_t_n, l_b_n, l_b_f, new Vector3(-1, 0, 0), MeshHelper.CubeFace.LEFT, cube, vertices, indices);

			if (visual.clearSides.Has(MeshHelper.CubeFace.DOWN))
				MakeQuadVerts(l_t_f, r_t_f, r_t_n, l_t_n, new Vector3(0, -1, 0), MeshHelper.CubeFace.DOWN, cube, vertices, indices);

			if (visual.clearSides.Has(MeshHelper.CubeFace.UP))
				MakeQuadVerts(r_b_f, l_b_f, l_b_n, r_b_n, new Vector3(0, 1, 0), MeshHelper.CubeFace.UP, cube, vertices, indices);
		}

		public static void MakeQuadVerts(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal, MeshHelper.CubeFace face, Cube cube, 
			List<VertexPositionColorTextureNormal> vertices, List<int> indices)
		{
			int offset = vertices.Count;
			indices.Add(offset + 0);
			indices.Add(offset + 1);
			indices.Add(offset + 3);
			indices.Add(offset + 1);
			indices.Add(offset + 2);
			indices.Add(offset + 3);

			const int textureWidth = 1024;
			const int textureHeight = 1024;

			const float cubeSideWidth = 1f / textureWidth;
			const float cubeSideHeight = 1f / textureHeight;

			RectangleF sourceRect = cube.GetSourceRect(face);

			Vector2 uvNear = new Vector2(sourceRect.x * cubeSideWidth, sourceRect.y * cubeSideHeight);
			Vector2 uvFar = new Vector2((sourceRect.x + sourceRect.width) * cubeSideWidth, (sourceRect.y + sourceRect.height) * cubeSideHeight);

			vertices.Add(new VertexPositionColorTextureNormal(a, cube.GetTintColor(), new Vector2(uvFar.X, uvFar.Y), normal));
			vertices.Add(new VertexPositionColorTextureNormal(b, cube.GetTintColor(), new Vector2(uvNear.X, uvFar.Y), normal));
			vertices.Add(new VertexPositionColorTextureNormal(c, cube.GetTintColor(), new Vector2(uvNear.X, uvNear.Y), normal));
			vertices.Add(new VertexPositionColorTextureNormal(d, cube.GetTintColor(), new Vector2(uvFar.X, uvNear.Y), normal));
		}
	}
}
