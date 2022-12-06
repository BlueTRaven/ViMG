using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ViMG.Cubes;

namespace ViMG
{
	public class ChunkMesher
	{
		public struct ChunkMeshTaskState
		{
			public ushort[] data;
			public World world;
			public ChunkManager2 manager;
			public ChunkPosition position;
			public Cube.RenderPass pass;

			public ChunkMeshTaskState(ushort[] data, World world, ChunkManager2 manager, ChunkPosition position, Cube.RenderPass pass)
			{
				this.data = data;
				this.world = world;
				this.manager = manager;
				this.position = position;
				this.pass = pass;
			}
		}

		private GraphicsDevice device;

		private const int NUM_MESH_TASKS = 8;

		private ChunkMeshTaskState[] taskStates = new ChunkMeshTaskState[NUM_MESH_TASKS];
		private Task<ChunkMesh>[] taskPool = new Task<ChunkMesh>[NUM_MESH_TASKS];

		public ChunkMesher(GraphicsDevice device)
		{
			this.device = device;

			for (int i = 0; i < NUM_MESH_TASKS; i++)
            {
				taskPool[i] = new Task<ChunkMesh>(PerformTask, i);
            }
		}

		public Task<ChunkMesh> StartConcurrent(World world, ChunkManager2 manager, ChunkPosition position, Cube.RenderPass pass)
        {
			Task<ChunkMesh> usingTask = null;
			int index = 0;

			for (int i = 0; i < NUM_MESH_TASKS; i++)
            {
				Task<ChunkMesh> t = taskPool[i];

				if (t.Status != TaskStatus.Running)
                {
					usingTask = t;
					index = i;

					break;
                }
            }

			if (usingTask != null)
            {
                ushort[] data = new ushort[(Chunk.CHUNK_SIZE + 2) * (Chunk.CHUNK_SIZE + 2) * (Chunk.CHUNK_SIZE + 2)];

				CubePosition start = new CubePosition(position.X * Chunk.CHUNK_SIZE - 1, position.Y * Chunk.CHUNK_SIZE - 1, position.Z * Chunk.CHUNK_SIZE - 1);
				CubePosition end = start + new CubePosition(Chunk.CHUNK_SIZE + 2, Chunk.CHUNK_SIZE + 2, Chunk.CHUNK_SIZE + 2);

				for (int x = start.X; x <= end.X; x++)
                {
					for (int y = start.Y - 1; y <= end.Y + 1; y++)
                    {
						for (int z = start.Z; z <= end.Z; z++)
                        {
							int rx = x - start.X;
							int ry = y - start.Y;
							int rz = z - start.Z;

							Util.ThreeDToOneD(new ValuePoint3D(rx, ry, rz), new ValuePoint3D(Chunk.CHUNK_SIZE + 2), out int dataIndex);

							data[dataIndex] = manager.GetCubeId(new CubePosition(x, y, z));
                        }
					}
				}

                taskStates[index] = new ChunkMeshTaskState(data, world, manager, position, pass);
				usingTask.Start();
            }

			return usingTask;
        }

		public Task<ChunkMesh> StartTask()
        {
			return null; 
        }

		private ChunkMesh PerformTask(object o)
        {
			ChunkMeshTaskState state = taskStates[(int)o];
			return GenerateChunk(state.world, state.manager, state.position, state.pass);
        }

