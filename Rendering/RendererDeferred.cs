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
        public struct DeferredDraw
        {
            public Texture2D Diffuse;
            public Texture2D Specular;
            public Texture2D Emissive;

            public VertexBuffer VBO;
            public IndexBuffer IBO;

            public Matrix World;
            public Matrix View;
            public Matrix WorldNormal;
            public Matrix ViewProjection;

            public bool UseSourceRect;
            public Vector2 SourceRectPos;
            public Vector2 SourceRectFarPos;
            public Vector2 TextureSize;

            public DeferredDraw(Texture2D diffuse, Texture2D specular, Texture2D emissive, VertexBuffer VBO, IndexBuffer IBO, Matrix world, Matrix view, Matrix projection, RectangleF? sourceRect)
            {
                this.Diffuse = diffuse;
                this.Specular = specular;
                this.Emissive = emissive;
                this.VBO = VBO;
                this.IBO = IBO;
                this.World = world;
                this.WorldNormal = Matrix.Transpose(Matrix.Invert(world));
                this.View = view;
                this.ViewProjection = view * projection;

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

        private BasicEffect EffectCopy;
        private SamplerState shadowBorderClampSS;
        private BlendState noAlphaBlendBS;
        private BlendState normalBS;
        private BlendState additiveBS;

        private VertexBuffer VBO;
        private IndexBuffer IBO;

        public List<DeferredDraw> DrawsPassGBuffer = new List<DeferredDraw>();

        public RendererDeferred(GraphicsDevice device)
        {
            EffectCopy = new BasicEffect(device);
            EffectCopy.TextureEnabled = true;
            EffectCopy.VertexColorEnabled = false;
            EffectCopy.FogEnabled = false;
            EffectCopy.LightingEnabled = false;

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

            this.device = device;

            ConstructRTs(Options.CurrentWindowResolution);

            EffectGBuffer = Main.assetsManager.GetAsset<Effect>("deferred_gbuffer");
            EffectDeferred = Main.assetsManager.GetAsset<Effect>("deferred");
            EffectLightAccumCSM = Main.assetsManager.GetAsset<Effect>("deferred_lightaccum_csmlight");
            EffectLightAccumPointLight = Main.assetsManager.GetAsset<Effect>("deferred_lightaccum_pointlight");

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

            VBO = new VertexBuffer(device, typeof(VertexPositionTexture), vpt.Length, BufferUsage.WriteOnly);
            VBO.SetData(vpt);

            IBO = new IndexBuffer(device, typeof(uint), 6, BufferUsage.WriteOnly);
            IBO.SetData(indices);
        }

        public void FrameStart()
        {
            DrawsPassGBuffer.Clear();
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
            work = new RenderTarget2D(device, rez.X, rez.Y, false, SurfaceFormat.HalfVector4, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            work.Name = "Work";
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

        public void Update()
        {
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

                foreach (DeferredDraw draw in DrawsPassGBuffer)
                {
                    if (draw.VBO != null && draw.IBO != null)
                    {
                        device.SetVertexBuffer(draw.VBO);
                        device.Indices = draw.IBO;

                        EffectGBuffer.Parameters["World"].SetValue(draw.World);
                        EffectGBuffer.Parameters["WorldNormal"].SetValue(Matrix.Transpose(Matrix.Invert(draw.World)));
                        EffectGBuffer.Parameters["ViewProjection"].SetValue(draw.ViewProjection);

                        EffectGBuffer.Parameters["Diffuse"].SetValue(draw.Diffuse);
                        EffectGBuffer.Parameters["Specular"].SetValue(draw.Specular);
                        EffectGBuffer.Parameters["Emissive"].SetValue(draw.Emissive);

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
            }

            device.SetVertexBuffer(VBO);
            device.Indices = IBO;

            device.SetRenderTarget(lightAccum);

            EffectLightAccumCSM.Parameters["Position"].SetValue(position);
            EffectLightAccumCSM.Parameters["Depth"].SetValue(depth);
            EffectLightAccumCSM.Parameters["Normal"].SetValue(normal);
            EffectLightAccumCSM.Parameters["CameraPosition"].SetValue(Main.camera.Position);
            device.SamplerStates[1] = shadowBorderClampSS;
            device.BlendState = additiveBS;

            DrawFullscreenQuad(EffectLightAccumCSM);

            //Now copy work back to lightAccum
            device.SetRenderTarget(lightAccum);

            EffectLightAccumPointLight.Parameters["Position"].SetValue(position);
            //EffectLightAccumPointLight.Parameters["Depth"].SetValue(depth);
            EffectLightAccumPointLight.Parameters["Normal"].SetValue(normal);
            //EffectLightAccumPointLight.Parameters["Diffuse"].SetValue(diffuse);
            EffectLightAccumPointLight.Parameters["CameraPosition"].SetValue(Main.camera.Position);

            DrawFullscreenQuad(EffectLightAccumPointLight);

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
        }

        private void DrawFullscreenQuad(Effect effect)
        {
            foreach (var pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, IBO.IndexCount / 3);
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
                return output;
            else return targets[currentOutput];
        }
    }
}
