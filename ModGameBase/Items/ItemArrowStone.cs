using BrUtility;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Rendering;

namespace ViMG.Items
{
    public class ItemArrowStone : Item
	{
		public ItemArrowStone() : base("ammo_arrow_stone")
		{
            Tags.Add("ammo_arrow");
		}

        public override ClientItem ClientInit()
        {
            return new ClientItem(this, new RectangleF(16, 48, 16, 16));
        }
	}
}