		public ChunkMesh GenerateChunk(World world, ChunkManager2 manager, ChunkPosition position, Cube.RenderPass pass, bool forceUpdate = false)
		{
			Vector3 n = new Vector3(0);
			Vector3 f = new Vector3(Cube.CUBE_SCALE);

			List<VertexCube> vertices = new List<VertexCube>();
			List<int> indices = new List<int>();

			for (int x = 0; x < Chunk.CHUNK_SIZE; x++)
			{
				for (int y = 0; y < Chunk.CHUNK_SIZE; y++)
				{
					for (int z = 0; z < Chunk.CHUNK_SIZE; z++)
					{
						CubePosition pos = new CubePosition(x, y, z, CubePosition.CoordinateSpace.ChunkSpace);
						pos = pos.InCubeSpace(position);

						ushort id = manager.GetCubeId(pos);

						MeshHelper.CubeFace faces = manager.GetCachedFaces(pos);

						if (pass == Cube.RenderPass.Transparent || pass == Cube.RenderPass.Opaque || pass == Cube.RenderPass.Fluid || pass == Cube.RenderPass.DepthOnly)
						{
							if (id == 0 || faces == MeshHelper.CubeFace.NONE)
							{
								continue;
							}

							Cube cube = Main.Registry.CubeRegistry.Get(id);
							
							int oldCount = vertices.Count;

							cube.MakeVerts(pass, world, pos.InWorldSpace(null), n + pos.InWorldSpace(null), f + pos.InWorldSpace(null), faces, vertices, indices);

							int count = vertices.Count - oldCount;

							BakeAO(manager, pos, oldCount, oldCount + count, vertices);
						}
                        else if (pass == Cube.RenderPass.Air)
                        {
							if (id != 0 || faces == MeshHelper.CubeFace.NONE)
							{
								continue;
							}

							Main.Registry.CubeRegistry.Air.MakeVerts(pass, world, pos.InWorldSpace(null), n + pos.InWorldSpace(null), f + pos.InWorldSpace(null), faces, vertices, indices);
						}
					}
				}
			}

			if (vertices.Count > 0 && indices.Count > 0)
			{
				var mesh = new ChunkMesh(device, vertices, indices);

				return mesh;
			}
			else return ChunkMesh.Empty;
		}

