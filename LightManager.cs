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

		private StructuredBuffer structuredBuffer;
		private int version;
		private int lastUploadedVersion;

		public readonly struct Data
        {
			public readonly Vector3 position;
			public readonly float start;
			public readonly Vector3 color;
			public readonly float end;

			public Data(Vector3 position, float start, float end, Vector3 color)
            {
				this.position = position;
				this.start = start;
				this.end = end;
				this.color = color;
			}

			public Data(Light light)
            {
				this.position = light.position;
				this.start = light.start;
				this.end = light.end;
				this.color = light.color.ToVector3();
			}
		}

		public readonly struct Light
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

		private RenderTargetCube[] lightsCubemaps = new RenderTargetCube[MAX_LIGHTS];

		private Light[] lights = new Light[MAX_LIGHTS];
		private Data[] datas = new Data[MAX_LIGHTS];

		public LightManager(GraphicsDevice device)
        {
			for (int i = 0; i < MAX_LIGHTS; i++)
				lightsCubemaps[i] = new RenderTargetCube(device, 256, false, SurfaceFormat.Single, DepthFormat.Depth24);
        }

		public int MakeLight(Vector3 position, float start, float end, Color color)
		{
			for (int i = 0; i < MAX_LIGHTS; i++)
			{ 
				if (!lights[i].active)
				{
					lights[i] = new Light(position, start, end, color, i);

					version++;
					return i;
				}
			}

			return -1;
		}

		public void KillLight(int index)
		{
			if (lights[index].active)
			{
				lights[index] = new Light();
				version++;
			}
		}

		public void UpdateDatas(Effect effect)
		{
			if (structuredBuffer == null)
				structuredBuffer = new StructuredBuffer(effect.GraphicsDevice, typeof(Data), MAX_LIGHTS, BufferUsage.WriteOnly, ShaderAccess.Read);

			if (version != lastUploadedVersion)
			{
				lastUploadedVersion = version;

				for (int i = 0; i < MAX_LIGHTS; i++)
				{
					datas[i] = new Data(lights[i]);
				}

				structuredBuffer.SetData(datas);

				effect.Parameters["Lights"].SetValue(structuredBuffer);
			}
		}

		public void DrawShadowmap(GraphicsDevice device, World world)
        {

        }
	}
}
