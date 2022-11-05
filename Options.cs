using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViMG
{
    public static class Options
    {
        public enum AntiAliasing
        {
            None,
            FXAA,
            SMAA,
        }

        public enum SMAAQuality
        {
            SMAA_LOW,
            SMAA_MEDIUM,
            SMAA_HIGH,
            SMAA_ULTRA,
        }

        public enum FXAAQuality
        {
            FXAA_LOW,
            FXAA_MEDIUM,
            FXAA_HIGH,
        }

        //some sentinel value. Just needs to not be any of the existing options. This is so we know for sure when we go from no SMAA to
        //any SMAA.
        public const SMAAQuality SMAA_INVALID = (SMAAQuality)200;
        public const FXAAQuality FXAA_INVALID = (FXAAQuality)200;

        public static Point[] Resolutions = new Point[]
        {
            new Point(320, 224),
            new Point(960, 540),
            new Point(640, 480),
            new Point(1280, 720),
            new Point(1920, 1080)
        };

        public static float WindowAspectRatio => (float)CurrentWindowResolution.X / (float)CurrentWindowResolution.Y;

        public static Point CurrentWindowResolution = Resolutions[3];
        public static Point CurrentInternalResolution = Resolutions[0];

        public static AntiAliasing CurrentAntiAliasing;
        public static SMAAQuality CurrentSMAAQuality = SMAA_INVALID;
        public static FXAAQuality CurrentFXAAQuality = FXAAQuality.FXAA_HIGH;//FXAA_INVALID;

        //TODO Remove
        public static float SMAAThreshold = 0.05f;
        public static bool SMAAThresholdChanged;
        public static bool UseInstancedLightVolumes;

        public static void CenterMouse()
        {
            Mouse.SetPosition(CurrentWindowResolution.X / 2, CurrentWindowResolution.Y / 2);
        }
    }
}
