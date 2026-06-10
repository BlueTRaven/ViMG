using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monogame.ImGuiNet
{
    public sealed class TextureInfo
    {
        public Texture2D Texture { get; set; }
        public bool IsManaged { get; set; }
    }
}
