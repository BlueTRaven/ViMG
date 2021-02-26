using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace ViMG
{
	public class FogHandler
	{
		private readonly Effect effect;

		public FogHandler(Effect effect)
		{
			this.effect = effect;
		}

		public void Set(float start, float end, Color color)
		{
			effect.Parameters["FogStart"].SetValue(start);
			effect.Parameters["FogEnd"].SetValue(end);
			//effect.Parameters["FogColor"].SetValue(color.ToVector3());
		}

		public void Disable()
		{
			Set(Main.camera.Far, Main.camera.Far, Color.White);
		}
	}
}
