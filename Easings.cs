using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViMG
{
    public static class Easings
    {
		public static float EaseLinear(float t)
		{
			return t;
		}

		public static float Ease(float t)
		{
			return t == 0 ? 0 : (float)Math.Pow(2f, 10f * t - 10f);
			//return 1 - (float)Math.Sqrt(1 - (float)Math.Pow(t, 2));
		}

		public static float EaseReverse(float t)
		{
			return 1 - Ease(t);
		}
	}
}
