using Engine.Clients;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG;
using ViMG.IMGUIImpl;
using ViMG.Rendering;

namespace Engine.Common
{
    [Obsolete()]
    public class LightManager
	{
		[ConsoleCommandVar("r_draw_light_instance_volumes", "Draws light instance volumes - for debugging purposes. Normal lights are red, shadowmapped lights are orange.")]
		public static bool DebugDrawLightInstanceVolumes = false;

		public static int LightManagerGeneration = 0;
		private static int lightsMax = 8192;
		[ConsoleCommandVar("r_max_lights", "Maximum number of lights. Default = 8192")]
		public static int LightsMax { get => lightsMax; set { lightsMax = value; LightManagerGeneration += 1; } }
		private static int lightsShadowmappedMax = 8;
		[ConsoleCommandVar("r_max_shadowmapped_lights", "Maximum number of shadowmapped lights. Default = 8")]
		public static int LightsShadowmappedMax { get => lightsShadowmappedMax; set { lightsShadowmappedMax = value; LightManagerGeneration += 1; } }

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

			public readonly float useNDotL;

			public Data(Light light, float intensity = 1)
            {
                if (!light.active)
                {
                    this.position = Vector3.Zero;
                    this.start = 0;
                    this.end = 0;
                    this.color = Color.Transparent.ToVector4();
					this.useNDotL = 1;
                }
                else
                {
                    this.position = light.position;
                    this.start = light.start;
                    this.end = light.end;
                    this.color = light.color;

					this.useNDotL = light.useNDotL ? 1 : 0;
                }
            }
        }

