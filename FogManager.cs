using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace ViMG
{
	public class FogManager
	{
		private readonly Effect effect;

		public Texture2D ColorHeightMap;

		public FogManager(Effect effect)
		{
			this.effect = effect;
		}

		public void Set(float start, float end, Texture2D colorMapDay, Texture2D colorMapNight, float percentBetween)
		{
			effect.Parameters["FogStart"].SetValue(start);
			effect.Parameters["FogEnd"].SetValue(end);

			ColorHeightMap = colorMapDay;
			effect.Parameters["TextureHeightFogMapDay"].SetValue(colorMapDay);
			effect.Parameters["TextureHeightFogMapNight"].SetValue(colorMapNight);
			effect.Parameters["HeightFogMapLerp"].SetValue(percentBetween);
		}

		public void Disable()
		{
			Set(Main.camera.Far, Main.camera.Far, DrawHelper.WhitePixel, DrawHelper.WhitePixel, 0);
		}
	}
}
