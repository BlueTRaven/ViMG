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

        public static void CenterMouse()
        {
            Mouse.SetPosition(CurrentWindowResolution.X / 2, CurrentWindowResolution.Y / 2);
        }
    }
}
