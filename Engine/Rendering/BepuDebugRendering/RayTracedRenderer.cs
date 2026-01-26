using BepuPhysics;
using BepuUtilities;
using Engine.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using ViMG;
using ViMG.VertexDeclarations;

namespace Engine.Rendering.BepuDebugRendering
{
    struct DummyVertex : IVertexType
    {
        public static readonly VertexDeclaration VertexDeclaration;

        public float dummy;

        VertexDeclaration IVertexType.VertexDeclaration
        {
            get
            {
                return VertexDeclaration;
            }
        }

        public static VertexDeclaration NewVertexDeclaration(int offset)
        {
            var elements = new VertexElement[] {
                            new VertexElement(Marshal.OffsetOf<DummyVertex>("dummy").ToInt32(), VertexElementFormat.Single, VertexElementUsage.TextureCoordinate, offset + 0),
};

            return new VertexDeclaration(elements);
        }

        static DummyVertex() 
        {
            VertexDeclaration = NewVertexDeclaration(0);
        }
    };

    [StructLayout(LayoutKind.Explicit)]
    struct RayTracedVertexConstants
    {
        [FieldOffset(0)]
        public Microsoft.Xna.Framework.Matrix Projection;
        [FieldOffset(64)]
        public Vector3 CameraPosition;
        [FieldOffset(76)]
        public float NearClip;
        [FieldOffset(80)]
        public Vector3 CameraRight;
        [FieldOffset(96)]
        public Vector3 CameraUp;
        [FieldOffset(112)]
        public Vector3 CameraBackward;
    }
    [StructLayout(LayoutKind.Explicit, Size = 64)]
    struct RayTracedPixelConstants
    {
        [FieldOffset(0)]
        public Vector3 CameraRight;
        [FieldOffset(12)]
        public float NearClip;
        [FieldOffset(16)]
        public Vector3 CameraUp;
        [FieldOffset(28)]
        public float FarClip;
        [FieldOffset(32)]
        public Vector3 CameraBackward;
        [FieldOffset(48)]
        public Vector2 PixelSizeAtUnitPlane;

    }
    public class RayTracedRenderer<TInstance> : IDisposable where TInstance : struct
    {
        //While multiple ray traced renderers will end up with redundant constants and some other details, it hardly matters. Doing it this way is super simple and low effort.
        StructuredBuffer instances;
        IndexBuffer indices;
        VertexBuffer verticesDummy;

        Effect effect;
        public RayTracedRenderer(GraphicsDevice device, string shaderPath, int maximumInstancesPerDraw = 2048)
        {
            var instanceTypeName = typeof(TInstance).Name;
            instances = new StructuredBuffer(device, typeof(TInstance), maximumInstancesPerDraw, BufferUsage.WriteOnly, ShaderAccess.Read);

            indices = new IndexBuffer(device, IndexElementSize.ThirtyTwoBits, DemoRenderer.Helpers.GetBoxIndices(maximumInstancesPerDraw).Length, BufferUsage.WriteOnly, ShaderAccess.Read);
            indices.SetData(DemoRenderer.Helpers.GetBoxIndices(maximumInstancesPerDraw));
            verticesDummy = new VertexBuffer(device, typeof(DummyVertex), maximumInstancesPerDraw * 8, BufferUsage.WriteOnly);

            effect = GlobalState.assetsManager.GetAsset<Effect>(shaderPath);
        }

        public void Render(GraphicsDevice device, Camera camera, Point screenResolution, Span<TInstance> instances, int start, int count)
        {
            var vertexConstantsData = new RayTracedVertexConstants
            {
                Projection = (camera.GetProjectionMatrix()), //compensate for the shader packing.
                CameraPosition = -camera.Position,
                CameraRight = camera.Right,
                NearClip = camera.Near,
                CameraUp = camera.Up,
                CameraBackward = camera.Forward,
            };
            effect.Parameters["Projection"].SetValue(vertexConstantsData.Projection);
            effect.Parameters["CameraPosition"].SetValue(vertexConstantsData.CameraPosition);
            effect.Parameters["CameraRight"].SetValue(vertexConstantsData.CameraRight);
            effect.Parameters["NearClip"].SetValue(vertexConstantsData.NearClip);
            effect.Parameters["CameraUp"].SetValue(vertexConstantsData.CameraUp);
            effect.Parameters["CameraBackward"].SetValue(vertexConstantsData.CameraBackward);

            float viewportHeight = Options.CurrentWindowResolution.Y;
            float viewportWidth = Options.CurrentWindowResolution.X;
            if (camera is CameraPerspective pers)
            {
                viewportHeight = 2 * (float)Math.Tan(Microsoft.Xna.Framework.MathHelper.ToRadians(pers.FOVDegrees) / 2);
                viewportWidth = viewportHeight * ((float)Options.CurrentWindowResolution.X / (float)Options.CurrentWindowResolution.Y);
            }
            var pixelConstantsData = new RayTracedPixelConstants
            {
                CameraRight = camera.Right,
                NearClip = camera.Near,
                CameraUp = camera.Up,
                FarClip = camera.Far,
                CameraBackward = camera.Forward,
                PixelSizeAtUnitPlane = new Vector2(viewportWidth / (float)screenResolution.X, viewportHeight / (float)screenResolution.Y)
            };
            effect.Parameters["CameraRightPS"].SetValue(pixelConstantsData.CameraRight);
            effect.Parameters["Near"].SetValue(pixelConstantsData.NearClip);
            effect.Parameters["CameraUpPS"].SetValue(pixelConstantsData.CameraUp);
            effect.Parameters["Far"].SetValue(pixelConstantsData.FarClip);
            effect.Parameters["CameraBackwardPS"].SetValue(pixelConstantsData.CameraBackward);
            effect.Parameters["PixelSizeAtUnitPlane"].SetValue(pixelConstantsData.PixelSizeAtUnitPlane);

            effect.Parameters["Instances"].SetValue(this.instances);
            device.RasterizerState = RasterizerState.CullNone;
            device.Indices = this.indices;
            device.SetVertexBuffer(verticesDummy);

            while (count > 0) 
            {
                var batchCount = Math.Min(this.instances.ElementCount, count);
                this.instances.SetData(instances.ToArray(), start, batchCount);
                foreach (var pass in effect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    device.DrawInstancedPrimitives(PrimitiveType.TriangleList, 0, 0, ((batchCount * 36) / 3), 1);
                }
                count -= batchCount;
                start += batchCount;
            }
            
        }

        bool disposed;
        public void Dispose()
        {
            if (!disposed)
            {
                disposed = true;
                instances.Dispose();
                indices.Dispose();
            }
        }
    }
}
