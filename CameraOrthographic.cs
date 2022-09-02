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

		public CameraOrthographic(Vector3 position, Vector3 direction, float near, float far, Matrix view, Matrix projection) : base(position, direction, Vector3.One, near, far, true)
        {
			this.viewMatrix = view;
			this.projectionMatrix = projection;
			viewDirty = false;
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
