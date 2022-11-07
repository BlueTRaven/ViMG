using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace ViMG
{
	public class LightManager
	{
		public const int MAX_LIGHTS = 1024 * 8;
		public const int MAX_LIGHTS_SHADOWMAPPED = 4;

		private StructuredBuffer bufferLights;
		private StructuredBuffer bufferShadowmappedLights;
		private int version;
		private int lastUploadedVersion;

		public readonly struct Data
		{
			public readonly Vector4 color;
			public readonly Vector3 position;
			public readonly float start;
			public readonly float end;

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

			public readonly bool isShadowmapped;

			public readonly int index;
			public readonly bool active;

			public Light(Vector3 position, float start, float end, Color color, bool isShadowmapped, int index)
			{
				this.position = position;
				this.start = start;
				this.end = end;
				this.color = color;

				this.isShadowmapped = isShadowmapped;

				this.index = index;
				active = true;
			}
		}

		private static DepthStencilState dss = new DepthStencilState()
		{
			DepthBufferEnable = true,
			DepthBufferFunction = CompareFunction.LessEqual,
		};

		private static RasterizerState rs = new RasterizerState()
		{
			FillMode = FillMode.Solid,
			CullMode = CullMode.None,
			DepthClipEnable = false,
		};

		private RenderTargetCube lightsCubemaps;
		//private RenderTarget2D lightsCubemaps;
		private Matrix[][] lightShadowmappedMatrices = new Matrix[MAX_LIGHTS_SHADOWMAPPED][];

		private Light[] lights = new Light[MAX_LIGHTS];
		private Light[] lightsShadowmapped = new Light[MAX_LIGHTS_SHADOWMAPPED];

		private ushort[] lightVersions = new ushort[MAX_LIGHTS];
		private ushort[] oldLightVersions = new ushort[MAX_LIGHTS];

		private Data[] datas = new Data[MAX_LIGHTS];
		private Data[] datasShadowmapped = new Data[MAX_LIGHTS_SHADOWMAPPED];

		private int numUsedLights;
		private int numUsedLightsShadowmapped;

		public LightManager(GraphicsDevice device)
		{
			//lightsCubemaps = new RenderTarget2D(device, 256, 256, false, SurfaceFormat.Single, DepthFormat.Depth24, 0, RenderTargetUsage.PreserveContents, false, 
				//MAX_LIGHTS_SHADOWMAPPED * 6);
			lightsCubemaps = new RenderTargetCube(device, 256, false, SurfaceFormat.Single, DepthFormat.Depth24Stencil8, 0, RenderTargetUsage.PreserveContents, MAX_LIGHTS_SHADOWMAPPED);

			for (int i = 0; i < MAX_LIGHTS_SHADOWMAPPED; i++)
			{
				lightShadowmappedMatrices[i] = new Matrix[6];
				Array.Fill(lightShadowmappedMatrices[i], Matrix.Identity);

				datasShadowmapped[i] = new Data(new Light());
			}
			/*for (int i = 0; i < MAX_LIGHTS_SHADOWMAPPED; i++)
				lightsCubemaps[i] = new RenderTargetCube(device, 256, false, SurfaceFormat.Single, DepthFormat.Depth24);*/
		}

		public Light Get(int index)
        {
			return lights[index];
        }

		public int Add(Vector3 position, float start, float end, Color color)
		{
			//Early-out - we have no more light slots available.
			if (numUsedLights >= MAX_LIGHTS)
				return -1;

			for (int i = 0; i < MAX_LIGHTS; i++)
			{
				if (!lights[i].active)
				{
					lights[i] = new Light(position, start, end, color, true, i);

					version++;
					lightVersions[i]++;

					numUsedLights++;

					return i;
				}
			}

			return -1;
		}

		public void AddShadowmapped(Vector3 position, float start, float end, Color color, out int shadowmappedLightIndex, out bool success)
		{
			//Attempt to allocate a non-shadowmapped light if we're above the max shadowmapped lights.
			if (numUsedLightsShadowmapped >= MAX_LIGHTS_SHADOWMAPPED)
            {
				shadowmappedLightIndex = Add(position, start, end, color);
				success = false;
				return;
            }

			for (int i = 0; i < MAX_LIGHTS_SHADOWMAPPED; i++)
			{
				if (!lightsShadowmapped[i].active)
				{
					lightsShadowmapped[i] = new Light(position, start, end, color, true, i);

					version++;
					lightVersions[i]++;

					numUsedLightsShadowmapped++;

					shadowmappedLightIndex = i;
					success = true;

					return;
				}
			}

			shadowmappedLightIndex = -1;
			success = false;
		}

		public void Update(int index, Vector3 position, float start, float end, Color color)
        {
			if (lights[index].active)
            {
				lightVersions[index]++;
				lights[index] = new Light(position, start, end, color, false, index);
            }
        }

		public void UpdateShadowmapped(int index, Vector3 position, float start, float end, Color color)
		{
			if (lightsShadowmapped[index].active)
			{
				lightsShadowmapped[index] = new Light(position, start, end, color, true, index);
			}
		}

		public void Remove(int index)
		{
			if (lights[index].active)
			{
				lights[index] = new Light();
				version++;

				numUsedLights--;
			}
		}

		public void RemoveShadowmapped(int index)
        {
			if (lightsShadowmapped[index].active)
			{
				lightsShadowmapped[index] = new Light();
				version++;

				numUsedLightsShadowmapped--;
			}
		}

		public void UpdateDatas(Effect effect)
		{
			if (bufferLights == null)
				bufferLights = new StructuredBuffer(effect.GraphicsDevice, typeof(Data), MAX_LIGHTS, BufferUsage.WriteOnly, ShaderAccess.Read);

			if (bufferShadowmappedLights == null)
				bufferShadowmappedLights = new StructuredBuffer(effect.GraphicsDevice, typeof(Data), MAX_LIGHTS_SHADOWMAPPED, BufferUsage.WriteOnly, ShaderAccess.Read);

			//if (version != lastUploadedVersion)
			{
				lastUploadedVersion = version;

				for (int i = 0; i < MAX_LIGHTS; i++)
				{
					datas[i] = new Data(lights[i]);
				}

				for (int i = 0; i < MAX_LIGHTS_SHADOWMAPPED; i++)
				{
					if (lightsShadowmapped[i].active)
					{
						Matrix proj = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(90f), 1, 0.001f, lightsShadowmapped[i].end);
						//proj *= Matrix.CreateScale(-1, 1, -1);

						lightShadowmappedMatrices[i][0] = Matrix.CreateLookAt(lightsShadowmapped[i].position, lightsShadowmapped[i].position + new Vector3(1, 0, 0), new Vector3(0, -1, 0)) * proj;
						lightShadowmappedMatrices[i][1] = Matrix.CreateLookAt(lightsShadowmapped[i].position, lightsShadowmapped[i].position + new Vector3(-1, 0, 0), new Vector3(0, -1, 0)) * proj;
						lightShadowmappedMatrices[i][2] = Matrix.CreateLookAt(lightsShadowmapped[i].position, lightsShadowmapped[i].position + new Vector3(0, -1, 0), new Vector3(0, 0, -1)) * proj;
						lightShadowmappedMatrices[i][3] = Matrix.CreateLookAt(lightsShadowmapped[i].position, lightsShadowmapped[i].position + new Vector3(0, 1, 0), new Vector3(0, 0, -1)) * proj;
						lightShadowmappedMatrices[i][4] = Matrix.CreateLookAt(lightsShadowmapped[i].position, lightsShadowmapped[i].position + new Vector3(0, 0, 1), new Vector3(0, -1, 0)) * proj;
						lightShadowmappedMatrices[i][5] = Matrix.CreateLookAt(lightsShadowmapped[i].position, lightsShadowmapped[i].position + new Vector3(0, 0, -1), new Vector3(0, -1, 0)) * proj;
					}
                    else
                    {
						Array.Fill(lightShadowmappedMatrices[i], Matrix.Identity);
					}

					datasShadowmapped[i] = new Data(lightsShadowmapped[i]);
				}

				bufferLights.SetData(datas);
				bufferShadowmappedLights.SetData(datasShadowmapped);
			}

			effect.Parameters["Lights"].SetValue(bufferLights);
			effect.Parameters["ShadowmappedLights"].SetValue(bufferShadowmappedLights);
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

			for (int i = 0; i < MAX_LIGHTS_SHADOWMAPPED; i++)
            {
				Light light = lightsShadowmapped[i];
				
				if (light.active)
					Main.Renderer.DrawsShadowmappedPointLightVolumePass.Add(new Rendering.RendererDeferred.PointLightVolumeDraw(i, light.position, light.end, null));
			}
        }

		public void DrawShadowmap(GraphicsDevice device, World world)
		{
			if (!Main.ENABLE_SHADOWS)
			{
				return;
			}

			device.DepthStencilState = dss;
			device.RasterizerState = rs;

			Effect effectDepth = Main.assetsManager.GetAsset<Effect>("depth_pointlight");

			//To begin with, draw everything every frame. This is SLOW! Eventually we'll want to only draw these lights
			//if something changes in them (i.e. chunk is dirty)
			for (int i = 0; i < MAX_LIGHTS_SHADOWMAPPED; i++)
			{
				Light light = lightsShadowmapped[i];

				if (light.active)
				{
					effectDepth.Parameters["LightPosition"].SetValue(light.position);
					effectDepth.Parameters["FarPlane"].SetValue(light.end);

					for (int j = 0; j < 6; j++)
					{
						Matrix viewProj = lightShadowmappedMatrices[i][j];
						effectDepth.Parameters["ViewProjection"].SetValue(viewProj);

						device.SetRenderTarget(lightsCubemaps, (CubeMapFace)j, i);
						//device.SetRenderTarget(lightsCubemaps, i * 6 + j);
						device.Clear(ClearOptions.Target | ClearOptions.DepthBuffer | ClearOptions.Stencil, Color.White, device.Viewport.MaxDepth, 0);

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
										ChunkMesh mesh = world.ChunkManager.GetMesh(chunkPos, Cubes.Cube.RenderPass.DepthOnly);
										Matrix transform = world.ChunkManager.GetTransform(chunkPos);

										if (mesh != null && !mesh.IsEmpty)
										{
											device.SetVertexBuffer(mesh.VBO);
											device.Indices = mesh.IBO;

											effectDepth.Parameters["World"].SetValue(transform);

											foreach (var pass in effectDepth.CurrentTechnique.Passes)
											{
												pass.Apply();
												device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, mesh.IndexCount / 3);
											}
										}
									}
								}
							}
						}
					}

					Main.Renderer.EffectLightAccumPointLight.Parameters["Cubemaps"].SetValue(lightsCubemaps);
					//Main.CubeLitEffect.Parameters["TexturesPointLights[" + i + "]"].SetValue(lightsCubemaps[i]);
				}

				//Main.CubeLitEffect.Parameters["Test"].SetValue(lightsCubemaps[0]);
			}

			Main.WVP.SetProjection(Main.camera.GetProjectionMatrix());
			Main.WVP.SetView(Main.camera.GetViewMatrix());
		}
	}
}
