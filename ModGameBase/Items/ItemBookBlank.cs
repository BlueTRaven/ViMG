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
    public class ItemBookBlank : Item
    {
        public ItemBookBlank() : base("book_blank")
        {
            name = "Book";
            description = "A book made of paper bound together.\n" +
                "Record your journeys, your discoveries of foreign magics, or perhaps... long-lost love...";
        }

        public override ClientItem ClientInit()
        {
            return new ClientItem(this, new RectangleF(48, 32, 16, 16));
        }
    }
}
