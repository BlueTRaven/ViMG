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
    public class ItemPaper : Item
    {
        public ItemPaper() : base("paper", new RectangleF(128, 16, 16, 16))
        {
            name = "Paper";
            description = "A simple piece of parchment paper without anything written on it.";
        }
    }
}
