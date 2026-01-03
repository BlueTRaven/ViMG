using BrUtility;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViMG.Cubes
{
    public class CubeCryptCobbles : Cube
    {
        public CubeCryptCobbles() : base("stone_crypt_cobbles", 3, 1)
        {
            Name = "Crypt Cobbles";
            Description = "Perhaps once used for paving the ground of crypts.";

            Client = new(this, new RectangleF(128, 96, 16, 16), Color.White);
        }
    }
}
