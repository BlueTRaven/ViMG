using BrUtility;
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
        public Texture2D Underground;
        public Texture2D Weather;

        public Skybox()
        {
            Day = DrawHelper.BlackPixel;
            Night = DrawHelper.BlackPixel;
            Underground = DrawHelper.BlackPixel;
            Weather = DrawHelper.BlackPixel;
        }
    }
}
