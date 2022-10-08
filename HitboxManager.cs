using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Entities;

namespace ViMG
{
	public class HitboxManager
	{
		public enum Group
        {
			INVALID,
			PLAYER_TAKE = 1 << 0,
			PLAYER_DEAL = 1 << 1,
			ENEMYHOSTILE_TAKE = 1 << 2,
			ENEMYHOSTILE_DEAL = 1 << 3,
			ENEMYHOSTILE_BOTH = ENEMYHOSTILE_DEAL | ENEMYHOSTILE_TAKE,
			NEUTRAL_DEAL = 1 << 4
        }
		public const int GROUP_PLAYER_TAKE_SOURCE = 0;
		public const int GROUP_ENEMYHOSTILE_SOURCE = 1;
		public const int GROUP_PLAYER_DEAL_SOURCE = 2;
		public const int GROUP_NEUTRAL_SOURCE = 3;

		public readonly struct Hitbox 
		{
			public readonly int index;
			public readonly bool active;

			public readonly Rectangle3D bounds;
			public readonly Vector3 direction;

			public readonly IHitboxOwner owner;
			public readonly Group group;

			public readonly int damage;
			public readonly float knockback;

			public readonly bool canInteract;

			public Hitbox(int index)
			{
				this.index = index;
				active = false;
				owner = null;
				bounds = new Rectangle3D();
				direction = Vector3.Zero;
				group = Group.INVALID;
				damage = -1;
				knockback = -1;
				canInteract = false;
			}

			public Hitbox(int index, IHitboxOwner owner, Rectangle3D bounds, Vector3 direction, Group group, int damage, float knockback, bool canInteract)
			{
				this.index = index;
				active = true;
				this.owner = owner;
				this.bounds = bounds;
				this.direction = direction;
				this.group = group;
				this.damage = damage;
				this.knockback = knockback;

				this.canInteract = canInteract;
			}

			public Hitbox(Hitbox old, Rectangle3D bounds, bool canInteract)
			{
				this.index = old.index;
				active = true;
				this.owner = old.owner;
				this.bounds = bounds;
				this.direction = old.direction;
				this.group = old.group;
				this.damage = old.damage;
				this.knockback = old.knockback;

				this.canInteract = canInteract;
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

		public int Add(IHitboxOwner owner, Rectangle3D bounds, Vector3 direction, Group group, int damage, float knockback, bool canInteract = true)
		{
			for (int i = 0; i < capacity; i++)
			{
				ref Hitbox hitbox = ref hitboxes[i];

				if (!hitbox.active)
				{
					hitbox = new Hitbox(i, owner, bounds, direction, group, damage, knockback, canInteract);

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

		public void Update(int index, Rectangle3D bounds, bool canInteract = true)
		{
			if (hitboxes[index].active)
			{
				hitboxes[index] = new Hitbox(hitboxes[index], bounds, canInteract);
			}

			for (int i = 0; i < hitboxes.Length; i++)
			{
				ref Hitbox hitbox = ref hitboxes[i];
				if (i == index || !hitbox.active)
					continue;

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

		public void DrawDebug(GraphicsDevice device)
        {

        }
	}
}
