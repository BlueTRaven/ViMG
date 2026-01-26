using BrUtility;
using Engine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViMG
{
    public class Skybox
    {
        public Texture2D Day;
        public Texture2D Night;
        public float DayNightAlpha; // 0=day 1=night
        public Texture2D Underground;
        public Texture2D Weather;
        public float WeatherAlpha;
        public Color WeatherColor = Color.White;

        public Skybox()
        {
            if (!GlobalState.IsHeadless)
            {
                Day = DrawHelper.BlackPixel;
                Night = DrawHelper.BlackPixel;
                Underground = DrawHelper.BlackPixel;
                Weather = DrawHelper.BlackPixel;
            }
        }
    }
}
