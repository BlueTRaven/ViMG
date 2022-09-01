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
				cameraDirty = true;
				frustumDirty = true;
			}
		}
		private Vector3 rotation;
		public Vector3 Rotation {
			get => rotation;
			set
			{
				rotation = value;
				cameraDirty = true;
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
				cameraDirty = true;
				frustumDirty = true;
			}
		}

		private bool cameraDirty;
		private Matrix camera;

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
			}
		}

		private bool frustumDirty;
		private BoundingFrustum frustum;

		public Camera(Vector3 startPosition, Vector3 startRotation, Vector3 startScale, float near, float far)
		{
			this.position = startPosition;
			this.rotation = startRotation;
			this.scale = startScale;

			this.near = near;
			this.far = far;

			cameraDirty = true;
		}

		public Matrix GetViewMatrix()
		{
			if (cameraDirty)
			{
				camera = Matrix.CreateTranslation(-Position) *
					Matrix.CreateRotationZ(Rotation.Z) *
					Matrix.CreateRotationY(Rotation.Y) *
					Matrix.CreateRotationX(Rotation.X) *
					Matrix.CreateScale(scale);
				cameraDirty = false;
			}

			return camera;
		}

		public abstract Matrix GetProjectionMatrix();

		public BoundingFrustum GetFrustum()
		{
			if (frustum == null)
				frustum = new BoundingFrustum(GetViewMatrix() * GetProjectionMatrix());

			if (frustumDirty || cameraDirty)
			{
				frustum.Matrix = GetViewMatrix() * GetProjectionMatrix();
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
	}
}
