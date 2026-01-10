using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Common
{
    public class CameraOrthographic : Camera
    {
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

        protected override Matrix GetViewMatrixInternal()
        {

            return base.GetViewMatrixInternal();
        }

		protected override Matrix GetProjectionMatrixInternal()
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
