using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;

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
	}
}
