using BrUtility;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Rendering;

namespace ViMG.Items
{
    public class ItemFlask : Item
	{
		public ItemFlask() : base("flask_empty")
		{
            name = "Empty Flask";
			description = "An empty flask without substance to fill its void.";
		}

        protected override ClientItem ClientInit()
        {
            return new ClientItem(this, new RectangleF(0, 96, 16, 16));
        }
	}
}
