using BrUtility;
using Engine;
using Engine.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SMAADemo;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using ViMG.IMGUIImpl;
using ViMG.VertexDeclarations;

namespace ViMG.Rendering
{
    public class RendererDeferred
    {
        public struct DrawSourceRectParameters
        {
            public bool UseSourceRect;
            public Vector2 SourceRectPos;
            public Vector2 SourceRectFarPos;

            public DrawSourceRectParameters(RectangleF sourceRect)
            {
                RectangleF rect = sourceRect;

                UseSourceRect = true;
                SourceRectPos = rect.Position;
                SourceRectFarPos = rect.FarPosition;
            }
        }

        public record struct DrawMaterial
        {
            public Texture2D Diffuse;
            public Texture2D Normal = DrawHelper.NormalPixel;
            public Texture2D Specular = DrawHelper.BlackPixel;
            public Texture2D Emissive = DrawHelper.BlackPixel;

            public DrawMaterial(Texture2D diffuseOnly)
            {
                Diffuse = diffuseOnly;
                Normal = DrawHelper.NormalPixel;
                Specular = DrawHelper.BlackPixel;
                Emissive = DrawHelper.BlackPixel;

                if (Diffuse == null)
                    throw new Exception("AAAAA");
            }

            public DrawMaterial(Texture2D diffuse, Texture2D normal = null, Texture2D specular = null, Texture2D emissive = null)
            {
                Diffuse = diffuse;
                Normal = normal ?? DrawHelper.NormalPixel;
                Specular = specular ?? DrawHelper.BlackPixel;
                Emissive = emissive ?? DrawHelper.BlackPixel;
            }

            public DrawMaterial(string name)
            {
                Diffuse = GlobalState.AssetsManager.GetAsset<Texture2D>(name);
                Normal = GlobalState.AssetsManager.GetAsset<Texture2D>(name + "_normal") ?? DrawHelper.NormalPixel;
                Specular = GlobalState.AssetsManager.GetAsset<Texture2D>(name + "_specular") ?? DrawHelper.BlackPixel;
                Emissive = GlobalState.AssetsManager.GetAsset<Texture2D>(name + "_emissive") ?? DrawHelper.BlackPixel;

                if (Diffuse == null)
                    throw new Exception("AAAAA");
            }

            public DrawMaterial(string diffuseName, string? normalName, string? specularName, string? emissiveName)
            {
                Diffuse = GlobalState.AssetsManager.GetAsset<Texture2D>(diffuseName);
                Normal = normalName != null ? GlobalState.AssetsManager.GetAsset<Texture2D>(normalName) : DrawHelper.NormalPixel;
                Specular = specularName != null ? GlobalState.AssetsManager.GetAsset<Texture2D>(specularName) : DrawHelper.BlackPixel;
                Emissive = emissiveName != null ? GlobalState.AssetsManager.GetAsset<Texture2D>(emissiveName) : DrawHelper.BlackPixel;
            }
        }

        public record struct InstancedDraw
        {
            public Matrix World;
            public Matrix WorldNormal;

            public DrawSourceRectParameters SourceRect;

            public Vector3 TintColor = Vector3.One;

            public InstancedDraw()
            {
                TintColor = Vector3.One;
            }
        }

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
            public DrawMaterial Material;

            public VerySimpleMesh Mesh;

            public Matrix World;
            public Matrix WorldNormal;

            public DrawSourceRectParameters SourceRect;

            public Vector3 TintColor;

            public GBufferDraw(DrawMaterial material, VerySimpleMesh mesh, Matrix world, RectangleF? sourceRect = null, Vector3? tintColor = null)
            {
                this.Material = material;
                //this.VBO = VBO;
                //this.IBO = IBO;
                this.Mesh = mesh;
                this.World = world;
                this.WorldNormal = Matrix.Transpose(Matrix.Invert(world));
                this.TintColor = tintColor.GetValueOrDefault(Color.White.ToVector3());

                if (sourceRect.HasValue)
                    SourceRect = new DrawSourceRectParameters(sourceRect.Value);
            }
        }

