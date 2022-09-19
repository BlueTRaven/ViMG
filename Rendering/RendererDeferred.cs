using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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
            
            public PointLightVolumeDraw(int index, Vector3 position, float scale)
            {
                this.LightIndex = index;
                this.LightPosition = position;
                this.LightScale = scale;
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
            public int SortValue;
            public Matrix Transform;
            public Texture2D Texture;
            public VertexBuffer VBO;
            public IndexBuffer IBO;

            public bool UseSourceRect;
            public Vector2 SourceRectPos;
            public Vector2 SourceRectFarPos;
            public Vector2 TextureSize;

            public Vector4 TintColor;

            public TransparentDraw(int sortValue, Matrix transform, Texture2D texture, VertexBuffer vbo, IndexBuffer ibo, RectangleF? sourceRect, Color? tintColor = null)
            {
                this.SortValue = sortValue;
                this.Transform = transform;
                this.Texture = texture;
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

                TextureSize = new Vector2(texture.Width, texture.Height);
            }
        }

        private readonly GraphicsDevice device;

        private RenderTarget2D diffuse;       //RGB albedo data; A specular data
        private RenderTarget2D lightAccum;  //RGB ambient + emissive to begin with. Light is accumulated after gbuffer pass.
        private RenderTarget2D depth;       //R depth data
        private RenderTarget2D position;    //RGB position data; A unused
        private RenderTarget2D normal;      //RGB normal data; A unused
        private RenderTarget2D ao;          //R AO data

        private RenderTarget2D work;

        private RenderTarget2D output;

        //SetRenderTargets uses params, which constructs an implicit array every time it's called,
        //which is an allocation every frame. Don't do that. Just allocate one to start with...
        private RenderTargetBinding[] targets;
        private int currentOutput = -1;

        public Effect EffectGBuffer;
        public Effect EffectLightAccumCSM;
        public Effect EffectLightAccumPointLight;
        public Effect EffectDeferred;
        public Effect EffectTransparent;

        private BasicEffect EffectCopy;
        private SamplerState shadowBorderClampSS;
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

        public List<GBufferDraw> DrawsPassGBuffer = new List<GBufferDraw>();
        public List<PointLightVolumeDraw> DrawsPointLightVolumePass = new List<PointLightVolumeDraw>();
        public List<TransparentDraw> DrawsTransparentPass = new List<TransparentDraw>();

        public static int NumPointLightsRendered;

        private float alive;

        public RendererDeferred(GraphicsDevice device)
        {
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
        }

        public void FrameStart()
        {
            DrawsPassGBuffer.Clear();
            DrawsPointLightVolumePass.Clear();
            DrawsTransparentPass.Clear();

            NumPointLightsRendered = 0;
        }

        private void ConstructRTs(Point rez)
        {
            diffuse?.Dispose();
            ao?.Dispose();
            position?.Dispose();
            normal?.Dispose();

            diffuse = new RenderTarget2D(device, rez.X, rez.Y, false, SurfaceFormat.Color, DepthFormat.Depth24Stencil8, 0, RenderTargetUsage.PreserveContents);
            diffuse.Name = "Diffuse";
            lightAccum = new RenderTarget2D(device, rez.X, rez.Y, false, SurfaceFormat.HalfVector4, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            lightAccum.Name = "Light Accumulation";
            depth = new RenderTarget2D(device, rez.X, rez.Y, false, SurfaceFormat.Single, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            depth.Name = "Depth";
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

            output = new RenderTarget2D(device, rez.X, rez.Y, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
        }

        public void SetPipelineState()
        {
            device.RasterizerState = Main.genericRS;
            device.BlendState = noAlphaBlendBS;
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

            if (DrawsPassGBuffer.Count > 0)
            {
                device.SetRenderTargets(targets);
                device.Clear(ClearOptions.DepthBuffer | ClearOptions.Target, Color.Black, device.Viewport.MaxDepth, 0);
                //device.RasterizerState = Main.noCullRS;

                Matrix viewProjection = Main.camera.GetViewMatrix() * Main.camera.GetProjectionMatrix();
                EffectGBuffer.Parameters["View"].SetValue(Main.camera.GetViewMatrix());
                EffectGBuffer.Parameters["ViewProjection"].SetValue(viewProjection);

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
                            EffectGBuffer.Parameters["TextureSize"].SetValue(draw.Diffuse.Bounds.Size.ToVector2());
                        }
                        else EffectGBuffer.Parameters["UseSourceRect"].SetValue(false);

                        foreach (var pass in EffectGBuffer.CurrentTechnique.Passes)
                        {
                            pass.Apply();
                            device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, draw.IBO.IndexCount / 3);
                        }
                    }
                }

                //device.RasterizerState = Main.genericRS;
            }

            device.SetVertexBuffer(vboQuad);
            device.Indices = iboQuad;

            device.SetRenderTarget(lightAccum);

            EffectLightAccumCSM.Parameters["Position"].SetValue(position);
            EffectLightAccumCSM.Parameters["Depth"].SetValue(depth);
            EffectLightAccumCSM.Parameters["Normal"].SetValue(normal);
            EffectLightAccumCSM.Parameters["CameraPosition"].SetValue(Main.camera.Position);
            device.SamplerStates[1] = shadowBorderClampSS;
            device.BlendState = additiveBS;

            DrawFullscreenQuad(EffectLightAccumCSM);

            if (DrawsPointLightVolumePass.Count > 0)
            {
                device.SetRenderTarget(lightAccum);
                device.RasterizerState = cullCWRS;
                device.SamplerStates[1] = shadowBorderClampSS;
                device.BlendState = additiveBS;

                EffectLightAccumPointLight.Parameters["Position"].SetValue(position);
                //EffectLightAccumPointLight.Parameters["Depth"].SetValue(depth);
                EffectLightAccumPointLight.Parameters["Normal"].SetValue(normal);
                //EffectLightAccumPointLight.Parameters["Diffuse"].SetValue(diffuse);
                EffectLightAccumPointLight.Parameters["CameraPosition"].SetValue(Main.camera.Position);

                Matrix viewProj = Main.camera.GetViewMatrix() * Main.camera.GetProjectionMatrix();

                foreach (PointLightVolumeDraw draw in DrawsPointLightVolumePass)
                {
                    EffectLightAccumPointLight.Parameters["WorldViewProjection"].SetValue(
                        Matrix.CreateScale(draw.LightScale) * 
                        Matrix.CreateTranslation(draw.LightPosition) *
                        viewProj);

                    EffectLightAccumPointLight.Parameters["LightIndex"].SetValue(draw.LightIndex);

                    device.SetVertexBuffer(vboUVSphere);
                    device.Indices = iboUVSphere;

                    foreach (var pass in EffectLightAccumPointLight.CurrentTechnique.Passes)
                    {
                        pass.Apply();
                        device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, iboUVSphere.IndexCount / 3);
                        //device.DrawInstancedPrimitives(PrimitiveType.TriangleList, 0, 0, iboUVSphere.IndexCount / 3, DrawsPointLightVolumePass.)
                    }

                    NumPointLightsRendered++;
                }

                device.RasterizerState = cullCCWRS;

                device.SetVertexBuffer(vboQuad);
                device.Indices = iboQuad;
                //DrawFullscreenQuad(EffectLightAccumPointLight);
            }

            //TODO transparent pass

            device.SetRenderTarget(output);
            device.Clear(ClearOptions.Target, Color.Black, 0, 0);
            device.BlendState = noAlphaBlendBS;

            EffectDeferred.Parameters["Diffuse"].SetValue(diffuse);
            //EffectDeferred.Parameters["Depth"].SetValue(depth);
            //EffectDeferred.Parameters["Position"].SetValue(position);
            EffectDeferred.Parameters["LightAccumulation"].SetValue(lightAccum);
            //EffectDeferred.Parameters["Normal"].SetValue(normal);
            EffectDeferred.Parameters["AO"].SetValue(ao);

            DrawFullscreenQuad(EffectDeferred);

            //We want to reuse the diffuse target and its depth buffer, so copy the output data back to diffuse
            device.SetRenderTarget(diffuse);
            device.Clear(ClearOptions.Target, Color.Black, 0, 0);
            device.DepthStencilState = noDepthReadWriteDSS; //disable reading and writing the depth buffer.
            EffectCopy.Texture = output;
            DrawFullscreenQuad(EffectCopy);

            device.DepthStencilState = depthReadNoWriteDSS;
            device.BlendState = BlendState.AlphaBlend;

            //TODO: use a custom shader for this?
            //TODO sorting should be done in update, not draw
            DrawsTransparentPass = DrawsTransparentPass.OrderByDescending(x => x.SortValue).ToList();

            EffectTransparent.Parameters["ViewProjection"].SetValue(Main.camera.GetViewMatrix() * Main.camera.GetProjectionMatrix());

            foreach (TransparentDraw draw in DrawsTransparentPass)
            {
                device.SetVertexBuffer(draw.VBO);
                device.Indices = draw.IBO;

                EffectTransparent.Parameters["Diffuse"].SetValue(draw.Texture);
                EffectTransparent.Parameters["World"].SetValue(draw.Transform);
                EffectTransparent.Parameters["TintColor"].SetValue(draw.TintColor);

                if (draw.UseSourceRect)
                {
                    EffectTransparent.Parameters["UseSourceRect"].SetValue(true);
                    EffectTransparent.Parameters["SourceRectPos"].SetValue(draw.SourceRectPos);
                    EffectTransparent.Parameters["SourceRectFarPos"].SetValue(draw.SourceRectFarPos);
                    EffectTransparent.Parameters["TextureSize"].SetValue(draw.TextureSize);
                }
                else EffectTransparent.Parameters["UseSourceRect"].SetValue(false);

                foreach (var pass in EffectTransparent.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, draw.IBO.IndexCount / 3);
                }
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
                return diffuse;
            else return targets[currentOutput];
        }
    }
}
