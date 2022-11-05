using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViMG.Rendering
{
    public class RendererFXAA : IDisposable
    {
        /// <summary>        
        /// Choose the amount of sub-pixel aliasing removal.
        /// This can effect sharpness.
        ///   1.00 - upper limit (softer)
        ///   0.75 - default amount of filtering
        ///   0.50 - lower limit (sharper, less sub-pixel aliasing removal)
        ///   0.25 - almost off
        ///   0.00 - completely off
        /// </summary>
        public float SubPixelAliasingRemoval
        {
            get { return subPixelAliasingRemovalParam.GetValueSingle(); }
            set { subPixelAliasingRemovalParam.SetValue(value); }
        }

        /// <summary>
        /// The minimum amount of local contrast required to apply algorithm.
        ///   0.333 - too little (faster)
        ///   0.250 - low quality
        ///   0.166 - default
        ///   0.125 - high quality 
        ///   0.063 - overkill (slower)
        /// </summary>
        public float EdgeThreshold
        {
            get { return edgeThresholdParam.GetValueSingle(); }
            set { edgeThresholdParam.SetValue(value); }
        }

        /// <summary>
        /// Trims the algorithm from processing darks.
        ///   0.0833 - upper limit (default, the start of visible unfiltered edges)
        ///   0.0625 - high quality (faster)
        ///   0.0312 - visible limit (slower)
        /// Special notes when using FXAA_GREEN_AS_LUMA,
        ///   Likely want to set this to zero.
        ///   As colors that are mostly not-green
        ///   will appear very dark in the green channel!
        ///   Tune by looking at mostly non-green content,
        ///   then start at zero and increase until aliasing is a problem.
        /// </summary>
        public float EdgeThresholdMin
        {
            get { return edgeThresholdMinParam.GetValueSingle(); }
            set { edgeThresholdMinParam.SetValue(value); }
        }

        public float ConsoleEdgeSharpness
        {
            get { return consoleEdgeSharpnessParam.GetValueSingle(); }
            set { consoleEdgeSharpnessParam.SetValue(value); }
        }

        public float ConsoleEdgeThreshold
        {
            get { return consoleEdgeThresholdParam.GetValueSingle(); }
            set { consoleEdgeThresholdParam.SetValue(value); }
        }

        public float ConsoleEdgeThresholdMin
        {
            get { return consoleEdgeThresholdMinParam.GetValueSingle(); }
            set { consoleEdgeThresholdMinParam.SetValue(value); }
        }

        public Vector2 InverseViewportSize
        {
            get { return inverseViewportSizeParam.GetValueVector2(); }
            set { inverseViewportSizeParam.SetValue(value); }
        }

        public Vector4 ConsoleSharpness
        {
            get { return consoleSharpnessParam.GetValueVector4(); }
            set { consoleSharpnessParam.SetValue(value); }
        }

        public Vector4 ConsoleOpt1
        {
            get { return consoleOpt1Param.GetValueVector4(); }
            set { consoleOpt1Param.SetValue(value); }
        }

        public Vector4 ConsoleOpt2
        {
            get { return consoleOpt2Param.GetValueVector4(); }
            set { consoleOpt2Param.SetValue(value); }
        }

        public Matrix Projection
        {
            get { return projectionParam.GetValueMatrix(); }
            set { projectionParam.SetValue(value); }
        }

        public Matrix View
        {
            get { return viewParam.GetValueMatrix(); }
            set { viewParam.SetValue(value); }
        }

        public Matrix World
        {
            get { return worldParam.GetValueMatrix(); }
            set { worldParam.SetValue(value); }
        }

        private readonly EffectParameter subPixelAliasingRemovalParam;
        private readonly EffectParameter edgeThresholdParam;
        private readonly EffectParameter edgeThresholdMinParam;
        private readonly EffectParameter consoleEdgeSharpnessParam;
        private readonly EffectParameter consoleEdgeThresholdParam;
        private readonly EffectParameter consoleEdgeThresholdMinParam;
        private readonly EffectParameter inverseViewportSizeParam;
        private readonly EffectParameter consoleSharpnessParam;
        private readonly EffectParameter consoleOpt1Param;
        private readonly EffectParameter consoleOpt2Param;
        private readonly EffectParameter projectionParam;
        private readonly EffectParameter viewParam;
        private readonly EffectParameter worldParam;

        private readonly Effect effect;

        private readonly GraphicsDevice device;
        private readonly Point rez;
        private readonly float n;

        ///   Where N ranges between,
        ///     N = 0.50 (default)
        ///     N = 0.33 (sharper)
        public RendererFXAA(GraphicsDevice device, Point rez, float N = 0.5f)
        {
            switch (Options.CurrentFXAAQuality)
            {
                case Options.FXAAQuality.FXAA_LOW:
                    effect = Main.assetsManager.GetAsset<Effect>("FXAAGreenLumaLow");
                    break;
                case Options.FXAAQuality.FXAA_MEDIUM:
                    effect = Main.assetsManager.GetAsset<Effect>("FXAAGreenLumaMedium");
                    break;
                case Options.FXAAQuality.FXAA_HIGH:
                    effect = Main.assetsManager.GetAsset<Effect>("FXAAGreenLumaHigh");
                    break;
            }

            subPixelAliasingRemovalParam = effect.Parameters["SubPixelAliasingRemoval"];
            edgeThresholdParam = effect.Parameters["EdgeThreshold"];
            edgeThresholdMinParam = effect.Parameters["EdgeThresholdMin"];
            consoleEdgeSharpnessParam = effect.Parameters["ConsoleEdgeSharpness"];
            consoleEdgeThresholdParam = effect.Parameters["ConsoleEdgeThreshold"];
            consoleEdgeThresholdMinParam = effect.Parameters["ConsoleEdgeThresholdMin"];
            inverseViewportSizeParam = effect.Parameters["InverseViewportSize"];
            consoleSharpnessParam = effect.Parameters["ConsoleSharpness"];
            consoleOpt1Param = effect.Parameters["ConsoleOpt1"];
            consoleOpt2Param = effect.Parameters["ConsoleOpt2"];
            projectionParam = effect.Parameters["Projection"];
            viewParam = effect.Parameters["View"];
            worldParam = effect.Parameters["World"];

            SubPixelAliasingRemoval = 0.75f;
            EdgeThreshold = 0.166f;
            EdgeThresholdMin = 0.0833f;
            ConsoleEdgeSharpness = 8.0f;
            ConsoleEdgeThreshold = 0.125f;
            ConsoleEdgeThresholdMin = 0f;

            InverseViewportSize = new Vector2(1f / rez.X, 1f / rez.Y);
            ConsoleSharpness = new Vector4(
                -N / rez.X, -N / rez.Y,
                N / rez.X, N / rez.Y);
            ConsoleOpt1 = new Vector4(
                -2.0f / rez.X, -2.0f / rez.Y,
                2.0f / rez.X, 2.0f / rez.Y);
            ConsoleOpt2 = new Vector4(
                8.0f / rez.X, 8.0f / rez.Y,
                -4.0f / rez.X, -4.0f / rez.Y);

            World = Matrix.Identity;
            View = Matrix.Identity;
            Projection = Matrix.Identity;

            this.device = device;
            this.rez = rez;
            n = N;
        }

        public void Render(RenderTarget2D src, RenderTarget2D dst) 
        {
            device.SetRenderTarget(dst);
            effect.Parameters["Texture"].SetValue(src);

            SubPixelAliasingRemoval = 0.75f;
            EdgeThreshold = 0.166f;
            EdgeThresholdMin = 0.0833f;
            ConsoleEdgeSharpness = 8.0f;
            ConsoleEdgeThreshold = 0.125f;
            ConsoleEdgeThresholdMin = 0f;

            InverseViewportSize = new Vector2(1f / rez.X, 1f / rez.Y);
            ConsoleSharpness = new Vector4(
                -n / rez.X, -n / rez.Y,
                n / rez.X, n / rez.Y);
            ConsoleOpt1 = new Vector4(
                -2.0f / rez.X, -2.0f / rez.Y,
                2.0f / rez.X, 2.0f / rez.Y);
            ConsoleOpt2 = new Vector4(
                8.0f / rez.X, 8.0f / rez.Y,
                -4.0f / rez.X, -4.0f / rez.Y);

            World = Matrix.Identity;
            View = Matrix.Identity;
            Projection = Matrix.Identity;

            DrawHelper3D.DrawFullscreenQuad(device, effect);
        }

        public void Dispose()
        {
            //TODO implement if necessary
        }
    }
}
