using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViMG
{
    public class CameraOrthographic : Camera
    {
		private bool projectionDirty;
		private Matrix projectionMatrix;

		private float left, right, top, bottom;

		public CameraOrthographic(Vector3 startPosition, Vector3 startRotation, Vector3 startScale, float left, float right, float top, float bottom, float near, float far) : 
            base(startPosition, startRotation, startScale, near, far)
        {
			this.left = left;
			this.right = right;
			this.top = top;
			this.bottom = bottom;
			projectionDirty = true;
		}

		public override Matrix GetProjectionMatrix()
		{
			if (projectionDirty)
			{
				projectionMatrix = Matrix.CreateOrthographicOffCenter(left, right, bottom, top, Near, Far);
				projectionDirty = false;
			}

			return projectionMatrix;
		}
	}
}
