using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace ViMG
{
	public class LightManager
	{
		public const int MAX_LIGHTS = 512;

		private StructuredBuffer structuredBuffer;
		private int version;
		private int lastUploadedVersion;

		public readonly struct Data
		{
			public readonly Vector4 color;
			public readonly Vector3 position;
			public readonly float start;
			public readonly float end;

			//public readonly Matrix[] lightViewProjections;

			public Data(Vector3 position, float start, float end, Vector4 color)
			{
				this.position = position;
				this.start = start;
				this.end = end;
				this.color = color;

				//lightViewProjections = null;
			}

			public Data(Light light, float intensity = 1)
			{
				if (!light.active)
				{
					this.position = Vector3.Zero;
					this.start = 0;
					this.end = 0;
					this.color = Color.Transparent.ToVector4();
				}
				else
				{
					this.position = light.position;
					this.start = light.start;
					this.end = light.end;
					this.color = light.color.ToVector4();
				}
			}
		}

		public readonly struct Light
		{
			public readonly Vector3 position;
			public readonly float start;
			public readonly float end;
			public readonly Color color;

			public readonly int index;
			public readonly bool active;

			public Light(Vector3 position, float start, float end, Color color, int index)
			{
				this.position = position;
				this.start = start;
				this.end = end;
				this.color = color;

				this.index = index;
				active = true;
			}
		}

		private RenderTargetCube[] lightsCubemaps = new RenderTargetCube[MAX_LIGHTS];

		private Light[] lights = new Light[MAX_LIGHTS];
		private Data[] datas = new Data[MAX_LIGHTS];

		public LightManager(GraphicsDevice device)
		{
			//for (int i = 0; i < MAX_LIGHTS; i++)
				//lightsCubemaps[i] = new RenderTargetCube(device, 256, false, SurfaceFormat.Single, DepthFormat.Depth24);
		}

		public Light Get(int index)
        {
			return lights[index];
        }

		public int Add(Vector3 position, float start, float end, Color color)
		{
			for (int i = 0; i < MAX_LIGHTS; i++)
			{
				if (!lights[i].active)
				{
					lights[i] = new Light(position, start, end, color, i);

					version++;
					return i;
				}
			}

			return -1;
		}

		public void Update(int index, Vector3 position, float start, float end, Color color)
        {
			if (lights[index].active)
            {
				lights[index] = new Light(position, start, end, color, index);
            }
        }

		public void Remove(int index)
		{
			if (lights[index].active)
			{
				lights[index] = new Light();
				version++;
			}
		}

		public void UpdateDatas(Effect effect)
		{
			if (structuredBuffer == null)
				structuredBuffer = new StructuredBuffer(effect.GraphicsDevice, typeof(Data), MAX_LIGHTS, BufferUsage.WriteOnly, ShaderAccess.Read);

			if (version != lastUploadedVersion)
			{
				lastUploadedVersion = version;

				for (int i = 0; i < MAX_LIGHTS; i++)
				{
					datas[i] = new Data(lights[i]);
				}

				structuredBuffer.SetData(datas);

			}

			effect.Parameters["Lights"].SetValue(structuredBuffer);
		}

		private Matrix[] views = new Matrix[6];

		public void Draw(GraphicsDevice device)
        {
			for (int i = 0; i < MAX_LIGHTS; i++)
            {
				Light light = lights[i];

				if (light.active)
					Main.Renderer.DrawsPointLightVolumePass.Add(new Rendering.RendererDeferred.PointLightVolumeDraw(i, light.position, light.end));
            }
        }

		public void DrawShadowmap(GraphicsDevice device, World world)
		{
			if (!Main.ENABLE_SHADOWS)
			{
				return;
			}

			//To begin with, draw everything every frame. This is SLOW! Eventually we'll want to only draw these lights
			//if something changes in them (i.e. chunk is dirty)
			for (int i = 0; i < MAX_LIGHTS; i++)
			{
				Light light = lights[i];

				if (light.active)
				{
					Matrix proj = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(90f), 1, 0.001f, light.end);

					views[0] = Matrix.CreateLookAt(light.position, light.position + new Vector3(1, 0, 0), new Vector3(0, 1, 0));
					views[1] = Matrix.CreateLookAt(light.position, light.position + new Vector3(-1, 0, 0), new Vector3(0, 1, 0));
					views[2] = Matrix.CreateLookAt(light.position, light.position + new Vector3(0, 1, 0), new Vector3(0, 0, 1));
					views[3] = Matrix.CreateLookAt(light.position, light.position + new Vector3(0, -1, 0), new Vector3(0, 0, -1));
					views[4] = Matrix.CreateLookAt(light.position, light.position + new Vector3(0, 0, 1), new Vector3(0, 1, 0));
					views[5] = Matrix.CreateLookAt(light.position, light.position + new Vector3(0, 0, -1), new Vector3(0, 1, 0));

					//Main.WVP.SetProjection(proj);

					for (int j = 0; j < 6; j++)
					{
						device.SetRenderTarget(lightsCubemaps[i], (CubeMapFace)j);
						device.Clear(ClearOptions.Target | ClearOptions.DepthBuffer | ClearOptions.Stencil, Color.White, device.Viewport.MaxDepth, 0);

						//Main.WVP.SetView(views[j]);
						Matrix viewProj = views[j] * proj;

						//TODO: fit drawn chunks more accurately. Right now we're drawing tons of unseen stuff
						const int drawDist = 1;
						for (int x = -drawDist; x <= drawDist; x++)
						{
							for (int y = -drawDist; y <= drawDist; y++)
							{
								for (int z = -drawDist; z <= drawDist; z++)
								{
									ChunkPosition chunkPos = ChunkPosition.WorldSpaceChunk(light.position);
									chunkPos.X += x;
									chunkPos.Y += y;
									chunkPos.Z += z;

									if (world.ChunkManager.IsInWorldBounds(chunkPos))
									{
										ChunkMesh mesh = world.ChunkManager.GetMesh(chunkPos, 0);
										Matrix transform = world.ChunkManager.GetTransform(chunkPos);

										if (mesh != null)
										{
											mesh.DrawDepth(device, Main.assetsManager.GetAsset<Effect>("depth"), transform, viewProj);
										}
									}
								}
							}
						}
					}

					//Main.CubeLitEffect.Parameters["TexturesPointLights[" + i + "]"].SetValue(lightsCubemaps[i]);
				}

				Main.CubeLitEffect.Parameters["Test"].SetValue(lightsCubemaps[0]);
			}

			Main.WVP.SetProjection(Main.camera.GetProjectionMatrix());
			Main.WVP.SetView(Main.camera.GetViewMatrix());
		}
	}
}
