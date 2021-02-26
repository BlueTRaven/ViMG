using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace ViMG
{
	public class DenseHitboxArray
	{
		public struct Hitbox 
		{
			public Rectangle3D bounds;
			public Vector3 direction;
			public bool active;

			public int group;
		}

		private Hitbox[] rectangles;

		private int capacity;
		public int Capacity => capacity;
		private readonly int grow;

		public DenseHitboxArray(int capacity, int grow = 32)
		{
			this.capacity = capacity;
			this.grow = grow;

			rectangles = new Hitbox[capacity];
		}

		public int Add(Rectangle3D bounds, Vector3 direction, int group)
		{
			for (int i = 0; i < capacity; i++)
			{
				ref Hitbox hitbox = ref rectangles[i];

				if (!hitbox.active)
				{
					hitbox = new Hitbox()
					{
						bounds = bounds,
						direction = direction,
						active = true,
						group = group
					};

					return i;
				}
			}

			Grow();
			return Add(bounds, direction, group);
		}

		private void Grow()
		{
			Hitbox[] old = rectangles;

			capacity += grow;

			rectangles = new Hitbox[capacity];

			for (int i = 0; i < old.Length; i++)
			{
				rectangles[i] = old[i];
			}
		}

		public Hitbox Get(int index)
		{
			return rectangles[index];
		}

		public void Remove(int index)
		{
			rectangles[index].active = false;
		}

		public void Update(int index, Rectangle3D bounds)
		{
			if (rectangles[index].active)
			{
				rectangles[index].bounds = bounds;
			}
		}

		public Hitbox[] GetAll()
		{
			return rectangles;
		}
	}
}
