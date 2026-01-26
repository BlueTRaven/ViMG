using Engine.Clients;
using Engine.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG;
using ViMG.Rendering;

namespace Engine.Rendering
{
    public class LightsRenderer
    {
        public readonly struct Data
        {
            public readonly Vector4 color;
            public readonly Vector3 position;
            public readonly float start;
            public readonly float end;

            public readonly float useNDotL;

            public Data(LightManager2.Light light, float intensity = 1)
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

        private StructuredBuffer? bufferLights;
        private StructuredBuffer? bufferShadowmappedLights;
        private RenderTargetCube lightsCubemaps;

        private Data[] datas = new Data[LightManager.LightsMax];
        private Data[] datasShadowmapped = new Data[LightManager.LightsShadowmappedMax];
        private Matrix[][] lightShadowmappedMatrices = new Matrix[LightManager.LightsShadowmappedMax][];

        public LightsRenderer(GraphicsDevice device)
        {
            lightsCubemaps = new RenderTargetCube(device, 256, false, SurfaceFormat.Single, DepthFormat.Depth24Stencil8, 0, RenderTargetUsage.PreserveContents, LightManager.LightsShadowmappedMax);

            for (int i = 0; i < LightManager.LightsShadowmappedMax; i++)
            {
                lightShadowmappedMatrices[i] = new Matrix[6];
                Array.Fill(lightShadowmappedMatrices[i], Matrix.Identity);

                datasShadowmapped[i] = new Data(new LightManager2.Light());
            }
        }

        public void UpdateDatas(LightManager2 lightManager, Effect effect)
        {
            if (bufferLights == null)
                bufferLights = new StructuredBuffer(effect.GraphicsDevice, typeof(Data), LightManager.LightsMax, BufferUsage.WriteOnly, ShaderAccess.Read);

            if (bufferShadowmappedLights == null)
                bufferShadowmappedLights = new StructuredBuffer(effect.GraphicsDevice, typeof(Data), LightManager.LightsShadowmappedMax, BufferUsage.WriteOnly, ShaderAccess.Read);

            for (int i = 0; i < LightManager.LightsMax; i++)
            {
                datas[i] = new Data(lightManager.Get(i));
            }

            for (int i = 0; i < LightManager.LightsShadowmappedMax; i++)
            {
                LightManager2.Light light = lightManager.GetShadowmapped(i);
                if (light.active)
                {
                    Matrix proj = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(90f), 1, 0.001f, light.end);
                    //proj *= Matrix.CreateScale(-1, 1, -1);

                    lightShadowmappedMatrices[i][0] = Matrix.CreateLookAt(light.position, light.position + new Vector3(1, 0, 0), new Vector3(0, -1, 0)) * proj;
                    lightShadowmappedMatrices[i][1] = Matrix.CreateLookAt(light.position, light.position + new Vector3(-1, 0, 0), new Vector3(0, -1, 0)) * proj;
                    lightShadowmappedMatrices[i][2] = Matrix.CreateLookAt(light.position, light.position + new Vector3(0, -1, 0), new Vector3(0, 0, -1)) * proj;
                    lightShadowmappedMatrices[i][3] = Matrix.CreateLookAt(light.position, light.position + new Vector3(0, 1, 0), new Vector3(0, 0, 1)) * proj;
                    lightShadowmappedMatrices[i][4] = Matrix.CreateLookAt(light.position, light.position + new Vector3(0, 0, 1), new Vector3(0, -1, 0)) * proj;
                    lightShadowmappedMatrices[i][5] = Matrix.CreateLookAt(light.position, light.position + new Vector3(0, 0, -1), new Vector3(0, -1, 0)) * proj;
                }
                else
                {
                    Array.Fill(lightShadowmappedMatrices[i], Matrix.Identity);
                }

                datasShadowmapped[i] = new Data(light);
            }

            bufferLights.SetData(datas);
            bufferShadowmappedLights.SetData(datasShadowmapped);

            effect.Parameters["Lights"].SetValue(bufferLights);
            effect.Parameters["ShadowmappedLights"].SetValue(bufferShadowmappedLights);
        }

        public void Draw(GraphicsDevice device, RendererDeferred renderer, LightManager2 lightManager)
        {
            for (int i = 0; i < LightManager.LightsShadowmappedMax; i++)
            {
                LightManager2.Light light = lightManager.Get(i);

                if (light.active)
                    renderer.DrawsPointLightVolumePass.Add(new RendererDeferred.PointLightVolumeDraw(i, light.position, light.end));

                if (LightManager.DebugDrawLightInstanceVolumes)
                {
                    renderer.DEBUGMarkersSphere.Add(new RendererDeferred.DEBUGDraw
                    {
                        Color = Color.Red * 0.2f,
                        Position = light.position,
                        Scale = new Vector3(light.end),
                    });
                }
            }

            for (int i = 0; i < LightManager.LightsShadowmappedMax; i++)
            {
                LightManager2.Light light = lightManager.GetShadowmapped(i);

                if (light.active)
                    renderer.DrawsShadowmappedPointLightVolumePass.Add(new RendererDeferred.PointLightVolumeDraw(i, light.position, light.end, null));

                if (LightManager.DebugDrawLightInstanceVolumes)
                {
                    renderer.DEBUGMarkersSphere.Add(new RendererDeferred.DEBUGDraw
                    {
                        Color = Color.Orange * 0.2f,
                        Position = light.position,
                        Scale = new Vector3(light.end),
                    });
                }
            }
        }

        private ChunkPosition[] drawnChunks = new ChunkPosition[9 * 9 * 9];

        public void DrawShadowmap(GraphicsDevice device, RendererDeferred renderer, ClientChunkManager chunkManager, LightManager2 lightManager)
        {
            if (!Main.ENABLE_SHADOWS)
            {
                return;
            }

            device.DepthStencilState = dss;
            device.RasterizerState = rs;

            Effect effectDepth = GlobalState.assetsManager.GetAsset<Effect>("depth_pointlight");

            bool anyDrawn = false;

            //To begin with, draw everything every frame. This is SLOW! Eventually we'll want to only draw these lights
            //if something changes in them (i.e. chunk is dirty)
            for (int i = 0; i < LightManager.LightsShadowmappedMax; i++)
            {
                LightManager2.Light light = lightManager.GetShadowmapped(i);

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

                    anyDrawn = true;
                    if (lightManager.GetShadowmappedLightDirty(i))
                    {
                        effectDepth.Parameters["LightPosition"].SetValue(light.position);
                        effectDepth.Parameters["FarPlane"].SetValue(light.end);
                        effectDepth.Parameters["World"].SetValue(Matrix.Identity);  //we never use anything other than Identity for chunks

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

            if (anyDrawn)
                renderer.EffectLightAccumPointLight.Parameters["Cubemaps"].SetValue(lightsCubemaps);
        }

        public void Dispose()
        {
            lightsCubemaps?.Dispose();
            bufferLights?.Dispose();
            bufferShadowmappedLights?.Dispose();
        }
    }
}
