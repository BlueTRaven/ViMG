using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace ViMG.Items
{
	public abstract class Item
	{
		public Texture2D Texture;
		public RectangleF SourceRect;

		public virtual bool LeftClick(Player player, Vector3 facing)
		{
			return false;
		}

		public virtual bool RightClick(Player player, Vector3 facing)
		{
			return false;
		}

		public virtual void Draw(GraphicsDevice device, Player player, Vector3 facing)
		{

		}
	}
}