		public readonly struct Light
		{
			public readonly Vector3 position;
			public readonly float start;
			public readonly float end;
			public readonly Vector4 color;

			public readonly bool isShadowmapped;
			public readonly bool useNDotL;

			public readonly int index;
			public readonly bool active;

			public Light(Vector3 position, float start, float end, Vector4 color, bool isShadowmapped, bool useNDotL, int index)
			{
				this.position = position;
				this.start = start;
				this.end = end;
				this.color = color;

				this.isShadowmapped = isShadowmapped;
				this.useNDotL = useNDotL;

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
		private Matrix[][] lightShadowmappedMatrices = new Matrix[LightsShadowmappedMax][];

		private Light[] lights = new Light[LightsMax];
		private Light[] lightsShadowmapped = new Light[LightsShadowmappedMax];

		private ushort[] lightVersions = new ushort[LightsShadowmappedMax];
		private ushort[] oldLightVersions = new ushort[LightsShadowmappedMax];
		private int[] oldShadowmapVersions = new int[LightsShadowmappedMax];

		private Data[] datas = new Data[LightsMax];
		private Data[] datasShadowmapped = new Data[LightsShadowmappedMax];

		private int numUsedLights;
		private int numUsedLightsShadowmapped;

		public readonly int generation;

		public LightManager(GraphicsDevice device)
		{
			//lightsCubemaps = new RenderTarget2D(device, 256, 256, false, SurfaceFormat.Single, DepthFormat.Depth24, 0, RenderTargetUsage.PreserveContents, false, 
				//MAX_LIGHTS_SHADOWMAPPED * 6);
			lightsCubemaps = new RenderTargetCube(device, 256, false, SurfaceFormat.Single, DepthFormat.Depth24Stencil8, 0, RenderTargetUsage.PreserveContents, LightsShadowmappedMax);

			for (int i = 0; i < LightsShadowmappedMax; i++)
			{
				lightShadowmappedMatrices[i] = new Matrix[6];
				Array.Fill(lightShadowmappedMatrices[i], Matrix.Identity);

				datasShadowmapped[i] = new Data(new Light());
			}

			generation = LightManagerGeneration;
			/*for (int i = 0; i < MAX_LIGHTS_SHADOWMAPPED; i++)
				lightsCubemaps[i] = new RenderTargetCube(device, 256, false, SurfaceFormat.Single, DepthFormat.Depth24);*/
		}

		public Light Get(int index)
        {
			return lights[index];
        }

		public Light GetShadowmapped(int index)
        {
			return lightsShadowmapped[index];
        }

		public int Add(Vector3 position, float start, float end, Vector4 color, bool useNDotL = true)
		{
			//Early-out - we have no more light slots available.
			if (numUsedLights >= LightsMax)
				return -1;

			for (int i = 0; i < LightsMax; i++)
			{
				if (!lights[i].active)
				{
					lights[i] = new Light(position, start, end, color, true, useNDotL, i);

					version++;

					numUsedLights++;

					return i;
				}
			}

			return -1;
		}

		public void AddShadowmapped(Vector3 position, float start, float end, Vector4 color, out int shadowmappedLightIndex, out bool success)
		{
			//Attempt to allocate a non-shadowmapped light if we're above the max shadowmapped lights.
			if (numUsedLightsShadowmapped >= LightsShadowmappedMax)
            {
				shadowmappedLightIndex = Add(position, start, end, color);
				success = false;
				return;
            }

			for (int i = 0; i < LightsShadowmappedMax; i++)
			{
				if (!lightsShadowmapped[i].active)
				{
					lightsShadowmapped[i] = new Light(position, start, end, color, true, true, i);

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

		public void Update(int index, Vector3 position, float start, float end, Vector4 color)
        {
			if (lights[index].active)
            {
				version++;
				lights[index] = new Light(position, start, end, color, false, true, index);
            }
        }

		public void UpdateShadowmapped(int index, Vector3 position, float start, float end, Vector4 color, bool markDirty = false)
		{
			if (lightsShadowmapped[index].active)
			{
				version++;
				lightsShadowmapped[index] = new Light(position, start, end, color, true, true, index);

				if (markDirty)
					MarkDirty(index);
			}
		}

		//Marks the shadowmapped light as dirty, forcing it to be re-calculated.
		public void MarkDirty(int index)
        {
			lightVersions[index]++;
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
				bufferLights = new StructuredBuffer(effect.GraphicsDevice, typeof(Data), LightsMax, BufferUsage.WriteOnly, ShaderAccess.Read);

			if (bufferShadowmappedLights == null)
				bufferShadowmappedLights = new StructuredBuffer(effect.GraphicsDevice, typeof(Data), LightsShadowmappedMax, BufferUsage.WriteOnly, ShaderAccess.Read);

			//if version does not match last version, that means something was added and we need to reupload datas.
			//for now, this happens if either normal or shadowmapped lights are updated.
			if (version != lastUploadedVersion)
			{
				lastUploadedVersion = version;

				for (int i = 0; i < LightsMax; i++)
				{
					datas[i] = new Data(lights[i]);
				}

				for (int i = 0; i < LightsShadowmappedMax; i++)
				{
					if (lightsShadowmapped[i].active)
					{
						Matrix proj = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(90f), 1, 0.001f, lightsShadowmapped[i].end);
						//proj *= Matrix.CreateScale(-1, 1, -1);

						lightShadowmappedMatrices[i][0] = Matrix.CreateLookAt(lightsShadowmapped[i].position, lightsShadowmapped[i].position + new Vector3(1, 0, 0), new Vector3(0, -1, 0)) * proj;
						lightShadowmappedMatrices[i][1] = Matrix.CreateLookAt(lightsShadowmapped[i].position, lightsShadowmapped[i].position + new Vector3(-1, 0, 0), new Vector3(0, -1, 0)) * proj;
						lightShadowmappedMatrices[i][2] = Matrix.CreateLookAt(lightsShadowmapped[i].position, lightsShadowmapped[i].position + new Vector3(0, -1, 0), new Vector3(0, 0, -1)) * proj;
						lightShadowmappedMatrices[i][3] = Matrix.CreateLookAt(lightsShadowmapped[i].position, lightsShadowmapped[i].position + new Vector3(0, 1, 0), new Vector3(0, 0, 1)) * proj;
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

		[Obsolete()]
		public void Draw(GraphicsDevice device)
        {
			//for (int i = 0; i < LightsShadowmappedMax; i++)
   //         {
			//	Light light = lights[i];

			//	if (light.active)
   //                 client.Renderer.DrawsPointLightVolumePass.Add(new RendererDeferred.PointLightVolumeDraw(i, light.position, light.end));

			//	if (DebugDrawLightInstanceVolumes)
			//	{
   //                 client.Renderer.DEBUGMarkersSphere.Add(new RendererDeferred.DEBUGDraw
			//		{
			//			Color = Color.Red * 0.2f,
			//			Position = light.position,
			//			Scale = new Vector3(light.end),
			//		});
			//	}
   //         }

			//for (int i = 0; i < LightsShadowmappedMax; i++)
   //         {
			//	Light light = lightsShadowmapped[i];
				
			//	if (light.active)
			//		Main.Renderer.DrawsShadowmappedPointLightVolumePass.Add(new RendererDeferred.PointLightVolumeDraw(i, light.position, light.end, null));

   //             if (DebugDrawLightInstanceVolumes)
   //             {
   //                 Main.Renderer.DEBUGMarkersSphere.Add(new RendererDeferred.DEBUGDraw
   //                 {
   //                     Color = Color.Orange * 0.2f,
   //                     Position = light.position,
   //                     Scale = new Vector3(light.end),
   //                 });
   //             }
   //         }
        }

		private ChunkPosition[] drawnChunks = new ChunkPosition[9 * 9 * 9];

		public void DrawShadowmap(GraphicsDevice device, ClientChunkManager chunkManager)
		{
			if (!Main.ENABLE_SHADOWS)
			{
				return;
			}

			device.DepthStencilState = dss;
			device.RasterizerState = rs;

			Effect effectDepth = Main.assetsManager.GetAsset<Effect>("depth_pointlight");

			bool anyDrawn = false;

			//To begin with, draw everything every frame. This is SLOW! Eventually we'll want to only draw these lights
			//if something changes in them (i.e. chunk is dirty)
			for (int i = 0; i < LightsShadowmappedMax; i++)
			{
				Light light = lightsShadowmapped[i];

				if (light.active)
				{
					//We can determine the version of a chunk (whether or not it's changed from the last frame, compared to what the light knows)
					//by accumulating the hash code of the chunk reference itself + a version number (which is incremented each time the chunk is meshed).
					int versionSum = 0;
					int drawnChunksCount = 0;
					//TODO: fit drawn chunks more accurately. Right now we're drawing tons of unseen stuff
					int drawDist = (int)MathF.Round((light.end / ViMG.Cubes.Cube.CUBE_SCALE) / Chunk.CHUNK_SIZE, MidpointRounding.ToPositiveInfinity);
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

								drawnChunks[drawnChunksCount++] = chunkPos;

								if (chunkManager.IsInWorldBounds(chunkPos))
								{
									versionSum += chunkManager.ChunkMesher?.RenderMesher?.GetMeshVersionCode(chunkPos) ?? 0;
								}
							}
						}
					}

					//Redraw the shadowmap if either:
					//The light itself has changed
					//Or the world around it has changed.
					if (lightVersions[i] != oldLightVersions[i] || versionSum != oldShadowmapVersions[i])
					{
						oldLightVersions[i] = lightVersions[i];
						oldShadowmapVersions[i] = versionSum;

						anyDrawn = true;
						effectDepth.Parameters["LightPosition"].SetValue(light.position);
						effectDepth.Parameters["FarPlane"].SetValue(light.end);
						effectDepth.Parameters["World"].SetValue(Matrix.Identity);	//we never use anything other than Identity for chunks

						for (int j = 0; j < 6; j++)
						{
							Matrix viewProj = lightShadowmappedMatrices[i][j];
							effectDepth.Parameters["ViewProjection"].SetValue(viewProj);

							device.SetRenderTarget(lightsCubemaps, (CubeMapFace)j, i);
							//device.SetRenderTarget(lightsCubemaps, i * 6 + j);
							device.Clear(ClearOptions.Target | ClearOptions.DepthBuffer | ClearOptions.Stencil, Color.White, device.Viewport.MaxDepth, 0);

							for (int k = 0; k < drawnChunksCount; k++)
							{
								ChunkPosition chunkPos = drawnChunks[k];

								if (chunkManager.IsInWorldBounds(chunkPos))
								{
									VerySimpleMesh mesh = chunkManager.ChunkMesher?.RenderMesher?.GetMesh(chunkPos, ViMG.Cubes.Cube.RenderPass.DepthOnly) ?? new();
                                    //Matrix transform = world.ChunkManager.GetTransform(chunkPos);


                                    if (mesh.VBOPosition != null && mesh.VBOTexCoord != null)
                                    {
                                        device.SetVertexBuffers(new VertexBufferBinding(mesh.VBOPosition, 0), new VertexBufferBinding(mesh.VBOTexCoord, 0));
                                        device.Indices = mesh.IBO;

                                        foreach (var pass in effectDepth.CurrentTechnique.Passes)
                                        {
                                            pass.Apply();
                                            device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, mesh.IBO.IndexCount / 3);
                                        }
                                    }
                                }
							}
						}
					}
				}
			}

			//if (anyDrawn)
			//	Main.Renderer.EffectLightAccumPointLight.Parameters["Cubemaps"].SetValue(lightsCubemaps);
		}

		public void Dispose()
        {
			lightsCubemaps?.Dispose();
			bufferLights?.Dispose();
			bufferShadowmappedLights?.Dispose();
        }
	}
}
