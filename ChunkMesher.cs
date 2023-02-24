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
		private const int PER_BATCH_MAX = 200;
		public const int NUM_CHUNK_MESH_PASSES = 5;

		//Represents a chunk mesh batch, including everything about a chunk that is necessary to mesh it, or to get the info required to do so.
		private struct ChunkMeshBatch
		{
			public ChunkMeshInfo[] cmis;
			public int num;

			public readonly bool isUsed;

			public ChunkMeshBatch(ChunkMeshInfo[] cmis)
			{
				this.cmis = cmis;
				this.num = 0;

				isUsed = true;
			}
		}

		//The state of a chunk batch task state.
		private readonly struct ChunkBatchMeshTaskState
		{
			public readonly ChunkMeshBatch batch;
			public readonly World world;
			public readonly ChunkManager manager;
			public readonly ChunkMesher mesher;

			public ChunkBatchMeshTaskState(ChunkMeshBatch batch, World world, ChunkManager manager, ChunkMesher mesher)
			{
				this.batch = batch;
				this.world = world;
				this.manager = manager;
				this.mesher = mesher;
			}
		}

		//The result of a chunk batch task.
		private readonly struct ChunkBatchMeshTaskResult
		{
			public readonly ChunkMeshInfo[] cmis;
			//number of meshes included in the batch
			public readonly int num;

			public ChunkBatchMeshTaskResult(ChunkMeshInfo[] cmis, int num)
			{
				this.cmis = cmis;
				this.num = num;
			}
		}

		private struct ChunkMeshInfo
		{
			public ChunkPosition position;
			public ChunkMesh[] meshes;
			public byte meshVersion; //mesh version; if different from version, needs to be re-meshed
			public byte version;

			//There's a difference between having existing meshes and needing to be remeshed (being dirty) and not having meshes at all.
			//Therefore this bool exists to determine if the given chunk has a mesh. If it doesn't, it isn't necessarily marked dirty,
			//it just needs a mesh to be created in the first place.
			public bool hasMeshes;

			public ChunkMeshInfo(ChunkPosition position)
			{
				this.position = position;
				meshes = new ChunkMesh[NUM_CHUNK_MESH_PASSES];
				meshVersion = 0;
				version = 1;

				hasMeshes = false;
			}

			public int GetMeshVersionCode()
			{
				return meshes.GetHashCode() + version;
			}
		};

		private readonly GraphicsDevice device;
        private readonly int sizeInChunks;
        private ChunkMeshBatch currentBatch;

		private ChunkMeshInfo[] chunkMeshInfos;

		private Queue<ChunkPosition> dirtyChunkPositions = new Queue<ChunkPosition>();
		private HashSet<ChunkPosition> dirtyChunkKnown = new HashSet<ChunkPosition>();

		private Queue<Task<ChunkBatchMeshTaskResult>> chunkMeshBatchTasks = new Queue<Task<ChunkBatchMeshTaskResult>>();

		public ChunkMesher(GraphicsDevice device, int sizeInChunks)
        {
            this.device = device;
            this.sizeInChunks = sizeInChunks;

			chunkMeshInfos = new ChunkMeshInfo[sizeInChunks * sizeInChunks * sizeInChunks];
			for (int i = 0; i < sizeInChunks * sizeInChunks * sizeInChunks; i++)
			{
				Util.OneDToThreeD(i, new ValuePoint3D(sizeInChunks), out ValuePoint3D point);
				chunkMeshInfos[i] = new ChunkMeshInfo(new ChunkPosition(point.x, point.y, point.z));
			}
		}

		public void Update(World world, ChunkManager manager)
        {
			const int MAX_MESH_PER_FRAME = 200;
			int meshedInThisFrame = 0;

			if (!currentBatch.isUsed)
				currentBatch = new ChunkMeshBatch(new ChunkMeshInfo[PER_BATCH_MAX]);

			if (currentBatch.num >= PER_BATCH_MAX)
			{
				MeshBatch(world, ref currentBatch);
				currentBatch = new ChunkMeshBatch(new ChunkMeshInfo[PER_BATCH_MAX]);
			}

			//Note that we only attempt to mesh one batch per frame regardless of what MAX_MESH_PER_FRAME is.
			while (dirtyChunkPositions.Count > 0 && meshedInThisFrame < MAX_MESH_PER_FRAME && currentBatch.num < PER_BATCH_MAX)
			{
				ChunkPosition position = dirtyChunkPositions.Dequeue();
				dirtyChunkKnown.Remove(position);

				ref ChunkMeshInfo c = ref GetChunkMeshInfo(position);

				if (c.version != c.meshVersion || !c.hasMeshes)
				{
					//place into the current batch to be meshed later.
					currentBatch.cmis[currentBatch.num++] = c;
					meshedInThisFrame++;
				}
			}

			//we want to make sure to flush this regardless of whether or not we've actually filled it fully
			//As there might be frames where we don't fully fill it, in which case it could wait a potentially arbitrary amount of time.
			if (currentBatch.num > 0)
			{
				MeshBatch(world, ref currentBatch);
				currentBatch = new ChunkMeshBatch(new ChunkMeshInfo[PER_BATCH_MAX]);
			}

			FlushMeshQueue(5);
		}

		public void FlushMeshQueue(int count = -1)
		{
			bool canExitEarly = count != -1;

			int batchCount = count;
			while (chunkMeshBatchTasks.Count > 0 && ((canExitEarly && batchCount > 0) || !canExitEarly))
			{
				var task = chunkMeshBatchTasks.Dequeue();

				if (task.IsCompleted)
				{
					if (!task.IsCompletedSuccessfully)
						throw new Exception("???");

					var batchResult = task.Result;

					for (int i = 0; i < batchResult.num; i++)
					{
						ChunkMeshInfo meshResult = batchResult.cmis[i];

						ref ChunkMeshInfo c = ref GetChunkMeshInfo(meshResult.position);

						if (meshResult.version >= c.version)
						{
							//Unload the old mesh now
							UnloadMesh(ref c);

							c.meshVersion = c.version;

							c.meshes = meshResult.meshes;
						}
						else
						{
							//version has changed while we're meshing - discard the old mesh, as a new one should already be queued.
							UnloadMesh(ref meshResult);
						}
					}
				}
				//Task isn't finished, re-queue it.
				else chunkMeshBatchTasks.Enqueue(task);

				batchCount--;
			}
		}

		public void BatchMeshChunk(World world, ChunkPosition position)
		{
			if (!currentBatch.isUsed)
				currentBatch = new ChunkMeshBatch(new ChunkMeshInfo[PER_BATCH_MAX]);

			if (currentBatch.num >= PER_BATCH_MAX)
			{
				MeshBatch(world, ref currentBatch);
				currentBatch = new ChunkMeshBatch(new ChunkMeshInfo[PER_BATCH_MAX]);
			}

			ref ChunkMeshInfo c = ref GetChunkMeshInfo(position);

			if (c.version != c.meshVersion || !c.hasMeshes)
				currentBatch.cmis[currentBatch.num++] = c;
		}

		private void MeshBatch(World world, ref ChunkMeshBatch batch)
		{
			Task<ChunkBatchMeshTaskResult> task = new Task<ChunkBatchMeshTaskResult>(MeshBatchTaskFn, new ChunkBatchMeshTaskState(batch, world, world.ChunkManager, this));
			task.Start();

			chunkMeshBatchTasks.Enqueue(task);
		}

		private static ChunkBatchMeshTaskResult MeshBatchTaskFn(object obj)
		{
			ChunkBatchMeshTaskState state = (ChunkBatchMeshTaskState)obj;

			//Prevent setting for the duration
			lock (state.manager)
			{
				state.manager.LockSet = true;
				for (int i = 0; i < state.batch.num; i++)
				{
					ChunkMeshInfo cmi = state.batch.cmis[i];
                    cmi.meshes = new ChunkMesh[NUM_CHUNK_MESH_PASSES];

                    /*cmi.meshes[(int)Cube.RenderPass.Opaque] = state.mesher.GenerateChunk(state.world, state.manager, cmi.position, Cube.RenderPass.Opaque, true);
                    cmi.meshes[(int)Cube.RenderPass.Transparent] = state.mesher.GenerateChunk(state.world, state.manager, cmi.position, Cube.RenderPass.Transparent, false);
                    cmi.meshes[(int)Cube.RenderPass.DepthOnly] = state.mesher.GenerateChunk(state.world, state.manager, cmi.position, Cube.RenderPass.DepthOnly, false);
                    cmi.meshes[(int)Cube.RenderPass.Fluid] = null;   //TODO fluids?
                    cmi.meshes[(int)Cube.RenderPass.Air] = state.mesher.GenerateChunk(state.world, state.manager, cmi.position, Cube.RenderPass.Air, false);*/

                    state.batch.cmis[i] = cmi;
					state.batch.cmis[i].hasMeshes = true;
				}
				state.manager.LockSet = false;
			}

			return new ChunkBatchMeshTaskResult(state.batch.cmis, state.batch.num);
		}

		private void UnloadMesh(ref ChunkMeshInfo c)
		{
			for (int i = 0; i < NUM_CHUNK_MESH_PASSES; i++)
			{
				if (c.meshes[i] != null && c.meshes[i] != ChunkMesh.Empty)
				{
					c.meshes[i].VBO.Dispose();
					c.meshes[i].IBO.Dispose();

					c.meshes[i] = null;
				}
			}

			c.hasMeshes = false;
		}

		public void UnloadMesh(ChunkPosition position)
        {
			UnloadMesh(ref GetChunkMeshInfo(position));
        }

		public void UnloadAllMeshes()
		{
			for (int j = 0; j < sizeInChunks * sizeInChunks * sizeInChunks; j++)
			{
				for (int k = 0; k < NUM_CHUNK_MESH_PASSES; k++)
				{
					ChunkMesh mesh = chunkMeshInfos[j].meshes[k];
					if (mesh != null && mesh != ChunkMesh.Empty)
					{
						mesh.VBO.Dispose();
						mesh.IBO.Dispose();

						chunkMeshInfos[j].meshes[k] = null;
					}
				}

				chunkMeshInfos[j].hasMeshes = false;
			}
		}

		public void MarkDirty(ChunkPosition position)
		{
			GetChunkMeshInfo(position).version++;

			if (!dirtyChunkKnown.Contains(position))
			{
				dirtyChunkPositions.Enqueue(position);
				dirtyChunkKnown.Add(position);
			}
		}

		public ChunkMesh GetMesh(ChunkPosition position, Cube.RenderPass pass)
		{
			ChunkMesh mesh = GetChunkMeshInfo(position).meshes[(int)pass];

			if (mesh != null && !mesh.IsEmpty && mesh.VBO.IsDisposed)
				throw new Exception("??");

			return mesh;
		}

		private ref ChunkMeshInfo GetChunkMeshInfo(ChunkPosition pos)
		{
			Util.ThreeDToOneD(new ValuePoint3D(pos.X, pos.Y, pos.Z), new ValuePoint3D(sizeInChunks), out int i);
			return ref chunkMeshInfos[i];
		}

		public int GetMeshVersionCode(ChunkPosition position)
		{
			return GetChunkMeshInfo(position).GetMeshVersionCode();
		}

		public ChunkMesh GenerateChunk(World world, ChunkManager manager, ChunkPosition position, Cube.RenderPass pass, bool forceUpdate = false)
		{
			Vector3 n = new Vector3(0);
			Vector3 f = new Vector3(Cube.CUBE_SCALE);

			List<VertexCube> vertices = new List<VertexCube>();
			List<int> indices = new List<int>();

			int cpi = 0;
			Span<CubePosition> positions = stackalloc CubePosition[Chunk.CHUNK_SIZE * Chunk.CHUNK_SIZE * Chunk.CHUNK_SIZE];
			Span<ushort> ids = stackalloc ushort[Chunk.CHUNK_SIZE * Chunk.CHUNK_SIZE * Chunk.CHUNK_SIZE];
			Span<MeshHelper.CubeFace> faces = stackalloc MeshHelper.CubeFace[Chunk.CHUNK_SIZE * Chunk.CHUNK_SIZE * Chunk.CHUNK_SIZE];
			for (int x = 0; x < Chunk.CHUNK_SIZE; x++)
			{
				for (int y = 0; y < Chunk.CHUNK_SIZE; y++)
				{
					for (int z = 0; z < Chunk.CHUNK_SIZE; z++)
					{
						CubePosition pos = new CubePosition(x, y, z, CubePosition.CoordinateSpace.ChunkSpace);
						pos = pos.InCubeSpace(position);

						positions[cpi] = pos;
						cpi++;
					}
				}
			}

			manager.ThreadedView.GetIds(positions, ids);
			manager.ThreadedView.GetFaces(positions, faces);

			for (int i = 0; i < Chunk.CHUNK_SIZE * Chunk.CHUNK_SIZE * Chunk.CHUNK_SIZE; i++) 
			{
				CubePosition pos = positions[i];
				ushort id = ids[i];
				MeshHelper.CubeFace face = faces[i];

				if (pass == Cube.RenderPass.Transparent || pass == Cube.RenderPass.Opaque || pass == Cube.RenderPass.Fluid || pass == Cube.RenderPass.DepthOnly)
				{
					//if we're air or have no faces, ignore this cube.
					if (id == 0 || face == MeshHelper.CubeFace.NONE)
						continue;

					Cube cube = Main.Registry.CubeRegistry.Get(id);

					int oldCount = vertices.Count;

					cube.MakeVerts(pass, world, pos.InWorldSpace(), n + pos.InWorldSpace(), f + pos.InWorldSpace(), face, vertices, indices);

					int count = vertices.Count - oldCount;

					BakeAO(manager, pos, oldCount, oldCount + count, vertices);
				}
				else if (pass == Cube.RenderPass.Air)
				{
					//Note that for air, we we do still make verts if id is 0 (though still not if no faces).
					if (id != 0 || face == MeshHelper.CubeFace.NONE)
						continue;
					
					Main.Registry.CubeRegistry.Air.MakeVerts(pass, world, pos.InWorldSpace(), n + pos.InWorldSpace(), f + pos.InWorldSpace(), face, vertices, indices);
				}
			}

			if (vertices.Count > 0 && indices.Count > 0)
			{
				var mesh = new ChunkMesh(device, vertices, indices);

				return mesh;
			}
			else return ChunkMesh.Empty;
		}

		private static void BakeAO(ChunkManager manager, CubePosition pos, int start, int end, List<VertexCube> vertices)
        {
			Span<CubePosition> checkPositions = stackalloc CubePosition[4];
			Span<ushort> checkIds = stackalloc ushort[4];

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

				checkPositions[0] = nrm;
				checkPositions[1] = nrm + t + bt;
				checkPositions[2] = nrm + t;
				checkPositions[3] = nrm + bt;
				manager.ThreadedView.GetIds(checkPositions, checkIds);

				int top = checkIds[0];
				int corner = checkIds[1];
				int sideA = checkIds[2];
				int sideB = checkIds[3];

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
