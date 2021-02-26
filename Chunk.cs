using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Cubes;

namespace ViMG
{
	public class Chunk
	{
		private readonly int x;
		private readonly int y;
		private readonly int z;

		public const int CHUNK_SIZE = 16;

		public ChunkPosition Position => new ChunkPosition(x, y, z);

		private ChunkData data;

		private bool initialized;
		public bool Initialized => initialized;

		private World world;

		private SimpleMesh<VertexPositionColor, int> mesh;

		public Rectangle3D Bounds => new Rectangle3D(Position.InWorldSpace(), new Vector3(CHUNK_SIZE * Cube.CUBE_SCALE));

		public Chunk(int x, int y, int z)
		{
			data = new ChunkData(this);

			this.x = x;
			this.y = y;
			this.z = z;
		}

		public Chunk(ChunkPosition position) : this(position.X, position.Y, position.Z)
		{

		}

		public void Initialize(World world)
		{
			this.world = world;
			initialized = true;
		}

		public World GetWorld()
		{
			if (!initialized)
				throw new Exception("Cannot get world before initialized.");

			return world;
		}

		public void SetData(ChunkData data)
		{
			data.SetChunk(this);
			this.data = data;
		}

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
						Cube.CubeInstance cube = data.GetCube(x, y, z);
						Cube.CubeVisualInstance visInstance = data.GetVisual(x, y, z);

						if (cube.cubeId != 0 && cube.cubeId <= world.CubeRegistry.Count)
						{
							Cube c = world.CubeRegistry.Get(cube.cubeId);

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

			mesh.Draw(device, Main.BasicEffect, new Vector3(x, y, z) * CHUNK_SIZE * Cube.CUBE_SCALE, Vector3.Zero, Vector3.One);
		}
	}
}
