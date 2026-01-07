using BrUtility;
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
				viewDirtyThisFrame = true;
				frustumDirty = true;
			}
		}
		private Vector3 rotationEuler;
		public Vector3 RotationEuler 
		{
			get => rotationEuler;
			set
			{
                rotationEuler = value;
				rotation = Quaternion.CreateFromYawPitchRoll(rotationEuler.X, rotationEuler.Y, rotationEuler.Z);
				//rotation = Quaternion.CreateFromRotationMatrix(mat);
				viewDirty = true;
				frustumDirty = true;
				viewDirtyThisFrame = true;
			}
		}
		private Quaternion rotation;
		public Quaternion Rotation
		{
			get => rotation;
			set
			{
                rotationEuler = EngineMathHelper.QuaternionToYawPitchRoll(value.ToNumerics());
				rotation = value;
				viewDirty = true;
                frustumDirty = true;
                viewDirtyThisFrame = true;
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
				viewDirtyThisFrame = true;
			}
		}

		public float Yaw => RotationEuler.X;
		public float Pitch => RotationEuler.Y;
		// we don't include roll because we never use that

		private bool viewDirtyThisFrame;
		protected bool viewDirty;
		protected Matrix viewMatrix;
		private bool projectionDirtyThisFrame;
		protected bool projectionDirty;
		protected Matrix projectionMatrix;

		public bool IsDirty => viewDirtyThisFrame || projectionDirtyThisFrame;

		public Vector3 Forward
		{
			get
			{
                Matrix mat = Matrix.CreateFromQuaternion(Rotation);

                return Vector3.Transform(new Vector3(0, 0, 1), mat);
            }
		}

		public Vector3 ForwardYawOnly
		{
			get
			{
                var newQuat = Rotation;
                newQuat.X = 0;
                newQuat.Z = 0;
                var mag = float.Sqrt(newQuat.W * newQuat.W + newQuat.Y * newQuat.Y);
                newQuat.W /= mag;
                newQuat.Y /= mag;
                Matrix mat = Matrix.CreateFromQuaternion(newQuat);
                //Matrix mat = Matrix.CreateRotationY(-Rotation.Y);

                return Vector3.Transform(new Vector3(0, 0, 1), mat);
            }
		}

		public Vector3 Up
		{
			get
			{
                Matrix mat = Matrix.CreateFromQuaternion(Rotation);

                return Vector3.Transform(new Vector3(0, 1, 0), mat);
            }
		}

		public Vector3 Right
		{
			get
			{
                Matrix mat = Matrix.CreateFromQuaternion(Rotation);

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
				projectionDirtyThisFrame = true;
			}
		}
		private float far;

        public float Far
		{
			get => far;
			set
			{
				far = value;
				frustumDirty = true;
				projectionDirty = true;
				projectionDirtyThisFrame = true;
			}
		}

		private bool frustumDirty;
		private BoundingFrustum frustum;

		public Camera(Vector3 startPosition, Vector3 startRotation, Vector3 startScale, float near, float far)
		{
			this.position = startPosition;
			this.rotationEuler = startRotation;
			this.rotation = Quaternion.CreateFromYawPitchRoll(rotationEuler.X, rotationEuler.Y, rotationEuler.Z);
			this.scale = startScale;

			this.near = near;
			this.far = far;
            viewDirty = true;
		}

		public void FrameBegin()
        {
			this.viewDirtyThisFrame = false;
			this.projectionDirtyThisFrame = false;
        }

		public Matrix GetViewMatrix()
        {
			return GetViewMatrixInternal();
        }

		protected virtual Matrix GetViewMatrixInternal()
		{
			if (viewDirty)
			{
				viewMatrix = Matrix.CreateTranslation(-Position) *
					//Matrix.CreateFromQuaternion(rotation) *
					Matrix.CreateRotationZ(RotationEuler.Z) *
					Matrix.CreateRotationY(-RotationEuler.X) *
					Matrix.CreateRotationX(-RotationEuler.Y) *
					Matrix.CreateScale(scale);
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
			projectionDirtyThisFrame = true;
			viewDirtyThisFrame = true;
			viewDirty = true;
			projectionDirty = true;
			frustumDirty = true;
        }
	}
}
