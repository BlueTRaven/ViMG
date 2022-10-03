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
		private ChunkManager manager;

		private static SimpleMesh<VertexPositionColor, int> mesh;

		public Rectangle3D Bounds => new Rectangle3D(Position.InWorldSpace(), new Vector3(CHUNK_SIZE * Cube.CUBE_SCALE));

		public Chunk(ChunkManager cm, int x, int y, int z) : this(cm, new ChunkPosition(x, y, z))
		{
		}

		public Chunk(ChunkManager cm, ChunkPosition position)
		{
			this.manager = cm;
			this.position = position;
		}

		public void Initialize(World world)
		{
			this.world = world;
			initialized = true;

			//PostChunkGen(world);
		}

		public void PostChunkGen(World world)
		{
			ushort[] cubes = GetData().GetAll();

			for (int i = 0; i < NUM_CUBES_IN_CHUNK; i++)
			{
				ushort id = cubes[i];

				if (id > 0)
				{
					int x = i % CHUNK_SIZE;
					int y = (i / CHUNK_SIZE) % CHUNK_SIZE;
					int z = i / (CHUNK_SIZE * CHUNK_SIZE);

					var pos = new CubePosition(x, y, z, CubePosition.CoordinateSpace.ChunkSpace);
					Cube cube = Main.Registry.CubeRegistry.Get(id);
					cube.PostChunkGen(GetData(), pos);

					if (cube.Solid)
						data.Density++;
				}
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
            else
            {
				//If data is null, we are no longer initialized.
				initialized = false;
            }

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
			/*device.RasterizerState = Main.wireframeRS;
			device.DepthStencilState = Main.genericDSS;

			if (mesh == null)
			{
				mesh = MeshHelper.MakeCubeVertexPositionColor(device, Vector3.Zero, new Vector3(CHUNK_SIZE) * Cube.CUBE_SCALE, MeshHelper.CubeFace.ALL, Color.White, DrawHelper.WhitePixel);
			}

			var mvp = Transform.FromTRS(new Vector3(position.X, position.Y, position.Z) * CHUNK_SIZE * Cube.CUBE_SCALE, Vector3.Zero, Vector3.One) * Main.camera.GetViewMatrix() * Main.camera.GetProjectionMatrix();
			mesh.Draw(device, Main.VertexPositionColorDebugEffect, mvp);*/
		}

        public ChunkManager GetChunkManager()
        {
			return manager;
        }
    }
}
