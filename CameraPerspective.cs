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
		public readonly float FOVDegrees;
		public float HalfFOV => FOVDegrees / 2f;

		public CameraPerspective(Vector3 startPosition, Vector3 startRotation, Vector3 startScale, float fovDegrees, float near, float far) : base(startPosition, startRotation, startScale, near, far)
        {
			this.FOVDegrees = fovDegrees;
			projectionDirty = true;
        }

		protected override Matrix GetProjectionMatrixInternal()
		{
			if (projectionDirty)
			{
				projectionMatrix = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(FOVDegrees), 
					(float)Options.CurrentWindowResolution.X / (float)Options.CurrentWindowResolution.Y, Near, Far);
				projectionDirty = false;
			}

			return projectionMatrix;
		}
	}
}
