using BrUtility;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace ViMG.Items
{
	public class ItemTinChunk : Item
	{
		public ItemTinChunk() : base("tin_chunk", Main.assetsManager.GetAsset<Texture2D>("swrod"), new RectangleF(64, 16, 16, 16))
		{
			name = "Tin Ore Chunk";
			description = "A weighty chunk of tin ore. It's too raw to be used for anything.";
		}
	}
}
