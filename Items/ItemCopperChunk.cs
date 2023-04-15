using BrUtility;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Entities.Renderers;

namespace ViMG.Items
{
	public class ItemCopperChunk : Item
	{
		public ItemCopperChunk() : base("copper_chunk", StaticMaterials.Items, new RectangleF(80, 16, 16, 16))
		{
			name = "Copper Ore Chunk";
			description = "A weighty chunk of copper ore. It's too raw to be used for anything.";
		}
	}
}
