using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViMG
{
	public abstract class Camera
	{
		private Vector3 position;
		public Vector3 Position
		{
			get => position;
			set
			{
				position = value;
				viewDirty = true;
				frustumDirty = true;
			}
		}
		private Vector3 rotation;
		public Vector3 Rotation {
			get => rotation;
			set
			{
				rotation = value;
				viewDirty = true;
				frustumDirty = true;
			}
		}
		private Vector3 scale;
		public Vector3 Scale
		{
			get => scale;
			set
			{
				scale = value;
				viewDirty = true;
				frustumDirty = true;
			}
		}

		protected bool viewDirty;
		protected Matrix viewMatrix;
		protected bool projectionDirty;
		protected Matrix projectionMatrix;

		public Vector3 Forward
		{
			get
			{
				Matrix mat = Matrix.CreateRotationX(-Rotation.X) *
						Matrix.CreateRotationY(-Rotation.Y) *
						Matrix.CreateRotationZ(-Rotation.Z);

				return Vector3.Transform(new Vector3(0, 0, 1), mat);
			}
		}

		public Vector3 ForwardYawOnly
		{
			get
			{
				Matrix mat = Matrix.CreateRotationY(-Rotation.Y);

				return Vector3.Transform(new Vector3(0, 0, 1), mat);
			}
		}

		public Vector3 Up
		{
			get
			{
				Matrix mat = Matrix.CreateRotationX(-Rotation.X) *
							Matrix.CreateRotationY(-Rotation.Y) *
							Matrix.CreateRotationZ(-Rotation.Z);

				return Vector3.Transform(new Vector3(0, 1, 0), mat);
			}
		}

		public Vector3 UpYawOnly
		{
			get
			{
				Matrix mat = Matrix.CreateRotationY(-Rotation.Y);

				return Vector3.Transform(new Vector3(0, 1, 0), mat);
			}
		}

		public Vector3 Right
		{
			get
			{
				Matrix mat = Matrix.CreateRotationX(-Rotation.X) *
							Matrix.CreateRotationY(-Rotation.Y) *
							Matrix.CreateRotationZ(-Rotation.Z);

				return Vector3.Transform(new Vector3(1, 0, 0), mat);
			}
		}

		private float near;
		public float Near
		{
			get => near;
			set
			{
				near = value;
				frustumDirty = true;
				projectionDirty = true;
			}
		}
		private float far;

        private readonly bool rotationAsDirection;

        public float Far
		{
			get => far;
			set
			{
				far = value;
				frustumDirty = true;
				projectionDirty = true;
			}
		}

		private bool frustumDirty;
		private BoundingFrustum frustum;

		public Camera(Vector3 startPosition, Vector3 startRotation, Vector3 startScale, float near, float far, bool rotationAsDirection = false)
		{
			this.position = startPosition;
			this.rotation = startRotation;
			this.scale = startScale;

			this.near = near;
			this.far = far;
            this.rotationAsDirection = rotationAsDirection;
            viewDirty = true;
		}

		public Matrix GetViewMatrix()
        {
			return GetViewMatrixInternal();
        }

		protected virtual Matrix GetViewMatrixInternal()
		{
			if (viewDirty)
			{
				if (!rotationAsDirection)
				{
					viewMatrix = Matrix.CreateTranslation(-Position) *
						Matrix.CreateRotationZ(Rotation.Z) *
						Matrix.CreateRotationY(Rotation.Y) *
						Matrix.CreateRotationX(Rotation.X) *
						Matrix.CreateScale(scale);
				}
                else
                {
					viewMatrix = Matrix.CreateLookAt(-Position, -Position + Rotation, new Vector3(0, 1, 0)) * Matrix.CreateScale(scale);
                }
				viewDirty = false;
			}

			return viewMatrix;
		}

		public Matrix GetProjectionMatrix()
        {
			return GetProjectionMatrixInternal();
        }

		protected abstract Matrix GetProjectionMatrixInternal();

		public BoundingFrustum GetFrustum()
		{
			if (frustum == null)
				frustum = new BoundingFrustum(GetViewMatrixInternal() * GetProjectionMatrixInternal());

			if (frustumDirty || viewDirty)
			{
				frustum.Matrix = GetViewMatrixInternal() * GetProjectionMatrixInternal();
				frustumDirty = false;
			}

			return frustum;
		}

		public bool FrustumIntersects(Rectangle3D bounds)
		{
			GetFrustum();

			BoundingBox box = new BoundingBox(bounds.Position, bounds.Position + bounds.Size);

			return frustum.Intersects(box);
		}

		public bool FrustumContains(Vector3 position)
		{
			GetFrustum();

			return frustum.Contains(position) == ContainmentType.Contains || frustum.Contains(position) == ContainmentType.Intersects;
		}

		public void MarkDirty()
        {
			viewDirty = true;
			projectionDirty = true;
			frustumDirty = true;
        }
	}
}
