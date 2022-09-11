using BrUtility;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViMG.Items
{
    public class ItemString : Item
    {
        public ItemString() : base("string", Main.assetsManager.GetAsset<Texture2D>("swrod"), new RectangleF(16, 32, 16, 16))
        {
            name = "String";
            description = "A simple piece of string extracted from Fibrous Plants.";
        }
    }
}
