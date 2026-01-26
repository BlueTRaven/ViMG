using Engine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ViMG.Cubes;

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

        public enum HDRType
        {
            HDR_EXP,
            HDR_ACES
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

        public static Point CurrentWindowResolution = Resolutions[2];
        public static Point CurrentInternalResolution = Resolutions[0];

        public static AntiAliasing CurrentAntiAliasing;
        public static SMAAQuality CurrentSMAAQuality = SMAA_INVALID;
        public static FXAAQuality CurrentFXAAQuality = FXAAQuality.FXAA_HIGH;//FXAA_INVALID;
        public static HDRType CurrentHDRType = HDRType.HDR_EXP;

        public static bool UseInstancedLightVolumes = true;

        public static bool BloomEnabled;

        public const int RENDER_DISTANCE_MIN = 4;
        public const int RENDER_DISTANCE_MAX = 16;
        public static int RenderDistance = 6;

        public static float DEBUGTimescale = 1f;

        //TODO Remove
        public static float SMAAThreshold = 0.05f;
        public static bool SMAAThresholdChanged;

        public static Vector2 DefaultFogExtents => new Vector2(Cube.CUBE_SCALE * Chunk.CHUNK_SIZE * (RenderDistance - 3),
                Cube.CUBE_SCALE * Chunk.CHUNK_SIZE * (RenderDistance - 1));

        public static bool ShowConsole;

        public static void CenterMouse()
        {
            if (Thread.CurrentThread == GlobalState.MainThread)
                Mouse.SetPosition(CurrentWindowResolution.X / 2, CurrentWindowResolution.Y / 2);
        }

        public static void OnSave(List<byte> saveBytes)
        {
            SaveHelper.SaveInt32(saveBytes, CurrentWindowResolution.X);
            SaveHelper.SaveInt32(saveBytes, CurrentWindowResolution.Y);

            SaveHelper.SaveInt32(saveBytes, (int)CurrentAntiAliasing);
            SaveHelper.SaveInt32(saveBytes, (int)CurrentSMAAQuality);
            SaveHelper.SaveInt32(saveBytes, (int)CurrentFXAAQuality);
            SaveHelper.SaveInt32(saveBytes, (int)CurrentHDRType);

            SaveHelper.SaveBool(saveBytes, UseInstancedLightVolumes);
            SaveHelper.SaveBool(saveBytes, BloomEnabled);

            SaveHelper.SaveBool(saveBytes, ShowConsole);
        }

        public static void OnLoad(byte[] loadBytes, ref int index)
        {
            CurrentWindowResolution.X = SaveHelper.LoadInt32(loadBytes, ref index);
            CurrentWindowResolution.Y = SaveHelper.LoadInt32(loadBytes, ref index);

            CurrentAntiAliasing = (AntiAliasing)SaveHelper.LoadInt32(loadBytes, ref index);
            CurrentSMAAQuality = (SMAAQuality)SaveHelper.LoadInt32(loadBytes, ref index);
            CurrentFXAAQuality = (FXAAQuality)SaveHelper.LoadInt32(loadBytes, ref index);
            CurrentHDRType = (HDRType)SaveHelper.LoadInt32(loadBytes, ref index);

            UseInstancedLightVolumes = SaveHelper.LoadBool(loadBytes, ref index);
            BloomEnabled = SaveHelper.LoadBool(loadBytes, ref index);

            ShowConsole = SaveHelper.LoadBool(loadBytes, ref index);
        }

        public static void OnSave(StreamWriter writer)
        {
            writer.WriteLine("rez_x " + CurrentWindowResolution.X);
            writer.WriteLine("rez_y " + CurrentWindowResolution.Y);
                        
            writer.WriteLine("aa " + (int)CurrentAntiAliasing);
            writer.WriteLine("smaa_quality " + (int)CurrentSMAAQuality);
            writer.WriteLine("fxaa_quality " + (int)CurrentFXAAQuality);
            writer.WriteLine("hdr " + (int)CurrentHDRType);
                        
            writer.WriteLine("instanced_light_volumes " + UseInstancedLightVolumes);
            writer.WriteLine("bloom " + BloomEnabled);

            writer.WriteLine("render_dist " + RenderDistance);

            writer.WriteLine("show_console " + ShowConsole);
        }

        public static void OnLoad(List<string> lines, GraphicsDeviceManager graphics)
        {
            foreach (string line in lines)
            {
                string[] split = line.Split(' ');
                if (split[0] == "rez_x")
                    int.TryParse(split[1], out CurrentWindowResolution.X);
                if (split[0] == "rez_y")
                    int.TryParse(split[1], out CurrentWindowResolution.Y);

                if (split[0] == "aa")
                    if (int.TryParse(split[1], out int aa))
                        CurrentAntiAliasing = (AntiAliasing)aa;
                if (split[0] == "smaa_quality")
                    if (int.TryParse(split[1], out int smaa))
                        CurrentSMAAQuality = (SMAAQuality)smaa;
                if (split[0] == "fxaa_quality")
                    if (int.TryParse(split[1], out int fxaa))
                        CurrentFXAAQuality = (FXAAQuality)fxaa;
                if (split[0] == "hdr")
                    if (int.TryParse(split[1], out int hdr))
                        CurrentHDRType = (HDRType)hdr;

                if (split[0] == "instanced_light_volumes")
                    bool.TryParse(split[1], out UseInstancedLightVolumes);
                if (split[0] == "bloom")
                    bool.TryParse(split[1], out BloomEnabled);

                if (split[0] == "render_dist")
                    int.TryParse(split[1], out RenderDistance);

                if (split[0] == "debug_timescale")
                    float.TryParse(split[1], out DEBUGTimescale);

                if (split[0] == "show_connsole")
                    bool.TryParse(split[1], out ShowConsole);
            }

            //graphics.PreferredBackBufferWidth = CurrentWindowResolution.X;
            //graphics.PreferredBackBufferHeight = CurrentWindowResolution.Y;
        }
    }
}