        //Instanced GBuffer draws share:
        //Textures
        //Meshes
        //And may have per-instance:
        //World matrices
        //Tint colors
        //Source rectangles
        //The SBO must contain an array of InstancedDraws.
        public struct InstancedGBufferDraw
        {
            public DrawMaterial Material;

            public VerySimpleMesh Mesh;

            public StructuredBuffer SBO;

            public int SBOStart;
            public int SBOLen;

            public InstancedGBufferDraw(DrawMaterial material, VerySimpleMesh mesh, StructuredBuffer SBO, int SBOStart = 0, int SBOLen = -1)
            {
                if (SBOLen == -1)
                    SBOLen = SBO.ElementCount;

                this.Material = material;
                this.Mesh = mesh;

                this.SBO = SBO;
                this.SBOStart = SBOStart;
                this.SBOLen = SBOLen;
            }
        }

        public struct TransparentDraw
        {
            public DrawMaterial Material;

            public float SortValue;
            public Matrix Transform;

            public VerySimpleMesh Mesh;

            public DrawSourceRectParameters SourceRect;

            public Vector4 TintColor;

            public TransparentDraw(float sortValue, DrawMaterial material, VerySimpleMesh mesh, Matrix transform, RectangleF? sourceRect = null, Color? tintColor = null)
            {
                this.Material = material;
                this.SortValue = sortValue;
                this.Transform = transform;
                this.Mesh = mesh;

                if (sourceRect.HasValue)
                    SourceRect = new DrawSourceRectParameters(sourceRect.Value);

                if (tintColor == null)
                    TintColor = Color.White.ToVector4();
                else TintColor = tintColor.Value.ToVector4();
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

        private RenderTarget2D skybox;

        private RenderTarget2D preTransparencyOutput;
        private RenderTarget2D ldrOutputPing;
        private RenderTarget2D ldrOutputPong;

        private RenderTarget2D outputRT;

        //SetRenderTargets uses params, which constructs an implicit array every time it's called,
        //which is an allocation every frame. Don't do that. Just allocate one to start with...
        private RenderTargetBinding[] gbufferTargets;
        private RenderTargetBinding[] transparentTargets;

        private RendererBloom bloom;
        private SMAA smaa;
        private RendererFXAA fxaa;
        
        private int currentOutput = -1;

        public Effect EffectGBuffer;
        public Effect EffectLightAccumCSM;
        public Effect EffectLightAccumPointLight;
        public Effect EffectDeferred;
        public Effect EffectTransparent;
        public Effect EffectSkybox;
        public bool EffectEmptyEnabled;
        public Effect EffectEmpty;
        public Effect EffectHDR;
        public Effect EffectRadialFog;
        public Effect EffectAurora;
        public Effect EffectStars;
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

        private uint[] lightVolumeIndices = new uint[LightManager.LightsMax];
        private StructuredBuffer bufferLightVolumeIndices;

        public List<GBufferDraw> DrawsPassGBuffer = new List<GBufferDraw>();
        public List<InstancedGBufferDraw> DrawsPassGBufferInstanced = new List<InstancedGBufferDraw>();
        public List<PointLightVolumeDraw> DrawsPointLightVolumePass = new List<PointLightVolumeDraw>();
        public List<PointLightVolumeDraw> DrawsShadowmappedPointLightVolumePass = new List<PointLightVolumeDraw>();
        private List<TransparentDraw> DrawsTransparentPass = new List<TransparentDraw>();
        public List<TransparentDraw> DrawsEmptyPass = new List<TransparentDraw>();
        public List<TransparentDraw> DrawsSkyboxPass = new List<TransparentDraw>();
        
        // Transient debug markers that can draw simple primitives. These do NOT use the depth buffer! They will be drawn over everything else!
        public List<DEBUGDraw> DEBUGMarkersSphere = new List<DEBUGDraw>();
        public List<DEBUGDraw> DEBUGMarkersRect = new List<DEBUGDraw>();

        public static int NumPointLightsRendered;
        public static int NumDrawCalls;

        public Vector2 FogExtents;  //X: near, Y: far

        public bool DoCSMLight = true;
        private float alive;

        private Options.SMAAQuality previousSMAAOption;
        private Options.FXAAQuality previousFXAAOption;

        public (VertexBuffer VBO, IndexBuffer IBO) DEBUGCubemapMesh;
        public VerySimpleMesh DEBUGSphereMesh;
        public (VertexBuffer VBO, IndexBuffer IBO) DEBUGCubeMesh;

        public RendererDeferred(GraphicsDevice device)
        {
            DEBUGCubemapMesh = MeshHelper.MakeDebugCubemap(device, -Vector3.One, Vector3.One);
            DEBUGSphereMesh = MeshHelper.MakeUVSphere(device, 1);

            FastList<VertexCube> cubeVertices = new FastList<VertexCube>();
            List<int> cubeIndices = new List<int>();
            MeshHelper.MakeCubeVertsVertexPositionColorTextureNormal(-Vector3.One / 2f, Vector3.One / 2f, MeshHelper.CubeFace.ALL, Color.White, cubeVertices, cubeIndices);
            DEBUGCubeMesh = MeshHelper.MakeSimplerMesh(device, cubeVertices.ToVertexTransparentPass(), cubeIndices);

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

            EffectGBuffer = GlobalState.AssetsManager.GetAsset<Effect>("deferred_gbuffer");
            EffectDeferred = GlobalState.AssetsManager.GetAsset<Effect>("deferred");
            EffectLightAccumCSM = GlobalState.AssetsManager.GetAsset<Effect>("deferred_lightaccum_csmlight");
            EffectLightAccumPointLight = GlobalState.AssetsManager.GetAsset<Effect>("deferred_lightaccum_pointlight");
            EffectTransparent = GlobalState.AssetsManager.GetAsset<Effect>("transparent");
            EffectSkybox = GlobalState.AssetsManager.GetAsset<Effect>("skybox");
            EffectEmpty = GlobalState.AssetsManager.GetAsset<Effect>("air");
            EffectHDR = GlobalState.AssetsManager.GetAsset<Effect>("hdr");
            EffectFXAA = GlobalState.AssetsManager.GetAsset<Effect>("fxaa");
            EffectRadialFog = GlobalState.AssetsManager.GetAsset<Effect>("radial_fog");
            EffectAurora = GlobalState.AssetsManager.GetAsset<Effect>("aurora");
            EffectStars = GlobalState.AssetsManager.GetAsset<Effect>("stars");

            DEBUGEffectVisualizeCubemap = GlobalState.AssetsManager.GetAsset<Effect>("visualize_cubemap");

            //EffectGBuffer.Parameters["AmbientStrength"].SetValue(0.1f);
            EffectGBuffer.Parameters["SpecularPower"].SetValue(4);

            //EffectRadialFog.Parameters["Color"].SetValue(GlobalState.assetsManager.GetAsset<Texture2D>("fog_colormap"));

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

            VerySimpleMesh uvSphere = MeshHelper.MakeUVSphere(device, 1);
            vboUVSphere = uvSphere.VBOPosition;
            iboUVSphere = uvSphere.IBO;

            bufferLightVolumeIndices = new StructuredBuffer(device, typeof(uint), lightVolumeIndices.Length, BufferUsage.WriteOnly, ShaderAccess.Read);
            EffectLightAccumPointLight.Parameters["LightInstanceIndices"].SetValue(bufferLightVolumeIndices);
        }

        public void FrameStart()
        {
            DrawsPassGBuffer.Clear();
            DrawsPassGBufferInstanced.Clear();
            DrawsPointLightVolumePass.Clear();
            DrawsShadowmappedPointLightVolumePass.Clear();
            DrawsTransparentPass.Clear();
            DrawsSkyboxPass.Clear();
            DrawsEmptyPass.Clear();

            DEBUGMarkersSphere.Clear();
            DEBUGMarkersRect.Clear();

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
            gbufferTargets = new RenderTargetBinding[]
            {
                diffuse,
                lightAccum,
                depth,
                position,
                normal,
                ao,
            };

            transparentTargets = new RenderTargetBinding[]
            {
                diffuse,
                position,
            };

            preTransparencyOutput?.Dispose();
            skybox?.Dispose();
            ldrOutputPing?.Dispose();
            ldrOutputPong?.Dispose();

            preTransparencyOutput = new RenderTarget2D(device, rez.X, rez.Y, false, SurfaceFormat.HalfVector4, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            skybox = new RenderTarget2D(device, rez.X, rez.Y, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
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
                    currentOutput = gbufferTargets.Length - 1;
            }

            if (Main.inputManager.JustPressed(Microsoft.Xna.Framework.Input.Keys.OemCloseBrackets))
            {
                currentOutput++;
                if (currentOutput > gbufferTargets.Length - 1)
                    currentOutput = -1;
            }

            switch (IMGUISettings.GBufferOverrideDraw)
            {
                case IMGUISettings.RendererGBufferOverrideDraw.All:
                    currentOutput = -2;
                    break;
                case IMGUISettings.RendererGBufferOverrideDraw.Composited:
                    currentOutput = -1;
                    break;
                case IMGUISettings.RendererGBufferOverrideDraw.Diffuse:
                    currentOutput = 0;
                    break;
                case IMGUISettings.RendererGBufferOverrideDraw.LightAccum:
                    currentOutput = 1;
                    break;
                case IMGUISettings.RendererGBufferOverrideDraw.Depth:
                    currentOutput = 2;
                    break;
                case IMGUISettings.RendererGBufferOverrideDraw.Position:
                    currentOutput = 3;
                    break;
                case IMGUISettings.RendererGBufferOverrideDraw.Normal:
                    currentOutput = 4;
                    break;
                case IMGUISettings.RendererGBufferOverrideDraw.Ao:
                    currentOutput = 5;
                    break;
            }
        }

        public void Draw(SpriteBatch batch, Engine.Common.Camera camera)
        {
            SetPipelineState();

            if (Options.CurrentSMAAQuality != previousSMAAOption)
                ConstructSMAA(Options.CurrentWindowResolution);

            if (Options.CurrentFXAAQuality != previousFXAAOption)
                ConstructFXAA(Options.CurrentWindowResolution);

            device.SetRenderTargets(gbufferTargets);
            device.Clear(ClearOptions.DepthBuffer | ClearOptions.Target, Color.Transparent, device.Viewport.MaxDepth, 0);

            device.SetRenderTarget(ao);
            device.Clear(Color.White);

            device.SetRenderTargets(gbufferTargets);

            if (DrawsPassGBuffer.Count > 0)
            {
                Matrix viewProjection = camera.GetViewMatrix() * camera.GetProjectionMatrix();
                EffectGBuffer.Parameters["View"].SetValue(camera.GetViewMatrix());
                EffectGBuffer.Parameters["ViewProjection"].SetValue(viewProjection);
                EffectGBuffer.Parameters["UseInstancing"].SetValue(false);

                device.SamplerStates[1] = bilinearClampSS;

                foreach (GBufferDraw draw in DrawsPassGBuffer)
                {
                    if (draw.Mesh.IBO != null)
                    {
                        device.SetVertexBuffers(draw.Mesh.Bindings);
                        //device.SetVertexBuffer(draw.VBO);
                        device.Indices = draw.Mesh.IBO;

                        EffectGBuffer.Parameters["World"].SetValue(draw.World);
                        EffectGBuffer.Parameters["WorldNormal"].SetValue(Matrix.Transpose(Matrix.Invert(draw.World)));

                        EffectGBuffer.Parameters["Diffuse"].SetValue(draw.Material.Diffuse);
                        EffectGBuffer.Parameters["Normal"].SetValue(draw.Material.Normal);
                        EffectGBuffer.Parameters["Specular"].SetValue(draw.Material.Specular);
                        EffectGBuffer.Parameters["Emissive"].SetValue(draw.Material.Emissive);

                        EffectGBuffer.Parameters["TextureSize"].SetValue(draw.Material.Diffuse.Bounds.Size.ToVector2());

                        EffectGBuffer.Parameters["TintColor"].SetValue(draw.TintColor);

                        if (draw.SourceRect.UseSourceRect)
                        {
                            EffectGBuffer.Parameters["UseSourceRect"].SetValue(true);
                            EffectGBuffer.Parameters["SourceRectPos"].SetValue(draw.SourceRect.SourceRectPos);
                            EffectGBuffer.Parameters["SourceRectFarPos"].SetValue(draw.SourceRect.SourceRectFarPos);
                        }
                        else EffectGBuffer.Parameters["UseSourceRect"].SetValue(false);
                        
                        foreach (var pass in EffectGBuffer.CurrentTechnique.Passes)
                        {
                            pass.Apply();
                            device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, draw.Mesh.IBO.IndexCount / 3);

                            NumDrawCalls++;
                        }
                    }
                }

                if (DrawsPassGBufferInstanced.Count > 0)
                {
                    EffectGBuffer.Parameters["UseInstancing"].SetValue(true);

                    foreach (InstancedGBufferDraw draw in DrawsPassGBufferInstanced)
                    {
                        device.SetVertexBuffers(draw.Mesh.Bindings);
                        device.Indices = draw.Mesh.IBO;
                        //device.SetVertexBuffer(draw.VBO);
                        //device.Indices = draw.IBO;

                        EffectGBuffer.Parameters["InstancedDraws"].SetValue(draw.SBO);

                        EffectGBuffer.Parameters["Diffuse"].SetValue(draw.Material.Diffuse);
                        EffectGBuffer.Parameters["Normal"].SetValue(draw.Material.Normal);
                        EffectGBuffer.Parameters["Specular"].SetValue(draw.Material.Specular);
                        EffectGBuffer.Parameters["Emissive"].SetValue(draw.Material.Emissive);

                        EffectGBuffer.Parameters["TextureSize"].SetValue(draw.Material.Diffuse.Bounds.Size.ToVector2());

                        foreach (var pass in EffectGBuffer.CurrentTechnique.Passes)
                        {
                            pass.Apply();
                            device.DrawInstancedPrimitives(PrimitiveType.TriangleList, 0, draw.SBOStart * draw.Mesh.IBO.IndexCount, draw.Mesh.IBO.IndexCount / 3, draw.SBOLen);

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
                EffectLightAccumCSM.Parameters["CameraPosition"].SetValue(camera.Position);
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
                EffectLightAccumPointLight.Parameters["CameraPosition"].SetValue(-camera.Position);

                Matrix viewProj = camera.GetViewMatrix() * camera.GetProjectionMatrix();

                EffectLightAccumPointLight.Parameters["ViewProjection"].SetValue(viewProj);
                //EffectLightAccumPointLight.Parameters["InvViewProjection"].SetValue(Matrix.Invert(Main.camera.GetViewMatrix() * Main.camera.GetProjectionMatrix()));
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
                EffectLightAccumPointLight.Parameters["CameraPosition"].SetValue(camera.Position);

                Matrix viewProj = camera.GetViewMatrix() * camera.GetProjectionMatrix();

                EffectLightAccumPointLight.Parameters["ViewProjection"].SetValue(viewProj);
                //EffectLightAccumPointLight.Parameters["InvViewProjection"].SetValue(Matrix.Invert(Main.camera.GetViewMatrix() * Main.camera.GetProjectionMatrix()));
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

            //Skybox
            //===============================================================================================================================================
            device.SetRenderTarget(skybox);
            device.Clear(Color.Transparent);

            device.BlendState = BlendState.AlphaBlend;

            EffectSkybox.Parameters["ViewProjection"].SetValue(camera.GetViewMatrix() * camera.GetProjectionMatrix());
            const int SEA_FLOOR = 128; // TODO: put this somewhere
            EffectSkybox.Parameters["SeaLevel"].SetValue(SEA_FLOOR * Cubes.Cube.CUBE_SCALE);

            //TODO sorting should be done in update, not draw
            DrawsSkyboxPass = DrawsSkyboxPass.OrderByDescending(x => x.SortValue).ToList();

            foreach (TransparentDraw draw in DrawsSkyboxPass)
            {
                device.SetVertexBuffers(draw.Mesh.Bindings);
                device.Indices = draw.Mesh.IBO;
                //device.SetVertexBuffer(draw.VBO);
                //device.Indices = draw.IBO;

                EffectSkybox.Parameters["Diffuse"].SetValue(draw.Material.Diffuse);
                EffectSkybox.Parameters["World"].SetValue(draw.Transform);
                EffectSkybox.Parameters["TintColor"].SetValue(draw.TintColor);

                if (draw.SourceRect.UseSourceRect)
                {
                    EffectSkybox.Parameters["UseSourceRect"].SetValue(true);
                    EffectSkybox.Parameters["SourceRectPos"].SetValue(draw.SourceRect.SourceRectPos);
                    EffectSkybox.Parameters["SourceRectFarPos"].SetValue(draw.SourceRect.SourceRectFarPos);
                }
                else EffectSkybox.Parameters["UseSourceRect"].SetValue(false);

                EffectSkybox.Parameters["TextureSize"].SetValue(draw.Material.Diffuse.Bounds.Size.ToVector2());

                foreach (var pass in EffectSkybox.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, draw.Mesh.IBO.IndexCount / 3);

                    NumDrawCalls++;
                }
            }

            /*EffectStars.Parameters["Time"].SetValue(alive);
            EffectStars.Parameters["WorldMatrix"].SetValue(Main.camera.GetViewMatrix());
            
            device.SetVertexBuffer(vboQuad);
            device.Indices = iboQuad;
            DrawFullscreenQuad(EffectStars);*/

            /*EffectAurora.Parameters["Time"].SetValue(alive);
            EffectAurora.Parameters["Resolution"].SetValue(Options.CurrentWindowResolution.ToVector2());
            EffectAurora.Parameters["CameraPosition"].SetValue(new Vector3(-Main.camera.Rotation.Y, Main.camera.Rotation.X, 0));
            EffectAurora.Parameters["Mat"].SetValue(
                    Matrix.CreateRotationX(Main.camera.Rotation.X) *
                    Matrix.CreateRotationY(-Main.camera.Rotation.Y)
                    );

            device.SetVertexBuffer(vboQuad);
            device.Indices = iboQuad;
            DrawFullscreenQuad(EffectAurora);*/
            //===============================================================================================================================================

            //Diffuse Composite
            //Composits all of the gbuffer back into one buffer.
            //===============================================================================================================================================
            device.SetRenderTarget(preTransparencyOutput);
            device.Clear(ClearOptions.Target, Color.Transparent, 0, 0);
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
            //===============================================================================================================================================

            //Radial Fog
            //===============================================================================================================================================
            if (true)
            {
                device.BlendState = BlendState.AlphaBlend;

                device.SetRenderTarget(preTransparencyOutput);

                EffectRadialFog.CurrentTechnique = EffectRadialFog.Techniques["T2"];
                EffectRadialFog.Parameters["Position"].SetValue(position);
                EffectRadialFog.Parameters["Color"].SetValue(skybox);
                EffectRadialFog.Parameters["FogExtents"].SetValue(FogExtents);
                EffectRadialFog.Parameters["CameraPosition"].SetValue(camera.Position);

                device.SetVertexBuffer(vboQuad);
                device.Indices = iboQuad;
                DrawFullscreenQuad(EffectRadialFog);
            }
            //===============================================================================================================================================

            //Transparent pass
            //===============================================================================================================================================
            //We want to reuse the diffuse target and its depth buffer, so copy the output data back to diffuse
            device.SetRenderTarget(diffuse);
            device.Clear(ClearOptions.Target, Color.Transparent, 0, 0);
            device.DepthStencilState = noDepthReadWriteDSS; //disable reading and writing the depth buffer.
            EffectCopy.Texture = preTransparencyOutput;
            DrawFullscreenQuad(EffectCopy);

            device.DepthStencilState = depthReadNoWriteDSS;
            device.BlendState = BlendState.AlphaBlend;

            //TODO sorting should be done in update, not draw
            DrawsTransparentPass = DrawsTransparentPass.OrderByDescending(x => x.SortValue).ToList();

            EffectTransparent.Parameters["ViewProjection"].SetValue(camera.GetViewMatrix() * camera.GetProjectionMatrix());
            EffectTransparent.Parameters["CameraPosition"].SetValue(camera.Position);
            EffectTransparent.Parameters["FogExtents"].SetValue(FogExtents);

            EffectTransparent.CurrentTechnique = EffectTransparent.Techniques["T1"];
            //device.RasterizerState = Main.noCullRS;
            foreach (TransparentDraw draw in DrawsTransparentPass)
            {
                device.SetVertexBuffers(draw.Mesh.Bindings);
                device.Indices = draw.Mesh.IBO;
                //device.SetVertexBuffer(draw.VBO);
                //device.Indices = draw.IBO;

                EffectTransparent.Parameters["Diffuse"].SetValue(draw.Material.Diffuse);
                EffectTransparent.Parameters["Emissive"].SetValue(draw.Material.Emissive);
                EffectTransparent.Parameters["World"].SetValue(draw.Transform);
                EffectTransparent.Parameters["TintColor"].SetValue(draw.TintColor);

                if (draw.SourceRect.UseSourceRect)
                {
                    EffectTransparent.Parameters["UseSourceRect"].SetValue(true);
                    EffectTransparent.Parameters["SourceRectPos"].SetValue(draw.SourceRect.SourceRectPos);
                    EffectTransparent.Parameters["SourceRectFarPos"].SetValue(draw.SourceRect.SourceRectFarPos);
                }
                else EffectTransparent.Parameters["UseSourceRect"].SetValue(false);

                EffectTransparent.Parameters["TextureSize"].SetValue(draw.Material.Diffuse.Bounds.Size.ToVector2());

                foreach (var pass in EffectTransparent.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, draw.Mesh.IBO.IndexCount / 3);

                    NumDrawCalls++;
                }
            }
            //===============================================================================================================================================

            //DEBUG
            //===============================================================================================================================================
            device.RasterizerState = RasterizerState.CullNone;

            device.SetVertexBuffers(DEBUGSphereMesh.Bindings);
            device.Indices = DEBUGSphereMesh.IBO;

            device.DepthStencilState = noDepthReadWriteDSS;
            device.BlendState = BlendState.AlphaBlend;

            foreach (DEBUGDraw draw in DEBUGMarkersSphere)
            {
                EffectTransparent.Parameters["Diffuse"].SetValue(DrawHelper.WhitePixel);
                EffectTransparent.Parameters["Emissive"].SetValue(DrawHelper.WhitePixel);
                EffectTransparent.Parameters["World"].SetValue(Matrix.CreateScale(draw.Scale) * Matrix.CreateTranslation(draw.Position));
                EffectTransparent.Parameters["TintColor"].SetValue(draw.Color.ToVector4());

                EffectTransparent.Parameters["UseSourceRect"].SetValue(false);

                foreach (var pass in EffectTransparent.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, DEBUGSphereMesh.IBO.IndexCount / 3);
                }
            }

            device.SetVertexBuffer(DEBUGCubeMesh.VBO);
            device.Indices = DEBUGCubeMesh.IBO;

            device.DepthStencilState = noDepthReadWriteDSS;
            device.BlendState = BlendState.AlphaBlend;

            foreach (DEBUGDraw draw in DEBUGMarkersRect)
            {
                EffectTransparent.Parameters["Diffuse"].SetValue(DrawHelper.WhitePixel);
                EffectTransparent.Parameters["Emissive"].SetValue(DrawHelper.WhitePixel);
                EffectTransparent.Parameters["World"].SetValue(Matrix.CreateScale(draw.Scale) * Matrix.CreateTranslation(draw.Position));
                EffectTransparent.Parameters["TintColor"].SetValue(draw.Color.ToVector4());

                EffectTransparent.Parameters["UseSourceRect"].SetValue(false);

                foreach (var pass in EffectTransparent.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, DEBUGCubeMesh.IBO.IndexCount / 3);
                }
            }
            //===============================================================================================================================================

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
                EffectEmpty.Parameters["ViewProjection"].SetValue(camera.GetViewMatrix() * camera.GetProjectionMatrix());

                device.DepthStencilState = DepthStencilState.None;
                //device.RasterizerState = cullCWRS;
                foreach (TransparentDraw draw in DrawsEmptyPass)
                {
                    device.SetVertexBuffers(draw.Mesh.Bindings);
                    device.Indices = draw.Mesh.IBO;
                    //device.SetVertexBuffer(draw.VBO);
                    //device.Indices = draw.IBO;

                    EffectEmpty.Parameters["Diffuse"].SetValue(DrawHelper.WhitePixel);
                    EffectEmpty.Parameters["World"].SetValue(draw.Transform);
                    
                    foreach (var pass in EffectEmpty.CurrentTechnique.Passes)
                    {
                        pass.Apply();
                        device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, draw.Mesh.IBO.IndexCount / 3);

                        NumDrawCalls++;
                    }
                }
            }

