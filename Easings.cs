using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ViMG
{
    public static class Easings
	{
		public static float EaseInSine(float t)
		{
            return 1 - float.Cos((t * MathF.PI) / 2f);
        }

		public static float EaseOutSine(float t)
		{
            return float.Sin((t * MathF.PI) / 2f);
        }

		public static float EaseInOutSine(float t)
		{
            return -(float.Cos(MathF.PI * t) - 1f) / 2f;
        }

        public static float EaseInOutElastic(float t)
		{
			const float c5 = (2f * MathF.PI) / 4.5f;

			return t == 0
			  ? 0
			  : t == 1f
			  ? 1
			  : t < 0.5f
			  ? -(float.Pow(2f, 20f * t - 10f) * float.Sin((20f * t - 11.125f) * c5)) / 2f
			  : (float.Pow(2f, -20f * t + 10f) * float.Sin((20f * t - 11.125f) * c5)) / 2f + 1f;
		}

		public static float EaseInExpo(float t)
		{
			return t == 0 ? 0 : (float)Math.Pow(2f, 10f * t - 10f);
		}

        public static float EaseLinear(float t)
        {
            return t;
        }

        public static float EaseReverse(float t)
		{
			return 1 - EaseInExpo(t);
		}
	}
}
