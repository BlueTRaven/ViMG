using BepuUtilities.Memory;
using DemoRenderer;
using DemoRenderer.ShapeDrawing;
using Engine.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG;

namespace Engine.Rendering.BepuDebugRendering
{
    public class Renderer : IDisposable
    {
        private Point currResolution;
        public RenderTarget2D Surface { get; private set; }
        //public ShaderCache ShaderCache { get; private set; }
        //public BackgroundRenderer Background { get; private set; }
        //TODO: Down the road, the sphere renderer will be joined by a bunch of other types. 
        //They'll likely be stored in an array indexed by a shape type rather than just being a swarm of properties.
        public RayTracedRenderer<SphereInstance> SphereRenderer { get; private set; }
        public RayTracedRenderer<CapsuleInstance> CapsuleRenderer { get; private set; }
        public RayTracedRenderer<CylinderInstance> CylinderRenderer { get; private set; }
        //public BoxRenderer BoxRenderer { get; private set; }
        //public TriangleRenderer TriangleRenderer { get; private set; }
        //public MeshRenderer MeshRenderer { get; private set; }
        public ShapesExtractor Shapes { get; private set; }
        //public LineRenderer LineRenderer { get; private set; }
        //public LineExtractor Lines { get; private set; }
        //public ImageRenderer ImageRenderer { get; private set; }
        //public GlyphRenderer GlyphRenderer { get; private set; }
        //public UILineRenderer UILineRenderer { get; private set; }
        //public CompressToSwap CompressToSwap { get; private set; }

        //public ImageBatcher ImageBatcher { get; private set; }
        //public TextBatcher TextBatcher { get; private set; }
        //public UILineBatcher UILineBatcher { get; private set; }


        ParallelLooper looper;
        BufferPool pool;

        Texture2D depthBuffer;
        DepthStencilState depthStencilState;
        //DepthStencilView dsv;
        //Technically we could get away with rendering directly to the backbuffer, but a dedicated color buffer simplifies some things- 
        //you aren't bound by the requirements of the swapchain's buffer during rendering, and post processing is nicer.
        //Not entirely necessary for the demos, but hey, you could add tonemapping if you wanted?
        Texture2D colorBuffer;
        Texture2D resolvedColorBuffer;

        RasterizerState rasterizerState;
        DepthStencilState opaqueDepthState;
        BlendState opaqueBlendState;
        DepthStencilState uiDepthState;
        BlendState uiBlendState;


        public Renderer(GraphicsDevice device, RenderTarget2D surface)
        {
            looper = new ParallelLooper();
            Surface = surface;

            pool = new BufferPool();
            Shapes = new ShapesExtractor(device, looper, pool);
            SphereRenderer = new RayTracedRenderer<SphereInstance>(device, "render_spheres");
            CapsuleRenderer = new RayTracedRenderer<CapsuleInstance>(device, "render_capsules");
            CylinderRenderer = new RayTracedRenderer<CylinderInstance>(device, "render_cylinders");
            //BoxRenderer = new BoxRenderer(surface.Device, ShaderCache);
            //TriangleRenderer = new TriangleRenderer(surface.Device, ShaderCache);
            //MeshRenderer = new MeshRenderer(surface.Device, Shapes.MeshCache, ShaderCache);
            //Lines = new LineExtractor(pool, looper);
            //LineRenderer = new LineRenderer(surface.Device, ShaderCache);
            //Background = new BackgroundRenderer(surface.Device, ShaderCache);
            //CompressToSwap = new CompressToSwap(surface.Device, ShaderCache);

            //ImageRenderer = new ImageRenderer(surface.Device, ShaderCache);
            //ImageBatcher = new ImageBatcher(pool);
            //GlyphRenderer = new GlyphRenderer(surface.Device, ShaderCache);
            //TextBatcher = new TextBatcher();
            //UILineRenderer = new UILineRenderer(surface.Device, ShaderCache);
            //UILineBatcher = new UILineBatcher();

            OnResize();
            rasterizerState = RasterizerState.CullNone;

            opaqueDepthState = new DepthStencilState()
            {
                DepthBufferEnable = true,
                DepthBufferFunction = CompareFunction.Greater,
                StencilEnable = false
            };
            opaqueDepthState.Name = "Opaque Depth State";

            opaqueBlendState = BlendState.AlphaBlend;

            uiDepthState = DepthStencilState.None;

            //The UI will use premultiplied alpha.
            uiBlendState = BlendState.AlphaBlend;
        }

