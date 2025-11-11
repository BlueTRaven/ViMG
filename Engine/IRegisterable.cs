using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace ViMG
{
	public interface IRegisterable
	{
		public string Identifier { get; }

		public virtual void LoadContent(GraphicsDevice device) { }
	}
}
