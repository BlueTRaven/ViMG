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
		private readonly Effect unlitEffect;

		public Texture2D ColorHeightMap;

		public FogManager(Effect effect, Effect unlitEffect)
		{
			this.effect = effect;
			this.unlitEffect = unlitEffect;
		}

		public void Set(float start, float end, Texture2D colorMapDay, Texture2D colorMapNight, float percentBetween)
		{
			effect.Parameters["FogStart"].SetValue(start);
			effect.Parameters["FogEnd"].SetValue(end);
			unlitEffect.Parameters["FogStart"].SetValue(start);
			unlitEffect.Parameters["FogEnd"].SetValue(end);

			ColorHeightMap = colorMapDay;
			effect.Parameters["TextureHeightFogMapDay"].SetValue(colorMapDay);
			effect.Parameters["TextureHeightFogMapNight"].SetValue(colorMapNight);
			effect.Parameters["HeightFogMapLerp"].SetValue(percentBetween);
			unlitEffect.Parameters["TextureHeightFogMapDay"].SetValue(colorMapDay);
			unlitEffect.Parameters["TextureHeightFogMapNight"].SetValue(colorMapNight);
			unlitEffect.Parameters["HeightFogMapLerp"].SetValue(percentBetween);
		}

		public void Enable()
        {
			effect.Parameters["EnableFog"].SetValue(true);
			unlitEffect.Parameters["EnableFog"].SetValue(true);
        }

		public void Disable()
		{
			effect.Parameters["EnableFog"].SetValue(false);
			unlitEffect.Parameters["EnableFog"].SetValue(false);
			//Set(Main.camera.Far, Main.camera.Far, DrawHelper.WhitePixel, DrawHelper.WhitePixel, 0);
		}
	}
}