        void OnResize()
        {
            Helpers.Dispose(ref depthBuffer);
            //Helpers.Dispose(ref dsv);
            Helpers.Dispose(ref colorBuffer);
            //Helpers.Dispose(ref rtv);

            var resolution = Options.CurrentInternalResolution;
            currResolution = resolution;

            //TextBatcher.Resolution = resolution;
            //ImageBatcher.Resolution = resolution;
            //UILineBatcher.Resolution = resolution;

            //var sampleDescription = new SampleDescription(4, 0);
            //depthBuffer = new Texture2D(Surface.Device, new Texture2DDescription
            //{
            //    Format = Format.R32_Typeless,
            //    ArraySize = 1,
            //    MipLevels = 1,
            //    Width = resolution.X,
            //    Height = resolution.Y,
            //    SampleDescription = sampleDescription,
            //    Usage = ResourceUsage.Default,
            //    BindFlags = BindFlags.DepthStencil,
            //    CpuAccessFlags = CpuAccessFlags.None,
            //    OptionFlags = ResourceOptionFlags.None
            //});
            //depthBuffer.DebugName = "Depth Buffer";

            //var depthStencilViewDescription = new DepthStencilViewDescription
            //{
            //    Flags = DepthStencilViewFlags.None,
            //    Dimension = DepthStencilViewDimension.Texture2DMultisampled,
            //    Format = Format.D32_Float,
            //    Texture2D = { MipSlice = 0 }
            //};
            //dsv = new DepthStencilView(Surface.Device, depthBuffer, depthStencilViewDescription);
            //dsv.DebugName = "Depth DSV";

            ////Using a 64 bit texture in the demos for lighting is pretty silly. But we gon do it.
            //var description = new Texture2DDescription
            //{
            //    Format = Format.R16G16B16A16_Float,
            //    ArraySize = 1,
            //    MipLevels = 1,
            //    Width = resolution.X,
            //    Height = resolution.Y,
            //    SampleDescription = sampleDescription,
            //    Usage = ResourceUsage.Default,
            //    BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
            //    CpuAccessFlags = CpuAccessFlags.None,
            //    OptionFlags = ResourceOptionFlags.None
            //};
            //colorBuffer = new Texture2D(Surface.Device, description);
            //colorBuffer.DebugName = "Color Buffer";

            //rtv = new RenderTargetView(Surface.Device, colorBuffer);
            //rtv.DebugName = "Color RTV";

            //description.SampleDescription = new SampleDescription(1, 0);
            //resolvedColorBuffer = new Texture2D(Surface.Device, description);
            //resolvedColorBuffer.DebugName = "Resolved Color Buffer";

            //resolvedSRV = new ShaderResourceView(Surface.Device, resolvedColorBuffer);
            //resolvedSRV.DebugName = "Resolved Color SRV";

            //resolvedRTV = new RenderTargetView(Surface.Device, resolvedColorBuffer);
            //resolvedRTV.DebugName = "Resolved Color RTV";

        }

        public void Render(GraphicsDevice device, Camera camera)
        {
            if (Options.CurrentInternalResolution != currResolution)
            {
                OnResize();
            }
            Shapes.MeshCache.FlushPendingUploads(device);

            device.Clear(ClearOptions.DepthBuffer | ClearOptions.Stencil | ClearOptions.Target, Color.Black, 0, 0);
            device.SetRenderTarget(Surface);
            //Note reversed depth.

            device.BlendState = opaqueBlendState;
            //All ray traced shapes use analytic coverage writes to get antialiasing.
            SphereRenderer.Render(device, camera, currResolution, Shapes.ShapeCache.Spheres.Span, 0, Shapes.ShapeCache.Spheres.Count);
            CapsuleRenderer.Render(device, camera, currResolution, Shapes.ShapeCache.Capsules.Span, 0, Shapes.ShapeCache.Capsules.Count);
            CylinderRenderer.Render(device, camera, currResolution, Shapes.ShapeCache.Cylinders.Span, 0, Shapes.ShapeCache.Cylinders.Count);

            device.BlendState = opaqueBlendState;
            //Non-raytraced shapes just use regular opaque rendering.
            //context.OutputMerger.SetBlendState(opaqueBlendState);
            //BoxRenderer.Render(context, camera, Surface.Resolution, Shapes.ShapeCache.Boxes.Span, 0, Shapes.ShapeCache.Boxes.Count);
            //TriangleRenderer.Render(context, camera, Surface.Resolution, Shapes.ShapeCache.Triangles.Span, 0, Shapes.ShapeCache.Triangles.Count);
            //MeshRenderer.Render(context, camera, Surface.Resolution, Shapes.ShapeCache.Meshes.Span, 0, Shapes.ShapeCache.Meshes.Count);
            //LineRenderer.Render(context, camera, Surface.Resolution, Lines.lines.Span, 0, Lines.lines.Count);

            //Background.Render(context, camera);

            //Glyph and screenspace line drawing rely on the same premultiplied alpha blending transparency. We'll handle their state out here.
            //context.OutputMerger.SetBlendState(uiBlendState);
            //context.OutputMerger.SetDepthStencilState(uiDepthState);
            //ImageRenderer.PreparePipeline(context);
            //ImageBatcher.Flush(context, Surface.Resolution, ImageRenderer);
            //UILineBatcher.Flush(context, Surface.Resolution, UILineRenderer);
            //GlyphRenderer.PreparePipeline(context);
            //TextBatcher.Flush(context, Surface.Resolution, GlyphRenderer);

            //Note that, for now, the compress to swap handles its own depth state since it's the only post processing stage.
        }

        bool disposed;
        public void Dispose()
        {
            if (!disposed)
            {
                disposed = true;
                //Background.Dispose();
                //CompressToSwap.Dispose();

                //Lines.Dispose();

                SphereRenderer.Dispose();
                CapsuleRenderer.Dispose();
                CylinderRenderer.Dispose();
                //BoxRenderer.Dispose();
                //TriangleRenderer.Dispose();
                //MeshRenderer.Dispose();

                //UILineRenderer.Dispose();
                //GlyphRenderer.Dispose();

                //dsv.Dispose();
                depthBuffer?.Dispose();
                //rtv.Dispose();
                colorBuffer?.Dispose();
                //resolvedSRV.Dispose();
                //resolvedRTV.Dispose();
                resolvedColorBuffer?.Dispose();

                rasterizerState.Dispose();
                opaqueDepthState.Dispose();
                opaqueBlendState.Dispose();
                uiDepthState.Dispose();
                uiBlendState.Dispose();

                Shapes.Dispose();

                pool.Clear();
            }
        }

#if DEBUG
        ~Renderer()
        {
            Helpers.CheckForUndisposed(disposed, this);
        }
#endif
    }
}
