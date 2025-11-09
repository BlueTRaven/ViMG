using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Cubes;

namespace ViMG
{
	public static class CollisionHelper
	{
		private static Vector3[] compass = new Vector3[6]
		{
			new Vector3(0, -1, 0),	//up
			new Vector3(0, 1, 0),  //down
			new Vector3(1, 0, 0),	//left
			new Vector3(-1, 0, 0),	//right
			new Vector3(0, 0, 1),	//back
			new Vector3(0, 0, -1)	//front
		};

		public static bool CheckCollision(Rectangle3D bounds, in Vector3 position, in float radius, out Vector3 change)
		{
			change = Vector3.Zero;

			Vector3 halfExtents = bounds.Size / 2f;
			Vector3 center = bounds.Position + halfExtents;

			Vector3 diff = position - center;
			Vector3 clamped = Vector3.Clamp(diff, -halfExtents, halfExtents);

			Vector3 closest = center + clamped;

			diff = closest - position;

			bool intersects = diff.Length() < radius;

			if (intersects)
			{
				float max = 0f;
				int direction = -1;

				for (int i = 0; i < 6; i++)
				{
					float dot = Vector3.Dot(Vector3.Normalize(diff), compass[i]);
					if (dot > max)
					{
						max = dot;
						direction = i;
					}
				}

				if (direction == 0 || direction == 1)
				{
					float penetration = radius - Math.Abs(diff.Y);
					if (direction == 0)
						change.Y += penetration;
					else change.Y -= penetration;
				}
				else if (direction == 2 || direction == 3)
				{
					float penetration = radius - Math.Abs(diff.X);
					if (direction == 2)
						change.X -= penetration;
					else change.X += penetration;
				}
				else if (direction == 4 || direction == 5)
				{
					float penetration = radius - Math.Abs(diff.Z);
					if (direction == 4)
						change.Z -= penetration;
					else change.Z += penetration;
				}
			}

			return intersects;
		}

		public struct Contact
        {
            public bool Intersecting;
            public Vector3 Normal;
            public float Penetration;
        }

        public static bool TestStaticAABBAABB(Rectangle3D a, Rectangle3D b, out Contact contact, float epsilon = Cube.CUBE_SCALE / 20f)
        {
            // [Minimum Translation Vector]
            float mtvDistance = float.MaxValue;             // Set current minimum distance (max float value so next value is always less)
            Vector3 mtvAxis = new Vector3();                // Axis along which to travel with the minimum distance

            contact = new Contact();

            // [Axes of potential separation]
            // • Each shape must be projected on these axes to test for intersection:
            //          
            // (1, 0, 0)                    A0 (= B0) [X Axis]
            // (0, 1, 0)                    A1 (= B1) [Y Axis]
            // (0, 0, 1)                    A1 (= B2) [Z Axis]

            // [X Axis]
            if (!TestAxisStatic(Vector3.UnitX, a.Position.X, a.FarPosition.X, b.Position.X, b.FarPosition.X, ref mtvAxis, ref mtvDistance))
            {
                return false;
            }

            // [Y Axis]
            if (!TestAxisStatic(Vector3.UnitY, a.Position.Y, a.FarPosition.Y, b.Position.Y, b.FarPosition.Y, ref mtvAxis, ref mtvDistance))
            {
                return false;
            }

            // [Z Axis]
            if (!TestAxisStatic(Vector3.UnitZ, a.Position.Z, a.FarPosition.Z, b.Position.Z, b.FarPosition.Z, ref mtvAxis, ref mtvDistance))
            {
                return false;
            }

            contact.Intersecting = true;

            // Calculate Minimum Translation Vector (MTV) [normal * penetration]
            contact.Normal = Vector3.Normalize(mtvAxis);

            // Multiply the penetration depth by itself plus a small increment
            // When the penetration is resolved using the MTV, it will no longer intersect
            contact.Penetration = MathF.Sqrt(mtvDistance) * (1f + epsilon);

            return true;
        }

        private static bool TestAxisStatic(Vector3 axis, float minA, float maxA, float minB, float maxB, ref Vector3 mtvAxis, ref float mtvDistance)
        {
            // [Separating Axis Theorem]
            // • Two convex shapes only overlap if they overlap on all axes of separation
            // • In order to create accurate responses we need to find the collision vector (Minimum Translation Vector)   
            // • Find if the two boxes intersect along a single axis 
            // • Compute the intersection interval for that axis
            // • Keep the smallest intersection/penetration value
            float axisLengthSquared = Vector3.Dot(axis, axis);

            // If the axis is degenerate then ignore
            if (axisLengthSquared < 1.0e-8f)
            {
                return true;
            }

            // Calculate the two possible overlap ranges
            // Either we overlap on the left or the right sides
            float d0 = (maxB - minA);   // 'Left' side
            float d1 = (maxA - minB);   // 'Right' side

            // Intervals do not overlap, so no intersection
            if (d0 <= 0.0f || d1 <= 0.0f)
            {
                return false;
            }

            // Find out if we overlap on the 'right' or 'left' of the object.
            float overlap = (d0 < d1) ? d0 : -d1;

            // The mtd vector for that axis
            Vector3 sep = axis * (overlap / axisLengthSquared);

            // The mtd vector length squared
            float sepLengthSquared = Vector3.Dot(sep, sep);

            // If that vector is smaller than our computed Minimum Translation Distance use that vector as our current MTV distance
            if (sepLengthSquared < mtvDistance)
            {
                mtvDistance = sepLengthSquared;
                mtvAxis = sep;
            }

            return true;
        }
    }
}
