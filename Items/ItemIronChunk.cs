using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Entities.Renderers;

namespace ViMG.Items
{
	public class ItemIronChunk : Item
	{
		public ItemIronChunk() : base("iron_chunk", StaticMaterials.Items, new RectangleF(48, 16, 16, 16))
		{
			name = "Iron Ore Chunk";
			description = "A weighty chunk of iron ore. It's too raw to be used for anything.";
		}
	}
}
