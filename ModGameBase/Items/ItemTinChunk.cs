using BrUtility;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Rendering;

namespace ViMG.Items
{
    public class ItemTinChunk : Item
	{
		public ItemTinChunk() : base("tin_chunk")
		{
            name = "Tin Ore Chunk";
			description = "A weighty chunk of tin ore. It's too raw to be used for anything.";
		}

        public override ClientItem ClientInit()
        {
            return new ClientItem(this, new RectangleF(64, 16, 16, 16));
        }
	}
}