            if (Options.BloomEnabled)
                bloom.Draw(diffuse);

            device.BlendState = noAlphaBlendBS;

            //Post-processing
            //===============================================================================================================================================
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
            }
            else if (Options.CurrentAntiAliasing == Options.AntiAliasing.SMAA)
            {
                if (Options.SMAAThresholdChanged)
                    smaa.Threshold = Options.SMAAThreshold;

                //depth for depth,
                //otherwise ldrOutputPing for lumi/color?
                smaa.Go(camera, depth, ldrOutputPing, ldrOutputPong, SMAA.Input.DEPTH);

                outputRT = ldrOutputPong;
            }
            else
            {
                outputRT = ldrOutputPing;
            }
            //===============================================================================================================================================

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
            else if (currentOutput == -2) 
                return "All";
            else return gbufferTargets[currentOutput].RenderTarget.Name;
        }

        public RenderTargetBinding? GetOutput()
        {

            if (currentOutput == -1 || currentOutput == -2)
            {
                if (outputRT == null) return null;
                else return outputRT;
            }
            else return gbufferTargets[currentOutput];
        }

        public void AddOpaqueDraw(GBufferDraw draw)
        {
            IMGUIConsole.Assert(draw.Mesh.VBOPosition != null, "OpaqueDraw requires a position VBO");
            IMGUIConsole.Assert(draw.Mesh.VBOColor != null, "OpaqueDraw requires a color VBO");
            IMGUIConsole.Assert(draw.Mesh.VBOTexCoord != null, "OpaqueDraw requires a texcoord VBO");
            IMGUIConsole.Assert(draw.Mesh.VBONormal != null, "OpaqueDraw requires a normal VBO");
            IMGUIConsole.Assert(draw.Mesh.VBOAO != null, "OpaqueDraw requires a AO VBO");
            IMGUIConsole.Assert(draw.Mesh.VBOAnim != null, "OpaqueDraw requires a Animation VBO");

            DrawsPassGBuffer.Add(draw);
        }

        public void AddTransparentDraw(TransparentDraw draw)
        {
            if (draw.Material.Diffuse == null)
            {
                Console.WriteLine("Cannot add draw without diffuse material.");
                return;
            }

            DrawsTransparentPass.Add(draw);
        }
    }
}
