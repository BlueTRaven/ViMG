using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Cubes;

namespace ViMG.Items
{
	public class ItemSlimeChunk : Item
	{
		public ItemSlimeChunk() : base("slime_chunk", Main.assetsManager.GetAsset<Texture2D>("swrod"), new RectangleF(0, 112, 16, 16))
		{
			Name = "Slime Chunk";
			Description = "A gooey chunk of slime. Smells surprisingly nice.";
		}
	}
}
