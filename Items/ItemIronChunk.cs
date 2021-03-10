using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace ViMG.Items
{
	public class ItemIronChunk : Item
	{
		public ItemIronChunk() : base("iron_chunk", Main.assetsManager.GetAsset<Texture2D>("swrod"), new RectangleF(0, 16, 16, 16))
		{
			Name = "Iron Ore Chunk";
			Description = "A weighty chunk of iron ore. It's too raw to be used for anything.";
		}
	}
}
