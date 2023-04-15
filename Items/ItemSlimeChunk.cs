using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Cubes;
using ViMG.Entities.Renderers;

namespace ViMG.Items
{
	public class ItemSlimeChunk : Item
	{
		public ItemSlimeChunk() : base("slime_chunk", StaticMaterials.Items, new RectangleF(0, 112, 16, 16))
		{
			name = "Slime Chunk";
			description = "A gooey chunk of slime. Smells surprisingly nice.";
		}
	}
}
