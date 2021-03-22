using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Entities;

namespace ViMG
{
	public class HitboxManager
	{
		public readonly struct Hitbox 
		{
			public readonly int index;
			public readonly bool active;

			public readonly Rectangle3D bounds;
			public readonly Vector3 direction;

			public readonly IHitboxOwner owner;
			public readonly int group;

			public readonly int damage;
			public readonly float knockback;

			public Hitbox(int index)
			{
				this.index = index;
				active = false;
				owner = null;
				bounds = new Rectangle3D();
				direction = Vector3.Zero;
				group = -1;
				damage = -1;
				knockback = -1;
			}

			public Hitbox(int index, IHitboxOwner owner, Rectangle3D bounds, Vector3 direction, int group, int damage, float knockback)
			{
				this.index = index;
				active = true;
				this.owner = owner;
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
				this.owner = old.owner;
				this.bounds = bounds;
				this.direction = old.direction;
				this.group = old.group;
				this.damage = old.damage;
				this.knockback = old.knockback;
			}

			public static Hitbox Invalid = new Hitbox();
		}

		private Hitbox[] hitboxes;

		private int capacity;
		public int Capacity => capacity;
		private readonly int grow;

		public HitboxManager(int capacity, int grow = 32)
		{
			this.capacity = capacity;
			this.grow = grow;

			hitboxes = new Hitbox[capacity];
		}

		public int Add(IHitboxOwner owner, Rectangle3D bounds, Vector3 direction, int group, int damage, float knockback)
		{
			for (int i = 0; i < capacity; i++)
			{
				ref Hitbox hitbox = ref hitboxes[i];

				if (!hitbox.active)
				{
					hitbox = new Hitbox(i, owner, bounds, direction, group, damage, knockback);

					return i;
				}
			}

			Grow();
			return Add(owner, bounds, direction, group, damage, knockback);
		}

		private void Grow()
		{
			Hitbox[] old = hitboxes;

			capacity += grow;

			hitboxes = new Hitbox[capacity];

			for (int i = 0; i < old.Length; i++)
			{
				hitboxes[i] = old[i];
			}
		}

		public ref Hitbox Get(int index)
		{
			if (index < 0)
				return ref Hitbox.Invalid;
			else return ref hitboxes[index];
		}

		public void Remove(int index)
		{
			hitboxes[index] = new Hitbox(index);
		}

		public void Update(int index, Rectangle3D bounds)
		{
			if (hitboxes[index].active)
			{
				hitboxes[index] = new Hitbox(hitboxes[index], bounds);
			}

			for (int i = 0; i < hitboxes.Length; i++)
			{
				ref Hitbox hitbox = ref hitboxes[i];

				if (hitbox.bounds.Intersects(bounds))
				{
					hitboxes[index].owner.OnInteractWithOther(hitboxes[index], hitbox);
				}
			}
		}

		public Hitbox[] GetAll()
		{
			return hitboxes;
		}
	}
}
