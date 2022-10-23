using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Buffs;
using ViMG.Cubes;
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

			public readonly Buff.BuffInstance[] applyBuffs;

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
				applyBuffs = null;
			}

			public Hitbox(int index, IHitboxOwner owner, Rectangle3D bounds, Vector3 direction, Group group, int damage, float knockback, bool canInteract, Buff.BuffInstance[] applyBuffs)
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

				this.applyBuffs = applyBuffs;
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

				this.applyBuffs = old.applyBuffs;
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

		public int Add(IHitboxOwner owner, Rectangle3D bounds, Vector3 direction, Group group, int damage, float knockback, bool canInteract = true, Buff.BuffInstance[] applyBuffs = null)
		{
			for (int i = 0; i < capacity; i++)
			{
				ref Hitbox hitbox = ref hitboxes[i];

				if (!hitbox.active)
				{
					hitbox = new Hitbox(i, owner, bounds, direction, group, damage, knockback, canInteract, applyBuffs ?? Array.Empty<Buff.BuffInstance>());

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

				for (int i = 0; i < hitboxes.Length; i++)
				{
					ref Hitbox hitbox = ref hitboxes[i];
					if (i == index || !hitbox.active)
						continue;

					if (hitbox.bounds.Intersects(bounds))
					{
						hitboxes[index].owner.OnInteractWithOther(hitboxes[index], hitbox);

						//OnInteractWithOther may occasionally remove this hitbox (for instance, a non-piercing projectile).
						//This is behavior we want to support,
						//so we simply check to see if the hitbox is active after this interaction. If it isn't, then break.
						if (!hitboxes[index].active)
							break;
					}
				}
			}
            else
            {
				Console.WriteLine("Tried to update hitbox id {0}, which was inactive.", index);
            }
		}

		public Hitbox[] GetAll()
		{
			return hitboxes;
		}

		private static (VertexBuffer VBO, IndexBuffer IBO) debugMesh;

		public void DrawDebug(GraphicsDevice device)
        {
			if (debugMesh.VBO == null)
            {
				List<VertexCube> vertices = new List<VertexCube>();
				List<int> indices = new List<int>();
				MeshHelper.MakeCubeVertsVertexPositionColorTextureNormal(Vector3.Zero, Vector3.One, MeshHelper.CubeFace.ALL, Color.White, vertices, indices);
 				debugMesh = MeshHelper.MakeSimplerMesh(device, vertices, indices);
            }

			for (int i = 0; i < hitboxes.Length; i++)
            {
				if (hitboxes[i].active)
                {
					float distance = (hitboxes[i].bounds.Position - Main.camera.Position).Length();
					Matrix transform = Matrix.CreateScale(hitboxes[i].bounds.Size) *
						Matrix.CreateTranslation(hitboxes[i].bounds.Position);

					Main.Renderer.DrawsTransparentPass.Add(new Rendering.RendererDeferred.TransparentDraw(distance, transform, DrawHelper.WhitePixel, DrawHelper.WhitePixel,
						debugMesh.VBO, debugMesh.IBO,
						tintColor: Color.Red * 0.5f));
                }
            }
        }
	}
}
