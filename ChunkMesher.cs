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

		public ChunkMesh GenerateChunk(Chunk chunk, World world)
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

						Cube.CubeInstance instance = chunk.GetData().GetCubeInstance(pos);
						Cube.CubeVisualInstance visual = chunk.GetData().GetVisual(pos);

						if (instance.cubeId == 0 || visual.clearSides == MeshHelper.CubeFace.NONE)
							continue;

						Cube cube = Main.Registry.CubeRegistry.Get(instance.cubeId);

						int oldCount = vertices.Count;

						bool ok = false;
						MakeCubeVerts(n + pos.InWorldSpace(out ok), f + pos.InWorldSpace(out ok), visual, cube, vertices, indices);

						int count = vertices.Count - oldCount;

						for (int i = oldCount; i < oldCount + count; i++)
						{
							VertexPositionColorTextureNormal vertex = vertices[i];

							//pos =
							CubePosition cubePos = pos.InChunkSpace(chunk);
							//pc =
							CubePosition vertCubePos = CubePosition.FromWorldSpace(vertex.Position).InChunkSpace(chunk);

							CubePosition nrm = new CubePosition(cubePos.X + (int)vertex.Normal.X,
								cubePos.Y + (int)vertex.Normal.Y,
								cubePos.Z + (int)vertex.Normal.Z, CubePosition.CoordinateSpace.ChunkSpace);

							CubePosition t = new CubePosition();
							CubePosition bt = new CubePosition();

							int sX = vertCubePos.X == cubePos.X ? -1 : 1;
							int sY = vertCubePos.Y == cubePos.Y ? -1 : 1;
							int sZ = vertCubePos.Z == cubePos.Z ? -1 : 1;

							if (vertex.Normal.X != 0)
							{
								t = new CubePosition(0, sY, 0, CubePosition.CoordinateSpace.ChunkSpace);
								bt = new CubePosition(0, 0, sZ, CubePosition.CoordinateSpace.ChunkSpace);
							}
							else if (vertex.Normal.Y != 0)
							{
								t = new CubePosition(sX, 0, 0, CubePosition.CoordinateSpace.ChunkSpace);
								bt = new CubePosition(0, 0, sZ, CubePosition.CoordinateSpace.ChunkSpace);
							}
							else if (vertex.Normal.Z != 0)
							{
								t = new CubePosition(sX, 0, 0, CubePosition.CoordinateSpace.ChunkSpace);
								bt = new CubePosition(0, sY, 0, CubePosition.CoordinateSpace.ChunkSpace);
							}

							int top = chunk.GetData().GetRawOrAdjacent(nrm, world);
							int corner = chunk.GetData().GetRawOrAdjacent(nrm + t + bt, world);
							int sideA = chunk.GetData().GetRawOrAdjacent(nrm + t, world);
							int sideB = chunk.GetData().GetRawOrAdjacent(nrm + bt, world);

							if (top > 0)
							{
								//nothing
							}
							else
							{
								float ao = 0;
								if (sideA > 0 && sideB > 0)
								{
									ao = 1;
								}
								else
								{
									// Only up to two of these will get hit
									if (corner > 0) ao++;
									if (sideA > 0) ao++;
									if (sideB > 0) ao++;

									ao /= 3;
								}

								vertex.AO = 1 - ao;
								vertices[i] = vertex;
							}
						}
					}
				}
			}

			/*for (int i = 0; i < vertices.Count; i += 4)
			{
				var aoA = vertices[i].AO;
				var aoB = vertices[i + 1].AO;
				var aoC = vertices[i + 2].AO;
				var aoD = vertices[i + 3].AO;

				if (aoA + aoD > aoB + aoC)
				{
					var v1 = vertices[i];
					var v2 = vertices[i + 1];
					var v3 = vertices[i + 2];
					var v4 = vertices[i + 3];

					var tmp = vertices[i].AO;
					v1.AO = v2.AO;
					v2.AO = v4.AO;
					v4.AO = v3.AO;
					v3.AO = tmp;

					vertices[i] = v1;
					vertices[i + 1] = v2;
					vertices[i + 2] = v3;
					vertices[i + 3] = v4;
				}
			}*/

			if (vertices.Count > 0 && indices.Count > 0)
			{
				var mesh = new ChunkMesh(device, vertices, indices);
				return mesh;
			}
			else return ChunkMesh.Empty;
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

			if ((visual.clearSides & MeshHelper.CubeFace.FRONT) == MeshHelper.CubeFace.FRONT)
				MakeQuadVerts(l_t_n, r_t_n, r_b_n, l_b_n, new Vector3(0, 0, -1), MeshHelper.CubeFace.FRONT, cube, vertices, indices);

			if ((visual.clearSides & MeshHelper.CubeFace.RIGHT) == MeshHelper.CubeFace.RIGHT)
				MakeQuadVerts(r_t_n, r_t_f, r_b_f, r_b_n, new Vector3(1, 0, 0), MeshHelper.CubeFace.RIGHT, cube, vertices, indices);

			if ((visual.clearSides & MeshHelper.CubeFace.BACK) == MeshHelper.CubeFace.BACK)
				MakeQuadVerts(r_t_f, l_t_f, l_b_f, r_b_f, new Vector3(0, 0, 1), MeshHelper.CubeFace.BACK, cube, vertices, indices);

			if ((visual.clearSides & MeshHelper.CubeFace.LEFT) == MeshHelper.CubeFace.LEFT)
				MakeQuadVerts(l_t_f, l_t_n, l_b_n, l_b_f, new Vector3(-1, 0, 0), MeshHelper.CubeFace.LEFT, cube, vertices, indices);

			if ((visual.clearSides & MeshHelper.CubeFace.DOWN) == MeshHelper.CubeFace.DOWN)
				MakeQuadVerts(l_t_f, r_t_f, r_t_n, l_t_n, new Vector3(0, -1, 0), MeshHelper.CubeFace.DOWN, cube, vertices, indices);

			if ((visual.clearSides & MeshHelper.CubeFace.UP) == MeshHelper.CubeFace.UP)
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
