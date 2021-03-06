using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace ViMG
{
	public class DenseHitboxArray
	{
		public readonly struct Hitbox 
		{
			public readonly int index;
			public readonly bool active;

			public readonly Rectangle3D bounds;
			public readonly Vector3 direction;

			public readonly int group;

			public readonly int damage;
			public readonly float knockback;

			public Hitbox(int index)
			{
				this.index = index;
				active = false;
				bounds = new Rectangle3D();
				direction = Vector3.Zero;
				group = -1;
				damage = -1;
				knockback = -1;
			}

			public Hitbox(int index, Rectangle3D bounds, Vector3 direction, int group, int damage, float knockback)
			{
				this.index = index;
				active = true;
				this.bounds = bounds;
				this.direction = direction;
				this.group = group;
				this.damage = damage;
				this.knockback = knockback;
			}

			public Hitbox(Hitbox old, Rectangle3D bounds)
			{
				this.index = old.index;
				active = true;
				this.bounds = bounds;
				this.direction = old.direction;
				this.group = old.group;
				this.damage = old.damage;
				this.knockback = old.knockback;
			}
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

		public int Add(Rectangle3D bounds, Vector3 direction, int group, int damage, float knockback)
		{
			for (int i = 0; i < capacity; i++)
			{
				ref Hitbox hitbox = ref rectangles[i];

				if (!hitbox.active)
				{
					hitbox = new Hitbox(i, bounds, direction, group, damage, knockback);

					return i;
				}
			}

			Grow();
			return Add(bounds, direction, group, damage, knockback);
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

		public ref Hitbox Get(int index)
		{
			return ref rectangles[index];
		}

		public void Remove(int index)
		{
			rectangles[index] = new Hitbox(index);
		}

		public void Update(int index, Rectangle3D bounds)
		{
			if (rectangles[index].active)
			{
				rectangles[index] = new Hitbox(rectangles[index], bounds);
			}
		}

		public Hitbox[] GetAll()
		{
			return rectangles;
		}
	}
}
