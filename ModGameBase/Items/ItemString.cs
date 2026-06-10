using BrUtility;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Rendering;

namespace ViMG.Items
{
    public class ItemString : Item
    {
        public ItemString() : base("string")
        {
            name = "String";
            description = "A simple piece of string extracted from Fibrous Plants.";
        }

        protected override ClientItem ClientInit()
        {
            return new ClientItem(this, new RectangleF(16, 32, 16, 16));
        }
    }
}