		private static void BakeAO(ChunkManager2 manager, CubePosition pos, int start, int end, List<VertexCube> vertices)
        {
			for (int i = start; i < end; i++)
			{
				VertexCube vertex = vertices[i];

				//pos =
				CubePosition cubePos = pos;
				//pc =
				CubePosition vertCubePos = CubePosition.FromWorldSpace(vertex.Position);

				CubePosition nrm = new CubePosition(cubePos.X + (int)vertex.Normal.X,
					cubePos.Y + (int)vertex.Normal.Y,
					cubePos.Z + (int)vertex.Normal.Z, CubePosition.CoordinateSpace.CubeSpace);

				CubePosition t = new CubePosition();
				CubePosition bt = new CubePosition();

				int sX = vertCubePos.X == cubePos.X ? -1 : 1;
				int sY = vertCubePos.Y == cubePos.Y ? -1 : 1;
				int sZ = vertCubePos.Z == cubePos.Z ? -1 : 1;

				if (vertex.Normal.X != 0)
				{
					t = new CubePosition(0, sY, 0, CubePosition.CoordinateSpace.CubeSpace);
					bt = new CubePosition(0, 0, sZ, CubePosition.CoordinateSpace.CubeSpace);
				}
				else if (vertex.Normal.Y != 0)
				{
					t = new CubePosition(sX, 0, 0, CubePosition.CoordinateSpace.CubeSpace);
					bt = new CubePosition(0, 0, sZ, CubePosition.CoordinateSpace.CubeSpace);
				}
				else if (vertex.Normal.Z != 0)
				{
					t = new CubePosition(sX, 0, 0, CubePosition.CoordinateSpace.CubeSpace);
					bt = new CubePosition(0, sY, 0, CubePosition.CoordinateSpace.CubeSpace);
				}

				int top = GetIdSafely(manager, nrm);// chunk.GetData().GetRawOrAdjacent(nrm, world);
				int corner = GetIdSafely(manager, nrm + t + bt);// chunk.GetData().GetRawOrAdjacent(nrm + t + bt, world);
				int sideA = GetIdSafely(manager, nrm + t);// chunk.GetData().GetRawOrAdjacent(nrm + t, world);
				int sideB = GetIdSafely(manager, nrm + bt);// chunk.GetData().GetRawOrAdjacent(nrm + bt, world);

				if (corner > 0 && Main.Registry.CubeRegistry.noAo[corner])
					corner = 0;
				if (sideA > 0 && Main.Registry.CubeRegistry.noAo[sideA])
					sideA = 0;
				if (sideB > 0 && Main.Registry.CubeRegistry.noAo[sideB])
					sideB = 0;

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

		private static ushort GetIdSafely(ChunkManager2 manager, CubePosition position)
        {
			if (manager.IsInWorldBounds(position))
				return manager.GetCubeId(position);
			return 0;
        }

		public static void MakeCubeVerts(Cube.RenderPass pass, World world, CubePosition cp, Vector3 min, Vector3 max, MeshHelper.CubeFace faces, Cube cube, List<VertexCube> vertices, List<int> indices)
		{
			Vector3 l_t_n = new Vector3(min.X, min.Y, min.Z);
			Vector3 r_t_n = new Vector3(max.X, min.Y, min.Z);
			Vector3 r_b_n = new Vector3(max.X, max.Y, min.Z);
			Vector3 l_b_n = new Vector3(min.X, max.Y, min.Z);
			Vector3 l_t_f = new Vector3(min.X, min.Y, max.Z);
			Vector3 r_t_f = new Vector3(max.X, min.Y, max.Z);
			Vector3 r_b_f = new Vector3(max.X, max.Y, max.Z);
			Vector3 l_b_f = new Vector3(min.X, max.Y, max.Z);

			if ((faces & MeshHelper.CubeFace.FRONT) == MeshHelper.CubeFace.FRONT)
				MakeQuadVerts(pass, world, cp, l_t_n, r_t_n, r_b_n, l_b_n, new Vector3(0, 0, -1), MeshHelper.CubeFace.FRONT, cube, vertices, indices);

			if ((faces & MeshHelper.CubeFace.RIGHT) == MeshHelper.CubeFace.RIGHT)
				MakeQuadVerts(pass, world, cp, r_t_n, r_t_f, r_b_f, r_b_n, new Vector3(1, 0, 0), MeshHelper.CubeFace.RIGHT, cube, vertices, indices);

			if ((faces & MeshHelper.CubeFace.BACK) == MeshHelper.CubeFace.BACK)
				MakeQuadVerts(pass, world, cp, r_t_f, l_t_f, l_b_f, r_b_f, new Vector3(0, 0, 1), MeshHelper.CubeFace.BACK, cube, vertices, indices);

			if ((faces & MeshHelper.CubeFace.LEFT) == MeshHelper.CubeFace.LEFT)
				MakeQuadVerts(pass, world, cp, l_t_f, l_t_n, l_b_n, l_b_f, new Vector3(-1, 0, 0), MeshHelper.CubeFace.LEFT, cube, vertices, indices);

			if ((faces & MeshHelper.CubeFace.DOWN) == MeshHelper.CubeFace.DOWN)
				MakeQuadVerts(pass, world, cp, l_t_f, r_t_f, r_t_n, l_t_n, new Vector3(0, -1, 0), MeshHelper.CubeFace.DOWN, cube, vertices, indices);

			if ((faces & MeshHelper.CubeFace.UP) == MeshHelper.CubeFace.UP)
				MakeQuadVerts(pass, world, cp, r_b_f, l_b_f, l_b_n, r_b_n, new Vector3(0, 1, 0), MeshHelper.CubeFace.UP, cube, vertices, indices);
		}

		public static void MakeQuadVerts(Cube.RenderPass pass, World world, CubePosition cp, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal, MeshHelper.CubeFace face, Cube cube, 
			List<VertexCube> vertices, List<int> indices)
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

			RectangleF sourceRect = cube.GetSourceRect(pass, world, cp, face);

			Vector2 uvNear = new Vector2(sourceRect.x * cubeSideWidth, sourceRect.y * cubeSideHeight);
			Vector2 uvFar = new Vector2((sourceRect.x + sourceRect.width) * cubeSideWidth, (sourceRect.y + sourceRect.height) * cubeSideHeight);

			vertices.Add(new VertexCube(a, cube.GetTintColor(), new Vector2(uvFar.X, uvFar.Y), normal));
			vertices.Add(new VertexCube(b, cube.GetTintColor(), new Vector2(uvNear.X, uvFar.Y), normal));
			vertices.Add(new VertexCube(c, cube.GetTintColor(), new Vector2(uvNear.X, uvNear.Y), normal));
			vertices.Add(new VertexCube(d, cube.GetTintColor(), new Vector2(uvFar.X, uvNear.Y), normal));

			var anim = cube.GetAnimation(face, pass, world, cp);

			if (anim.Valid)
			{
				for (int i = offset; i < 4; i++)
				{
					var vertex = vertices[i];

					vertex.AnimFrameTime = anim.FrameTime;
					vertex.NumAnimFrames = anim.NumFrames;
					vertex.AnimFrameSize = anim.FrameWidth;

					vertices[i] = vertex;
				}
			}
		}
	}
}
