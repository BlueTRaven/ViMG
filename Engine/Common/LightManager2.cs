using BrUtility;
using Microsoft.Xna.Framework;
using SharpDX.MediaFoundation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Common
{
    public class LightManager2
    {
        public readonly struct Light
        {
            public readonly Vector3 position;
            public readonly float start;
            public readonly float end;
            public readonly Vector4 color;

            public readonly bool isShadowmapped;
            public readonly bool useNDotL;

            public readonly int index;
            public readonly bool active;

            public Light(Vector3 position, float start, float end, Vector4 color, bool isShadowmapped, bool useNDotL, int index)
            {
                this.position = position;
                this.start = start;
                this.end = end;
                this.color = color;

                this.isShadowmapped = isShadowmapped;
                this.useNDotL = useNDotL;

                this.index = index;
                active = true;
            }

            // Used to determine if a shadowmapped light is dirty.
            public int GetLightHash()
            {
                return HashCode.Combine(position, end);
            }
        }

        public record struct LightConfig
        {
            public required Vector3 position;
            public required float min;
            public required float max;
            public required Color color;

            public int GetLightHash()
            {
                return HashCode.Combine(position, max);
            }
        }

        public struct ShadowmappedLight
        {
            public bool dirty;
            public Light light;
            public int time;
        }

        private Light[] lights = new Light[LightManager.LightsMax];
        private ShadowmappedLight[] lightsShadowmapped = new ShadowmappedLight[LightManager.LightsShadowmappedMax];

        private FastList<int> freeLights;
        private FastList<int> freeLightsS;

        public LightManager2()
        {
            freeLights = new FastList<int>(lights.Length);
            for (int i = lights.Length - 1; i >= 0; i--)
            {
                freeLights.Add(i);
            }

            freeLightsS = new FastList<int>(lightsShadowmapped.Length);
            for (int i = lightsShadowmapped.Length - 1; i >= 0; i--)
            {
                freeLightsS.AddAssumeCapacity(i);
            }
        }

        public void Reset()
        {
            Array.Fill(lights, new Light());

            freeLights.Clear();
            for (int i = lights.Length - 1; i >= 0; i--)
            {
                freeLights.Add(i);
            }

            freeLightsS.Clear();

            for (int i = lightsShadowmapped.Length - 1; i >= 0; i--)
            {
                if (lightsShadowmapped[i].time < 0)
                {
                    freeLightsS.Add(i);
                } else lightsShadowmapped[i].time--;
                //freeLights.Add(i);
            }
        }

        public void Add(LightConfig config)
        {
            if (freeLights.Length > 0)
            {
                int index = freeLights.Buffer[freeLights.Length - 1];
                freeLights.RemoveAt(freeLights.Length - 1);

                lights[index] = new Light(config.position, config.min, config.max, config.color.ToVector4(), false, false, index);
            }
        }

        public void AddShadowmapped(LightConfig config)
        {
            // Is the light already present?
            for (int i = 0; i < LightManager.LightsShadowmappedMax; i++)
            {
                if (lightsShadowmapped[i].light.GetLightHash() == config.GetLightHash())
                {
                    lightsShadowmapped[i].time += 1;
                    return;
                }
            }

            // If not, attempt to add a new shadowmapped light
            if (freeLightsS.Length > 0)
            {
                int index = freeLightsS.Buffer[freeLightsS.Length - 1];
                freeLightsS.RemoveAt(freeLightsS.Length - 1);

                lightsShadowmapped[index] = new ShadowmappedLight 
                {
                    light = new Light(config.position, config.min, config.max, config.color.ToVector4(), false, false, index), 
                    time = 1,
                    dirty = true,
                };
            }
            // Otherwise just add a new ordinary light
            else 
                Add(config);
        }

        public Light Get(int index)
        {
            return lights[index];
        }

        public Light GetShadowmapped(int index)
        {
            return lightsShadowmapped[index].light;
        }

        public bool GetShadowmappedLightDirty(int index)
        {
            var dirty = lightsShadowmapped[index].dirty;
            lightsShadowmapped[index].dirty = false;

            return dirty;
        }
    }
}
