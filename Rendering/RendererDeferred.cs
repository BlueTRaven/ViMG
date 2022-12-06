using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SMAADemo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViMG.Rendering
{
    public class RendererDeferred
    {
        public struct PointLightVolumeDraw
        {
            public int LightIndex;
            public Vector3 LightPosition;
            public float LightScale;  //In other words, its End value

            public RenderTargetCube Cubemap;

            public PointLightVolumeDraw(int index, Vector3 position, float scale, RenderTargetCube cubemap = null)
            {
                this.LightIndex = index;
                this.LightPosition = position;
                this.LightScale = scale;

                this.Cubemap = cubemap;
            }
        }

        public struct GBufferDraw
        {
            public Texture2D Diffuse;
            public Texture2D Specular;
            public Texture2D Emissive;

            public VertexBuffer VBO;
            public IndexBuffer IBO;

            public Matrix World;
            public Matrix WorldNormal;

            public bool UseSourceRect;
            public Vector2 SourceRectPos;
            public Vector2 SourceRectFarPos;
            public Vector2 TextureSize;

            public Vector3 TintColor;

            public GBufferDraw(Texture2D diffuse, Texture2D specular, Texture2D emissive, VertexBuffer VBO, IndexBuffer IBO, Matrix world, RectangleF? sourceRect = null, Vector3? tintColor = null)
            {
                this.Diffuse = diffuse;
                this.Specular = specular;
                this.Emissive = emissive;
                this.VBO = VBO;
                this.IBO = IBO;
                this.World = world;
                this.WorldNormal = Matrix.Transpose(Matrix.Invert(world));
                this.TintColor = tintColor.GetValueOrDefault(Color.White.ToVector3());

                if (sourceRect != null)
                {
                    RectangleF rect = sourceRect.Value;

                    UseSourceRect = true;
                    SourceRectPos = rect.Position;
                    SourceRectFarPos = rect.FarPosition;
                }
                else
                {
                    UseSourceRect = false;
                    SourceRectPos = new Vector2();
                    SourceRectFarPos = new Vector2();
                }
                
                TextureSize = new Vector2(diffuse.Width, diffuse.Height);
            }
        }

        public struct TransparentDraw
        {
            public float SortValue;
            public Matrix Transform;
            public Texture2D Diffuse;
            public Texture2D Emissive;
            public VertexBuffer VBO;
            public IndexBuffer IBO;

            public bool UseSourceRect;
            public Vector2 SourceRectPos;
            public Vector2 SourceRectFarPos;
            public Vector2 TextureSize;

            public Vector4 TintColor;

            public TransparentDraw(float sortValue, Matrix transform, Texture2D diffuse, Texture2D emissive, VertexBuffer vbo, IndexBuffer ibo, RectangleF? sourceRect = null, Color? tintColor = null)
            {
                this.SortValue = sortValue;
                this.Transform = transform;
                this.Diffuse = diffuse;
                this.Emissive = emissive;
                this.VBO = vbo;
                this.IBO = ibo;

                if (sourceRect != null)
                {
                    RectangleF rect = sourceRect.Value;

                    UseSourceRect = true;
                    SourceRectPos = rect.Position;
                    SourceRectFarPos = rect.FarPosition;
                }
                else
                {
                    UseSourceRect = false;
                    SourceRectPos = new Vector2();
                    SourceRectFarPos = new Vector2();
                }

                if (tintColor == null)
                    TintColor = Color.White.ToVector4();
                else TintColor = tintColor.Value.ToVector4();

                TextureSize = new Vector2(diffuse.Width, diffuse.Height);
            }
        }

        public struct DEBUGDraw
        {
            public Vector3 Position;
            public Vector3 Scale;
            public Color Color;
        }

        private readonly GraphicsDevice device;

        private RenderTarget2D diffuse;       //RGB albedo data; A specular data
        private RenderTarget2D lightAccum;  //RGB ambient + emissive to begin with. Light is accumulated after gbuffer pass.
        private RenderTarget2D depth;       //R depth data
        private RenderTarget2D position;    //RGB position data; A unused
        private RenderTarget2D normal;      //RGB normal data; A unused
        private RenderTarget2D ao;          //R AO data

        private RenderTarget2D preTransparencyOutput;
        private RenderTarget2D ldrOutputPing;
        private RenderTarget2D ldrOutputPong;

        private RenderTarget2D outputRT;

        //SetRenderTargets uses params, which constructs an implicit array every time it's called,
        //which is an allocation every frame. Don't do that. Just allocate one to start with...
        private RenderTargetBinding[] targets;
        private RendererBloom bloom;
        private SMAA smaa;
        private RendererFXAA fxaa;
        
        private int currentOutput = -1;

        public Effect EffectGBuffer;
        public Effect EffectLightAccumCSM;
        public Effect EffectLightAccumPointLight;
        public Effect EffectDeferred;
        public Effect EffectTransparent;
        public bool EffectEmptyEnabled;
        public Effect EffectEmpty;
        public Effect EffectHDR;
        public Effect EffectRadialFog;

        public Effect EffectFXAA;

        private Effect DEBUGEffectVisualizeCubemap;

        private BasicEffect EffectCopy;
        private SamplerState shadowBorderClampSS;
        private SamplerState bilinearClampSS;
        private BlendState noAlphaBlendBS;
        private BlendState normalBS;
        private BlendState additiveBS;
        private RasterizerState cullCWRS;
        private RasterizerState cullCCWRS;
        private DepthStencilState depthReadNoWriteDSS;
        private DepthStencilState noDepthReadWriteDSS;

        private VertexBuffer vboQuad;
        private IndexBuffer iboQuad;
        private VertexBuffer vboUVSphere;
        private IndexBuffer iboUVSphere;

        private uint[] lightVolumeIndices = new uint[LightManager.MAX_LIGHTS];
        private StructuredBuffer bufferLightVolumeIndices;

        public List<GBufferDraw> DrawsPassGBuffer = new List<GBufferDraw>();
        public List<PointLightVolumeDraw> DrawsPointLightVolumePass = new List<PointLightVolumeDraw>();
        public List<PointLightVolumeDraw> DrawsShadowmappedPointLightVolumePass = new List<PointLightVolumeDraw>();
        public List<TransparentDraw> DrawsTransparentPass = new List<TransparentDraw>();
        public List<TransparentDraw> DrawsEmptyPass = new List<TransparentDraw>();
        //Note that DEBUG markers ARE NOT RESET EVERY FRAME.
        //If you want to add a different type of data, RESET THEM YOURSELF!
        public List<DEBUGDraw> DEBUGMarkersSphere = new List<DEBUGDraw>();
        public List<DEBUGDraw> DEBUGMarkersRect = new List<DEBUGDraw>();

        public static int NumPointLightsRendered;
        public static int NumDrawCalls;

        public bool DoCSMLight = true;
        private float alive;

        private Options.SMAAQuality previousSMAAOption;
        private Options.FXAAQuality previousFXAAOption;

        public (VertexBuffer VBO, IndexBuffer IBO) DEBUGCubemapMesh;
        public (VertexBuffer VBO, IndexBuffer IBO) DEBUGSphereMesh;
        public (VertexBuffer VBO, IndexBuffer IBO) DEBUGCubeMesh;

        public RendererDeferred(GraphicsDevice device)
        {
            DEBUGCubemapMesh = MeshHelper.MakeCubemap(device, -Vector3.One, Vector3.One);
            DEBUGSphereMesh = DrawHelper3D.MakeUVSphere(device, Cubes.Cube.CUBE_SCALE);

            List<VertexCube> cubeVertices = new List<VertexCube>();
            List<int> cubeIndices = new List<int>();
            MeshHelper.MakeCubeVertsVertexPositionColorTextureNormal(-Vector3.One / 2f, Vector3.One / 2f, MeshHelper.CubeFace.ALL, Color.White, cubeVertices, cubeIndices);
            DEBUGCubeMesh = MeshHelper.MakeSimplerMesh(device, cubeVertices, cubeIndices);

            bloom = new RendererBloom(device);

            EffectCopy = new BasicEffect(device);
            EffectCopy.TextureEnabled = true;
            EffectCopy.VertexColorEnabled = false;
            EffectCopy.FogEnabled = false;
            EffectCopy.LightingEnabled = false;

            cullCCWRS = new RasterizerState()
            {
                CullMode = CullMode.CullCounterClockwiseFace,
            };

            cullCWRS = new RasterizerState()
            {
                CullMode = CullMode.CullClockwiseFace,
            };

            shadowBorderClampSS = new SamplerState()
            {
                AddressU = TextureAddressMode.Border,
                AddressV = TextureAddressMode.Border,
                BorderColor = Color.Black,
                Filter = TextureFilter.Point,
                MaxMipLevel = 0,
                MaxAnisotropy = 0,
            };

            bilinearClampSS = SamplerState.LinearClamp;

            noAlphaBlendBS = BlendState.Opaque;
            normalBS = BlendState.AlphaBlend;
            additiveBS = BlendState.Additive;

            depthReadNoWriteDSS = DepthStencilState.DepthRead;
            noDepthReadWriteDSS = DepthStencilState.None;

            this.device = device;

            ConstructRTs(Options.CurrentWindowResolution);

            EffectGBuffer = Main.assetsManager.GetAsset<Effect>("deferred_gbuffer");
            EffectDeferred = Main.assetsManager.GetAsset<Effect>("deferred");
            EffectLightAccumCSM = Main.assetsManager.GetAsset<Effect>("deferred_lightaccum_csmlight");
            EffectLightAccumPointLight = Main.assetsManager.GetAsset<Effect>("deferred_lightaccum_pointlight");
            EffectTransparent = Main.assetsManager.GetAsset<Effect>("transparent");
            EffectEmpty = Main.assetsManager.GetAsset<Effect>("air");
            EffectHDR = Main.assetsManager.GetAsset<Effect>("hdr");
            EffectFXAA = Main.assetsManager.GetAsset<Effect>("fxaa");
            EffectRadialFog = Main.assetsManager.GetAsset<Effect>("radial_fog");

            DEBUGEffectVisualizeCubemap = Main.assetsManager.GetAsset<Effect>("visualize_cubemap");

            EffectGBuffer.Parameters["AmbientStrength"].SetValue(0.1f);
            EffectGBuffer.Parameters["SpecularPower"].SetValue(4);

            Main.WindowResizedEvent += ConstructRTs;

            VertexPositionTexture[] vpt = new VertexPositionTexture[4]
            {
                new VertexPositionTexture(new Vector3(-1, 1, 0), new Vector2(0, 0)),
                new VertexPositionTexture(new Vector3(1, 1, 0), new Vector2(1, 0)),
                new VertexPositionTexture(new Vector3(1, -1, 0), new Vector2(1, 1)),
                new VertexPositionTexture(new Vector3(-1, -1, 0), new Vector2(0, 1)),
            };

            uint[] indices = new uint[6]
            {
                0, 1, 2,
                2, 3, 0,
            };

            vboQuad = new VertexBuffer(device, typeof(VertexPositionTexture), vpt.Length, BufferUsage.WriteOnly);
            vboQuad.SetData(vpt);

            iboQuad = new IndexBuffer(device, typeof(uint), 6, BufferUsage.WriteOnly);
            iboQuad.SetData(indices);

            (vboUVSphere, iboUVSphere) = DrawHelper3D.MakeUVSphere(device, 1);

            bufferLightVolumeIndices = new StructuredBuffer(device, typeof(uint), lightVolumeIndices.Length, BufferUsage.WriteOnly, ShaderAccess.Read);
            EffectLightAccumPointLight.Parameters["LightInstanceIndices"].SetValue(bufferLightVolumeIndices);
        }

        public void FrameStart()
        {
            DrawsPassGBuffer.Clear();
            DrawsPointLightVolumePass.Clear();
            DrawsShadowmappedPointLightVolumePass.Clear();
            DrawsTransparentPass.Clear();
            DrawsEmptyPass.Clear();

            NumDrawCalls = 0;
            NumPointLightsRendered = 0;
        }

        private void ConstructRTs(Point rez)
        {
            ConstructSMAA(rez);
            ConstructFXAA(rez);

            diffuse?.Dispose();
            lightAccum?.Dispose();
            depth?.Dispose();
            position?.Dispose();
            normal?.Dispose();
            ao?.Dispose();

            diffuse = new RenderTarget2D(device, rez.X, rez.Y, false, SurfaceFormat.HalfVector4, DepthFormat.Depth24Stencil8, 0, RenderTargetUsage.PreserveContents);
            diffuse.Name = "Diffuse";
            lightAccum = new RenderTarget2D(device, rez.X, rez.Y, false, SurfaceFormat.HalfVector4, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            lightAccum.Name = "Light Accumulation";
            depth = new RenderTarget2D(device, rez.X, rez.Y, false, SurfaceFormat.Single, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            depth.Name = "Depth";
            //TODO get rid of; use inverse wvp + depth to calculate
            position = new RenderTarget2D(device, rez.X, rez.Y, false, SurfaceFormat.Vector4, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            position.Name = "Position (World Space)";
            normal = new RenderTarget2D(device, rez.X, rez.Y, false, SurfaceFormat.HalfVector4, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            normal.Name = "Normal (World Space)";
            ao = new RenderTarget2D(device, rez.X, rez.Y, false, SurfaceFormat.Single, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            ao.Name = "AO";

            //Note that diffuse must be first.
            //This is because Monogame outputs to the depth buffer of the first render target.
            targets = new RenderTargetBinding[]
            {
                diffuse,
                lightAccum,
                depth,
                position,
                normal,
                ao,
            };

            preTransparencyOutput?.Dispose();
            ldrOutputPing?.Dispose();
            ldrOutputPong?.Dispose();

            preTransparencyOutput = new RenderTarget2D(device, rez.X, rez.Y, false, SurfaceFormat.HalfVector4, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            ldrOutputPing = new RenderTarget2D(device, rez.X, rez.Y, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            ldrOutputPong = new RenderTarget2D(device, rez.X, rez.Y, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
        }

        private void ConstructSMAA(Point rez)
        {
            smaa?.Dispose();

            switch (Options.CurrentAntiAliasing)
            {
                case Options.AntiAliasing.SMAA:
                    switch (Options.CurrentSMAAQuality)
                    {
                        case Options.SMAAQuality.SMAA_ULTRA:
                            smaa = new SMAA(device, rez.X, rez.Y, SMAA.Preset.ULTRA);
                            previousSMAAOption = Options.SMAAQuality.SMAA_ULTRA;
                            break;
                        case Options.SMAAQuality.SMAA_HIGH:
                            smaa = new SMAA(device, rez.X, rez.Y, SMAA.Preset.HIGH);
                            previousSMAAOption = Options.SMAAQuality.SMAA_HIGH;
                            break;
                        case Options.SMAAQuality.SMAA_MEDIUM:
                            smaa = new SMAA(device, rez.X, rez.Y, SMAA.Preset.MEDIUM);
                            previousSMAAOption = Options.SMAAQuality.SMAA_MEDIUM;
                            break;
                        case Options.SMAAQuality.SMAA_LOW:
                            smaa = new SMAA(device, rez.X, rez.Y, SMAA.Preset.LOW);
                            previousSMAAOption = Options.SMAAQuality.SMAA_LOW;
                            break;
                    }
                    break;
                default:
                    previousSMAAOption = Options.SMAA_INVALID;    
                    break;
            }
        }

        private void ConstructFXAA(Point rez)
        {
            fxaa?.Dispose();

            if (Options.CurrentAntiAliasing == Options.AntiAliasing.FXAA)
            {
                fxaa = new RendererFXAA(device, rez);
                previousFXAAOption = Options.CurrentFXAAQuality;
            }
            else
            {
                previousFXAAOption = Options.FXAA_INVALID;
            }
        }

        public void SetPipelineState()
        {
            device.RasterizerState = Main.genericRS;
            device.BlendState = noAlphaBlendBS;
            device.DepthStencilState = DepthStencilState.Default;
        }

        public void Update(double deltaTime)
        {
            alive += (float)deltaTime;

            EffectGBuffer.Parameters["Time"].SetValue(alive);

            if (Main.inputManager.JustPressed(Microsoft.Xna.Framework.Input.Keys.OemOpenBrackets))
            {
                currentOutput--;
                if (currentOutput < -1)
                    currentOutput = targets.Length - 1;
            }

            if (Main.inputManager.JustPressed(Microsoft.Xna.Framework.Input.Keys.OemCloseBrackets))
            {
                currentOutput++;
                if (currentOutput > targets.Length - 1)
                    currentOutput = -1;
            }
        }

        public void Draw(SpriteBatch batch)
        {
            SetPipelineState();

            if (Options.CurrentSMAAQuality != previousSMAAOption)
                ConstructSMAA(Options.CurrentWindowResolution);

            if (Options.CurrentFXAAQuality != previousFXAAOption)
                ConstructFXAA(Options.CurrentWindowResolution);

            device.SetRenderTargets(targets);
            device.Clear(ClearOptions.DepthBuffer | ClearOptions.Target, Color.Transparent, device.Viewport.MaxDepth, 0);

            if (DrawsPassGBuffer.Count > 0)
            {
                Matrix viewProjection = Main.camera.GetViewMatrix() * Main.camera.GetProjectionMatrix();
                EffectGBuffer.Parameters["View"].SetValue(Main.camera.GetViewMatrix());
                EffectGBuffer.Parameters["ViewProjection"].SetValue(viewProjection);
                EffectGBuffer.Parameters["InvViewProjection"].SetValue(Matrix.Invert(viewProjection));

                device.SamplerStates[1] = bilinearClampSS;

                foreach (GBufferDraw draw in DrawsPassGBuffer)
                {
                    if (draw.VBO != null && draw.IBO != null)
                    {
                        device.SetVertexBuffer(draw.VBO);
                        device.Indices = draw.IBO;

                        EffectGBuffer.Parameters["World"].SetValue(draw.World);
                        EffectGBuffer.Parameters["WorldNormal"].SetValue(Matrix.Transpose(Matrix.Invert(draw.World)));

                        EffectGBuffer.Parameters["Diffuse"].SetValue(draw.Diffuse);
                        EffectGBuffer.Parameters["Specular"].SetValue(draw.Specular);
                        EffectGBuffer.Parameters["Emissive"].SetValue(draw.Emissive);

                        EffectGBuffer.Parameters["TintColor"].SetValue(draw.TintColor);

                        if (draw.UseSourceRect)
                        {
                            EffectGBuffer.Parameters["UseSourceRect"].SetValue(true);
                            EffectGBuffer.Parameters["SourceRectPos"].SetValue(draw.SourceRectPos);
                            EffectGBuffer.Parameters["SourceRectFarPos"].SetValue(draw.SourceRectFarPos);
                        }
                        else EffectGBuffer.Parameters["UseSourceRect"].SetValue(false);
                        
                        EffectGBuffer.Parameters["TextureSize"].SetValue(draw.Diffuse.Bounds.Size.ToVector2());

                        foreach (var pass in EffectGBuffer.CurrentTechnique.Passes)
                        {
                            pass.Apply();
                            device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, draw.IBO.IndexCount / 3);

                            NumDrawCalls++;
                        }
                    }
                }
            }

            device.SetVertexBuffer(vboQuad);
            device.Indices = iboQuad;

            device.SetRenderTarget(lightAccum);

            if (DoCSMLight)
            {
                EffectLightAccumCSM.Parameters["Position"].SetValue(position);
                EffectLightAccumCSM.Parameters["Depth"].SetValue(depth);
                EffectLightAccumCSM.Parameters["Normal"].SetValue(normal);
                EffectLightAccumCSM.Parameters["CameraPosition"].SetValue(Main.camera.Position);
                device.SamplerStates[1] = shadowBorderClampSS;
                device.BlendState = additiveBS;

                DrawFullscreenQuad(EffectLightAccumCSM);

                NumDrawCalls++;
            }

            if (DrawsPointLightVolumePass.Count > 0)
            {
                device.SetRenderTarget(lightAccum);
                device.RasterizerState = cullCWRS;
                device.SamplerStates[1] = shadowBorderClampSS;
                device.BlendState = additiveBS;
                device.DepthStencilState = depthReadNoWriteDSS;

                EffectLightAccumPointLight.Parameters["Position"].SetValue(position);
                //EffectLightAccumPointLight.Parameters["Depth"].SetValue(depth);
                EffectLightAccumPointLight.Parameters["Normal"].SetValue(normal);
                //EffectLightAccumPointLight.Parameters["Diffuse"].SetValue(diffuse);
                EffectLightAccumPointLight.Parameters["CameraPosition"].SetValue(Main.camera.Position);

                Matrix viewProj = Main.camera.GetViewMatrix() * Main.camera.GetProjectionMatrix();

                EffectLightAccumPointLight.Parameters["ViewProjection"].SetValue(viewProj);
                EffectLightAccumPointLight.Parameters["UseInstancing"].SetValue(Options.UseInstancedLightVolumes);
                EffectLightAccumPointLight.Parameters["UseShadowmap"].SetValue(false);

                device.SetVertexBuffer(vboUVSphere);
                device.Indices = iboUVSphere;

                if (!Options.UseInstancedLightVolumes)
                {
                    foreach (PointLightVolumeDraw draw in DrawsPointLightVolumePass)
                    {
                        EffectLightAccumPointLight.Parameters["LightIndex"].SetValue(draw.LightIndex);

                        foreach (var pass in EffectLightAccumPointLight.CurrentTechnique.Passes)
                        {
                            pass.Apply();
                            device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, iboUVSphere.IndexCount / 3);
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < DrawsPointLightVolumePass.Count; i++)
                    {
                        PointLightVolumeDraw draw = DrawsPointLightVolumePass[i];

                        lightVolumeIndices[i] = (uint)draw.LightIndex;
                    }

                    bufferLightVolumeIndices.SetData(lightVolumeIndices);

                    foreach (var pass in EffectLightAccumPointLight.CurrentTechnique.Passes)
                    {
                        pass.Apply();
                        device.DrawInstancedPrimitives(PrimitiveType.TriangleList, 0, 0, iboUVSphere.IndexCount / 3, DrawsPointLightVolumePass.Count);
                    }
                }

                NumPointLightsRendered = DrawsPointLightVolumePass.Count;

                device.RasterizerState = cullCCWRS;
            }

            if (DrawsShadowmappedPointLightVolumePass.Count > 0)
            {
                device.SetRenderTarget(lightAccum);
                device.RasterizerState = cullCWRS;
                device.SamplerStates[1] = shadowBorderClampSS;
                device.BlendState = additiveBS;
                device.DepthStencilState = depthReadNoWriteDSS;

                EffectLightAccumPointLight.Parameters["Position"].SetValue(position);
                //EffectLightAccumPointLight.Parameters["Depth"].SetValue(depth);
                EffectLightAccumPointLight.Parameters["Normal"].SetValue(normal);
                //EffectLightAccumPointLight.Parameters["Diffuse"].SetValue(diffuse);
                EffectLightAccumPointLight.Parameters["CameraPosition"].SetValue(Main.camera.Position);

                Matrix viewProj = Main.camera.GetViewMatrix() * Main.camera.GetProjectionMatrix();

                EffectLightAccumPointLight.Parameters["ViewProjection"].SetValue(viewProj);
                EffectLightAccumPointLight.Parameters["UseInstancing"].SetValue(Options.UseInstancedLightVolumes);
                EffectLightAccumPointLight.Parameters["UseShadowmap"].SetValue(true);

                device.SetVertexBuffer(vboUVSphere);
                device.Indices = iboUVSphere;

                if (!Options.UseInstancedLightVolumes)
                {
                    foreach (PointLightVolumeDraw draw in DrawsShadowmappedPointLightVolumePass)
                    {
                        EffectLightAccumPointLight.Parameters["LightIndex"].SetValue(draw.LightIndex);

                        foreach (var pass in EffectLightAccumPointLight.CurrentTechnique.Passes)
                        {
                            pass.Apply();
                            device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, iboUVSphere.IndexCount / 3);
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < DrawsShadowmappedPointLightVolumePass.Count; i++)
                    {
                        PointLightVolumeDraw draw = DrawsShadowmappedPointLightVolumePass[i];

                        lightVolumeIndices[i] = (uint)draw.LightIndex;
                    }

                    bufferLightVolumeIndices.SetData(lightVolumeIndices);

                    foreach (var pass in EffectLightAccumPointLight.CurrentTechnique.Passes)
                    {
                        pass.Apply();
                        device.DrawInstancedPrimitives(PrimitiveType.TriangleList, 0, 0, iboUVSphere.IndexCount / 3, DrawsShadowmappedPointLightVolumePass.Count);
                    }
                }

                NumPointLightsRendered += DrawsShadowmappedPointLightVolumePass.Count;

                device.RasterizerState = cullCCWRS;
            }

            device.SetRenderTarget(preTransparencyOutput);
            device.Clear(ClearOptions.Target, Color.Black, 0, 0);
            device.BlendState = noAlphaBlendBS;

            EffectDeferred.Parameters["Diffuse"].SetValue(diffuse);
            //EffectDeferred.Parameters["Depth"].SetValue(depth);
            //EffectDeferred.Parameters["Position"].SetValue(position);
            EffectDeferred.Parameters["LightAccumulation"].SetValue(lightAccum);
            //EffectDeferred.Parameters["Normal"].SetValue(normal);
            EffectDeferred.Parameters["AO"].SetValue(ao);

            device.SetVertexBuffer(vboQuad);
            device.Indices = iboQuad;
            DrawFullscreenQuad(EffectDeferred);

            //We want to reuse the diffuse target and its depth buffer, so copy the output data back to diffuse
            device.SetRenderTarget(diffuse);
            device.Clear(ClearOptions.Target, Color.Black, 0, 0);
            device.DepthStencilState = noDepthReadWriteDSS; //disable reading and writing the depth buffer.
            EffectCopy.Texture = preTransparencyOutput;
            DrawFullscreenQuad(EffectCopy);

            device.DepthStencilState = depthReadNoWriteDSS;
            device.BlendState = BlendState.AlphaBlend;

            //TODO sorting should be done in update, not draw
            DrawsTransparentPass = DrawsTransparentPass.OrderByDescending(x => x.SortValue).ToList();

            EffectTransparent.Parameters["ViewProjection"].SetValue(Main.camera.GetViewMatrix() * Main.camera.GetProjectionMatrix());

            //device.RasterizerState = Main.noCullRS;
            foreach (TransparentDraw draw in DrawsTransparentPass)
            {
                device.SetVertexBuffer(draw.VBO);
                device.Indices = draw.IBO;

                EffectTransparent.Parameters["Diffuse"].SetValue(draw.Diffuse);
                EffectTransparent.Parameters["Emissive"].SetValue(draw.Emissive);
                EffectTransparent.Parameters["World"].SetValue(draw.Transform);
                EffectTransparent.Parameters["TintColor"].SetValue(draw.TintColor);

                if (draw.UseSourceRect)
                {
                    EffectTransparent.Parameters["UseSourceRect"].SetValue(true);
                    EffectTransparent.Parameters["SourceRectPos"].SetValue(draw.SourceRectPos);
                    EffectTransparent.Parameters["SourceRectFarPos"].SetValue(draw.SourceRectFarPos);
                }
                else EffectTransparent.Parameters["UseSourceRect"].SetValue(false);

                EffectTransparent.Parameters["TextureSize"].SetValue(draw.TextureSize);

                foreach (var pass in EffectTransparent.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, draw.IBO.IndexCount / 3);

                    NumDrawCalls++;
                }
            }

            foreach (DEBUGDraw draw in DEBUGMarkersSphere)
            {
                EffectTransparent.Parameters["Diffuse"].SetValue(DrawHelper.WhitePixel);
                EffectTransparent.Parameters["Emissive"].SetValue(DrawHelper.WhitePixel);
                EffectTransparent.Parameters["World"].SetValue(Matrix.CreateScale(draw.Scale) * Matrix.CreateTranslation(draw.Position));
                EffectTransparent.Parameters["TintColor"].SetValue(draw.Color.ToVector4());

                EffectTransparent.Parameters["UseSourceRect"].SetValue(false);

                device.SetVertexBuffer(DEBUGSphereMesh.VBO);
                device.Indices = DEBUGSphereMesh.IBO;

                device.DepthStencilState = noDepthReadWriteDSS;
                device.BlendState = BlendState.Opaque;

                foreach (var pass in EffectTransparent.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, DEBUGSphereMesh.IBO.IndexCount / 3);
                }
            }

            foreach (DEBUGDraw draw in DEBUGMarkersRect)
            {
                EffectTransparent.Parameters["Diffuse"].SetValue(DrawHelper.WhitePixel);
                EffectTransparent.Parameters["Emissive"].SetValue(DrawHelper.WhitePixel);
                EffectTransparent.Parameters["World"].SetValue(Matrix.CreateScale(draw.Scale) * Matrix.CreateTranslation(draw.Position));
                EffectTransparent.Parameters["TintColor"].SetValue(draw.Color.ToVector4());

                EffectTransparent.Parameters["UseSourceRect"].SetValue(false);

                device.SetVertexBuffer(DEBUGCubeMesh.VBO);
                device.Indices = DEBUGCubeMesh.IBO;

                device.DepthStencilState = noDepthReadWriteDSS;
                device.BlendState = BlendState.Opaque;

                foreach (var pass in EffectTransparent.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, DEBUGCubeMesh.IBO.IndexCount / 3);
                }
            }

            /*foreach (PointLightVolumeDraw draw in DrawsShadowmappedPointLightVolumePass)
            {
                DEBUGEffectVisualizeCubemap.Parameters["Position"].SetValue(position);
                DEBUGEffectVisualizeCubemap.Parameters["Cubemap"].SetValue(draw.Cubemap);
                DEBUGEffectVisualizeCubemap.Parameters["CubemapCenter"].SetValue(draw.LightPosition);
                DEBUGEffectVisualizeCubemap.Parameters["ProjectionFar"].SetValue(draw.LightScale);

                DEBUGEffectVisualizeCubemap.Parameters["World"].SetValue(Matrix.CreateTranslation(draw.LightPosition));
                DEBUGEffectVisualizeCubemap.Parameters["ViewProjection"].SetValue(Main.camera.GetViewMatrix() * Main.camera.GetProjectionMatrix());

                device.SetVertexBuffer(cubemapMesh.VBO);
                device.Indices = cubemapMesh.IBO;

                //device.SetRenderTarget(diffuse);
                device.DepthStencilState = noDepthReadWriteDSS;
                device.BlendState = BlendState.Opaque;

                foreach (var pass in DEBUGEffectVisualizeCubemap.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, cubemapMesh.IBO.IndexCount / 3);
                }
            }*/

            if (EffectEmptyEnabled)
            {
                EffectEmpty.Parameters["ViewProjection"].SetValue(Main.camera.GetViewMatrix() * Main.camera.GetProjectionMatrix());

                device.DepthStencilState = DepthStencilState.None;
                //device.RasterizerState = cullCWRS;
                foreach (TransparentDraw draw in DrawsEmptyPass)
                {
                    device.SetVertexBuffer(draw.VBO);
                    device.Indices = draw.IBO;

                    EffectEmpty.Parameters["Diffuse"].SetValue(DrawHelper.WhitePixel);
                    EffectEmpty.Parameters["World"].SetValue(draw.Transform);
                    
                    foreach (var pass in EffectEmpty.CurrentTechnique.Passes)
                    {
                        pass.Apply();
                        device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, draw.IBO.IndexCount / 3);

                        NumDrawCalls++;
                    }
                }
            }

            if (Options.BloomEnabled)
                bloom.Draw(diffuse);

            device.BlendState = noAlphaBlendBS;

            //convert HDR to LDR for rendering to screen.
            device.SetRenderTarget(ldrOutputPing);
            device.Clear(Color.Black);
            EffectHDR.CurrentTechnique = Options.CurrentHDRType == Options.HDRType.HDR_EXP ? EffectHDR.Techniques["TechHDRExp"] : EffectHDR.Techniques["TechHDRAces"];
            EffectHDR.Parameters["Texture"].SetValue(diffuse);
            //EffectHDR.Parameters["Exposure"].SetValue(...);
            device.SetVertexBuffer(vboQuad);
            device.Indices = iboQuad;
            DrawFullscreenQuad(EffectHDR);

            if (Options.CurrentAntiAliasing == Options.AntiAliasing.FXAA)
            {
                device.SamplerStates[0] = SamplerState.LinearClamp;

                fxaa.Render(ldrOutputPing, ldrOutputPong);
                outputRT = ldrOutputPong;

                device.SamplerStates[0] = SamplerState.PointWrap;
                /*{
                    device.SamplerStates[0] = SamplerState.LinearClamp;

                    *//*EffectFXAA.CurrentTechnique = EffectFXAA.Techniques["ppfxaa_Console"];
                    EffectFXAA.Parameters["ConsoleOpt1"].SetValue(new Vector4(-2.0f / ldrOutputPong.Width, -2.0f / ldrOutputPong.Height, 2.0f / ldrOutputPong.Width, 2.0f / ldrOutputPong.Height));
                    EffectFXAA.Parameters["ConsoleOpt2"].SetValue(new Vector4(8.0f / ldrOutputPong.Width, 8.0f / ldrOutputPong.Height, -4.0f / ldrOutputPong.Width, -4.0f / ldrOutputPong.Height));
                    EffectFXAA.Parameters["ConsoleEdgeSharpness"].SetValue(8.0f);
                    EffectFXAA.Parameters["ConsoleEdgeThreshold"].SetValue(0.125f);
                    EffectFXAA.Parameters["ConsoleEdgeThresholdMin"].SetValue(0.05f);*//*

                    EffectFXAA.CurrentTechnique = EffectFXAA.Techniques["ppfxaa_PC"];
                    EffectFXAA.Parameters["fxaaQualitySubpix"].SetValue(0.75f);
                    EffectFXAA.Parameters["fxaaQualityEdgeThreshold"].SetValue(0.166f); //
                    EffectFXAA.Parameters["fxaaQualityEdgeThresholdMin"].SetValue(0.0625f); //0.0833f

                    EffectFXAA.Parameters["invViewportWidth"].SetValue(1f / ldrOutputPong.Width);
                    EffectFXAA.Parameters["invViewportHeight"].SetValue(1f / ldrOutputPong.Height);

                    device.SetRenderTarget(ldrOutputPong);
                    EffectFXAA.Parameters["Texture"].SetValue(ldrOutputPing);

                    DrawFullscreenQuad(EffectFXAA);

                    outputRT = ldrOutputPong;

                    device.SamplerStates[0] = SamplerState.PointWrap;
                }*/
            }
            else if (Options.CurrentAntiAliasing == Options.AntiAliasing.SMAA)
            {
                if (Options.SMAAThresholdChanged)
                    smaa.Threshold = Options.SMAAThreshold;

                //depth for depth,
                //otherwise ldrOutputPing for lumi/color?
                smaa.Go(depth, ldrOutputPing, ldrOutputPong, SMAA.Input.DEPTH);

                outputRT = ldrOutputPong;
            }
            else
            {
                outputRT = ldrOutputPing;
            }

            if (true)
            {
                device.BlendState = BlendState.Additive;

                device.SetRenderTarget(outputRT);

                EffectRadialFog.Parameters["Position"].SetValue(position);
                EffectRadialFog.Parameters["CameraPosition"].SetValue(Main.camera.Position);
                EffectRadialFog.Parameters["FogExtents"].SetValue(new Vector2(Cubes.Cube.CUBE_SCALE * Chunk.CHUNK_SIZE * (Options.RenderDistance - 2), Cubes.Cube.CUBE_SCALE * Chunk.CHUNK_SIZE * Options.RenderDistance));

                EffectRadialFog.Parameters["FogColor"].SetValue(Color.White.ToVector4());

                DrawFullscreenQuad(EffectRadialFog);
            }

            EffectCopy.View = Matrix.Identity;
            EffectCopy.Projection = Matrix.Identity;
            EffectCopy.World = Matrix.Identity;
        }

        private void DrawFullscreenQuad(Effect effect)
        {
            foreach (var pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, iboQuad.IndexCount / 3);

                NumDrawCalls++;
            }
        }

        public string GetOutputString()
        {
            if (currentOutput == -1)
                return "Composite";
            else return targets[currentOutput].RenderTarget.Name;
        }

        public RenderTargetBinding GetOutput()
        {
            if (currentOutput == -1)
                return outputRT;
            else return targets[currentOutput];
        }
    }
}
