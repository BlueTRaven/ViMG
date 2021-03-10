using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace ViMG
{
	public class LightManager
	{
		private const int MAX_LIGHTS = 16;

		private readonly struct Light
		{
			public readonly Vector3 position;
			public readonly float start;
			public readonly float end;
			public readonly Color color;

			public readonly int index;
			public readonly bool active;

			public Light(Vector3 position, float start, float end, Color color, int index)
			{
				this.position = position;
				this.start = start;
				this.end = end;
				this.color = color;

				this.index = index;
				active = true;
			}
		}

		private Light[] lights = new Light[MAX_LIGHTS];

		private Vector3[] positions = new Vector3[16] { Vector3.Zero, Vector3.Zero, Vector3.Zero, Vector3.Zero, Vector3.Zero, Vector3.Zero, Vector3.Zero, Vector3.Zero, Vector3.Zero, Vector3.Zero, Vector3.Zero, Vector3.Zero, Vector3.Zero, Vector3.Zero, Vector3.Zero, Vector3.Zero };
		private float[] starts = new float[16] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
		private float[] ends = new float[16] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
		private Vector3[] colors = new Vector3[16] { Vector3.Zero, Vector3.Zero, Vector3.Zero, Vector3.Zero, Vector3.Zero, Vector3.Zero, Vector3.Zero, Vector3.Zero, Vector3.Zero, Vector3.Zero, Vector3.Zero, Vector3.Zero, Vector3.Zero, Vector3.Zero, Vector3.Zero, Vector3.Zero, };

		public int MakeLight(Vector3 position, float start, float end, Color color)
		{
			for (int i = 0; i < MAX_LIGHTS; i++)
			{ 
				if (!lights[i].active)
				{
					lights[i] = new Light(position, start, end, color, i);

					return i;
				}
			}

			return -1;
		}

		public void KillLight(int index)
		{
			if (lights[index].active)
				lights[index] = new Light();
		}

		public void SetToEffect(Effect effect)
		{
			for (int i = 0; i < MAX_LIGHTS; i++)
			{
				positions[i] = lights[i].position;
				starts[i] = lights[i].start;
				ends[i] = lights[i].end;
				colors[i] = lights[i].color.ToVector3();
			}

			effect.Parameters["LightsPosition"].SetValue(positions);
			effect.Parameters["LightsStart"].SetValue(starts);
			effect.Parameters["LightsEnd"].SetValue(ends);
			effect.Parameters["LightsColor"].SetValue(colors);
		}
	}
}
