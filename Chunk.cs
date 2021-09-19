using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using ViMG.Cubes;

namespace ViMG
{
	public class Chunk
	{
		private ChunkPosition position;

		public const int CHUNK_SIZE = 16;
		public const int NUM_CUBES_IN_CHUNK = CHUNK_SIZE * CHUNK_SIZE * CHUNK_SIZE;

		public ChunkPosition Position => position;

		private ChunkData data;

		private bool initialized;
		public bool Initialized => initialized;

		private World world;

		private static SimpleMesh<VertexPositionColor, int> mesh;

		public Rectangle3D Bounds => new Rectangle3D(Position.InWorldSpace(), new Vector3(CHUNK_SIZE * Cube.CUBE_SCALE));

		public Chunk(GenericPool<ChunkData> chunkDatas, int x, int y, int z) : this(chunkDatas, new ChunkPosition(x, y, z))
		{
		}

		public Chunk(GenericPool<ChunkData> chunkDatas, ChunkPosition position)
		{
			data = chunkDatas.Get();
			data.SetChunk(this);
			data.GenStep = ChunkData.GenerationStep.Broad;

			this.position = position;
		}

		~Chunk()
		{
			world.GetChunkManager().ChunkDatas.Return(data);
		}

		public void Initialize(World world)
		{
			this.world = world;
			initialized = true;

			//PostChunkGen(world);
		}

		public void PostChunkGen(World world)
		{
			/*for (int x = 0; x < CHUNK_SIZE; x++)
			{
				for (int y = 0; y < CHUNK_SIZE; y++)
				{
					for (int z = 0; z < CHUNK_SIZE; z++)
					{
						var pos = new CubePosition(x, y, z, CubePosition.CoordinateSpace.ChunkSpace);

						GetData().GetCube(x, y, z).GetOrDefault(Main.Registry.CubeRegistry.Air).PostChunkGen(GetData(), pos);
					}
				}
			}*/

			for (int i = 0; i < NUM_CUBES_IN_CHUNK; i++)
			{
				int id = GetData().GetRaw(i);

				if (id > 0)
				{
					int x = i % CHUNK_SIZE;
					int y = (i / CHUNK_SIZE) % CHUNK_SIZE;
					int z = i / (CHUNK_SIZE * CHUNK_SIZE);

					var pos = new CubePosition(x, y, z, CubePosition.CoordinateSpace.ChunkSpace);
					Main.Registry.CubeRegistry.Get(id).PostChunkGen(GetData(), pos);
				}

				//GetData().GetCube(x, y, z).GetOrDefault(Main.Registry.CubeRegistry.Air).PostChunkGen(GetData(), pos);
			}
		}

		public World GetWorld()
		{
			if (!initialized)
				throw new Exception("Cannot get world before initialized.");

			return world;
		}

		public void SetData(ChunkData data)
		{
			//support unsetting data
			if (data != null)
				data.SetChunk(this);

			this.data = data;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ChunkData GetData()
		{
			return data;
		}

		public void Draw(GraphicsDevice device, Effect effect)
		{
			throw new Exception();

			for (int x = 0; x < CHUNK_SIZE; x++)
			{
				for (int y = 0; y < CHUNK_SIZE; y++)
				{
					for (int z = 0; z < CHUNK_SIZE; z++)
					{
						Cube.CubeInstance cube = data.GetCubeInstance(x, y, z);
						Cube.CubeVisualInstance visInstance = data.GetVisual(x, y, z);

						if (cube.cubeId != 0 && cube.cubeId <= Main.Registry.CubeRegistry.Count)
						{
							Cube c = Main.Registry.CubeRegistry.Get(cube.cubeId);

							//c.Draw(device, effect, cube, visInstance);
						}
					}
				}
			}
		}

		public void DrawDebug(GraphicsDevice device)
		{
			device.RasterizerState = Main.wireframeRS;
			device.DepthStencilState = Main.genericDSS;

			if (mesh == null)
			{
				mesh = MeshHelper.MakeCubeVertexPositionColor(device, Vector3.Zero, new Vector3(CHUNK_SIZE) * Cube.CUBE_SCALE, MeshHelper.CubeFace.ALL, Color.White, DrawHelper.WhitePixel);
			}

			mesh.Draw(device, Main.BasicEffect, new Vector3(position.X, position.Y, position.Z) * CHUNK_SIZE * Cube.CUBE_SCALE, Vector3.Zero, Vector3.One);
		}
	}
}
