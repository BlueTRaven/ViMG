using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViMG
{
    public class CameraPerspective : Camera
	{
		private Matrix projectionMatrix;

		private float fovDegrees;

		public CameraPerspective(Vector3 startPosition, Vector3 startRotation, Vector3 startScale, float fovDegrees, float near, float far) : base(startPosition, startRotation, startScale, near, far)
        {
			this.fovDegrees = fovDegrees;
			projectionDirty = true;
        }

		protected override Matrix GetProjectionMatrixInternal()
		{
			if (projectionDirty)
			{
				projectionMatrix = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(fovDegrees), (float)Main.WindowResolution.X / (float)Main.WindowResolution.Y, Near, Far);
				projectionDirty = false;
			}

			return projectionMatrix;
		}
	}
}
