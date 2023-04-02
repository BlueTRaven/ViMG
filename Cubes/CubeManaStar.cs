using BrUtility;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViMG.Cubes
{
    public class CubeManaStar : Cube
    {
        public CubeManaStar() : base("mana_star", new RectangleF(112, 112, 16, 16), Color.White, 16, 0)
        {
            Name = "Mana Star";
            Description = "Cube item version. Unobtainable.";

            Transparency = TransparencyValue.Transparent;
        }
    }
}
