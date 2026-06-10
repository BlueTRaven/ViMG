using Engine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViMG.Rendering
{
    public class RendererBloom : IDisposable
    {
        private const int NUM_MIPS = 4;
        private RenderTarget2D[] mips;

        private readonly GraphicsDevice device;

        private Effect downsampleEffect;
        private Effect upsampleEffect;

        private BlendState bs = new BlendState()
        {
            ColorBlendFunction = BlendFunction.Add,
            ColorSourceBlend = Blend.One,
            ColorDestinationBlend = Blend.One,
            AlphaBlendFunction = BlendFunction.Add,
            AlphaSourceBlend = Blend.One,
            AlphaDestinationBlend = Blend.One,
        };
        private bool disposedValue;

        public RendererBloom(GraphicsDevice device)
        {
            this.device = device;

            downsampleEffect = GlobalState.AssetsManager.GetAsset<Effect>("bloom_downsample");
            upsampleEffect = GlobalState.AssetsManager.GetAsset<Effect>("bloom_upsample");

            ConstructRTs(Options.CurrentWindowResolution);

            Main.WindowResizedEvent += ConstructRTs;
        }

        private void ConstructRTs(Point resolution)
        {
            if (mips != null)
                for (int i = 0; i < mips.Length; i++)
                    mips[i].Dispose();
            else mips = new RenderTarget2D[NUM_MIPS];

            Point mipResolution = resolution;

            for (int i = 0; i < NUM_MIPS; i++) 
            {
                mipResolution = new Point(mipResolution.X / 2, mipResolution.Y / 2);

                mips[i] = new RenderTarget2D(device, mipResolution.X, mipResolution.Y, false, SurfaceFormat.HalfVector4, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            }
        }

        public RenderTarget2D Draw(RenderTarget2D sourceTexture)
        {
            SamplerState oldSamplerState = device.SamplerStates[0];
            device.SamplerStates[0] = SamplerState.LinearClamp;

            BlendState oldBlendState = device.BlendState;
            device.BlendState = BlendState.AlphaBlend;

            downsampleEffect.Parameters["Texture"].SetValue(sourceTexture);
            downsampleEffect.Parameters["SrcResolution"].SetValue(new Vector2(sourceTexture.Width, sourceTexture.Height));

            for (int i = 0; i < NUM_MIPS; i++)
            {
                device.SetRenderTarget(mips[i]);

                DrawHelper3D.DrawFullscreenQuad(device, downsampleEffect);

                downsampleEffect.Parameters["Texture"].SetValue(mips[i]);
                downsampleEffect.Parameters["SrcResolution"].SetValue(new Vector2(mips[i].Width, mips[i].Height));
            }

            upsampleEffect.Parameters["FilterRadius"].SetValue(0.0001f);

            device.BlendState = bs;

            if (Main.inputManager.IsPressed(Microsoft.Xna.Framework.Input.Keys.O))
                device.BlendState = BlendState.Additive;

            for (int i = NUM_MIPS - 1; i >= 0; i--)
            {
                if (i > 0)
                {
                    RenderTarget2D mip = mips[i];
                    RenderTarget2D nextMip = mips[i - 1];

                    upsampleEffect.Parameters["Texture"].SetValue(mip);
                    upsampleEffect.Parameters["Strength"].SetValue(((float)i / (float)NUM_MIPS));

                    device.SetRenderTarget(nextMip);

                    DrawHelper3D.DrawFullscreenQuad(device, upsampleEffect);
                }
                else
                {
                    RenderTarget2D mip = mips[i];
                    RenderTarget2D nextMip = sourceTexture;

                    upsampleEffect.Parameters["Texture"].SetValue(mip);
                    upsampleEffect.Parameters["Strength"].SetValue(((float)i / (float)NUM_MIPS));

                    device.SetRenderTarget(nextMip);

                    DrawHelper3D.DrawFullscreenQuad(device, upsampleEffect);
                }
            }

            device.SamplerStates[0] = oldSamplerState;
            device.BlendState = oldBlendState;

            return sourceTexture;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                }

                foreach (var item in mips)
                {
                    item.Dispose();
                }
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~RendererBloom()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
