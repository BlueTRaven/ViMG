using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Buffs;
using ViMG.Cubes;
using ViMG.Entities;
using ViMG.IMGUIImpl;
using ViMG.Rendering;
using ViMG.VertexDeclarations;
using static Engine.Common.LightManager;

namespace ViMG
{
    public class HitboxManager
	{
		public enum Group
        {
			INVALID,
			/*TAKE = 1 << 0,
			DEAL = 1 << 1,
			PLAYER = 1 << 2,
			ENEMY = 1 << 3,*/
			PLAYER_TAKE = GROUP_SOURCE_PLAYER | DAMAGE_TYPE_TAKE,
			PLAYER_DEAL = GROUP_SOURCE_PLAYER | DAMAGE_TYPE_DEAL,
			ENEMYHOSTILE_TAKE = GROUP_SOURCE_ENEMY | DAMAGE_TYPE_TAKE,
			ENEMYHOSTILE_DEAL = GROUP_SOURCE_ENEMY | DAMAGE_TYPE_DEAL,
			ENEMYHOSTILE_BOTH = ENEMYHOSTILE_DEAL | ENEMYHOSTILE_TAKE,
			NEUTRAL_DEAL = GROUP_SOURCE_PLAYER | GROUP_SOURCE_ENEMY | DAMAGE_TYPE_TAKE
		}
		
		public const int GROUP_SOURCE_MASK = GROUP_SOURCE_PLAYER | GROUP_SOURCE_ENEMY;

		public const int DAMAGE_TYPE_TAKE = 1 << 0;
		public const int DAMAGE_TYPE_DEAL = 1 << 1;
		public const int GROUP_SOURCE_PLAYER = 1 << 2;
		public const int GROUP_SOURCE_ENEMY = 1 << 3;

		public readonly struct Hitbox 
		{
			public readonly int index;
			public readonly bool active;

			public readonly Rectangle3D bounds;
			public readonly Vector3 direction;

			public readonly IHitboxOwner owner;
			public readonly IHitboxOwner manager;	//The entity that manages this hitbox, which might be different from the owner (in the case of Projectiles).
			public readonly Group group;

			public readonly int damage;
			public readonly float knockback;

			public readonly bool canInteract;

			public readonly Buff.BuffInstance[] applyBuffs;

			public readonly int data;
			public readonly int inventorySlot;

			public Hitbox(int index)
			{
				this.index = index;
				active = false;
				owner = null;
				manager = null;
				bounds = new Rectangle3D();
				direction = Vector3.Zero;
				group = Group.INVALID;
				damage = -1;
				knockback = -1;
				canInteract = false;
				applyBuffs = null;
				inventorySlot = -1;
				data = 0;
			}

			public Hitbox(int index, IHitboxOwner owner, Rectangle3D bounds, Vector3 direction, Group group, int damage, float knockback, bool canInteract, Buff.BuffInstance[] applyBuffs)
			{
				this.index = index;
				active = true;
				this.owner = owner;
				this.manager = null;
				this.bounds = bounds;
				this.direction = direction;
				this.group = group;
				this.damage = damage;
				this.knockback = knockback;

				this.canInteract = canInteract;

				this.applyBuffs = applyBuffs ?? Array.Empty<Buff.BuffInstance>();
				inventorySlot = -1;
				data = 0;
			}

			public Hitbox(int index, IHitboxOwner owner, IHitboxOwner manager, Rectangle3D bounds, Vector3 direction, Group group, int damage, float knockback, bool canInteract, Buff.BuffInstance[] applyBuffs, int inventorySlot, int data)
			{
				this.index = index;
				active = true;
				this.owner = owner;
				this.manager = manager;
				this.bounds = bounds;
				this.direction = direction;
				this.group = group;
				this.damage = damage;
				this.knockback = knockback;

				this.canInteract = canInteract;

				this.applyBuffs = applyBuffs;
				this.inventorySlot = inventorySlot;
				this.data = data;
			}

            public Hitbox(int index, HitboxParameters parameters)
            {
                this.index = index;
                active = true;
                this.owner = parameters.owner;
                this.manager = parameters.manager;
                this.bounds = parameters.bounds;
                this.direction = parameters.direction;
                this.group = parameters.stats.group;
                this.damage = parameters.stats.damage;
                this.knockback = parameters.stats.knockback;

                this.canInteract = parameters.canInteract;

                this.applyBuffs = parameters.stats.applyBuffs;
                this.inventorySlot = parameters.stats.inventorySlot;
                this.data = parameters.stats.data;
            }

            public Hitbox(Hitbox old, Rectangle3D bounds, bool canInteract)
			{
				this.index = old.index;
				active = true;
				this.owner = old.owner;
				this.manager = old.manager;
				this.bounds = bounds;
				this.direction = old.direction;
				this.group = old.group;
				this.damage = old.damage;
				this.knockback = old.knockback;

				this.canInteract = canInteract;

				this.applyBuffs = old.applyBuffs;
				this.inventorySlot = old.inventorySlot;
				this.data = old.data;
			}

			public static Hitbox Invalid = new Hitbox();
		}

		public record struct HitboxParameters
		{
            public Rectangle3D bounds;
            public Vector3 direction;

            public required IHitboxOwner owner;
            public IHitboxOwner manager;    //The entity that manages this hitbox, which might be different from the owner (in the case of Projectiles).

			public HitboxStats stats;

            public bool canInteract;
        }

		public record struct HitboxStats 
		{
            public Group group;

            public int damage;
            public float knockback;

            public Buff.BuffInstance[] applyBuffs;

            public int data;
            public int inventorySlot;
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

		public int Add(IHitboxOwner owner, Rectangle3D bounds, Vector3 direction, Group group, int damage, float knockback, bool canInteract = true, Buff.BuffInstance[] applyBuffs = null, IHitboxOwner manager = null, int inventorySlot = -1, int data = 0)
		{
			for (int i = 0; i < capacity; i++)
			{
				ref Hitbox hitbox = ref hitboxes[i];

				if (!hitbox.active)
				{
					hitbox = new Hitbox(i, owner, manager, bounds, direction, group, damage, knockback, canInteract, applyBuffs ?? Array.Empty<Buff.BuffInstance>(), inventorySlot, data);

					return i;
				}
			}

			Grow();
			return Add(owner, bounds, direction, group, damage, knockback);
		}

		public int Add(HitboxParameters parameters)
		{
            for (int i = 0; i < capacity; i++)
            {
                ref Hitbox hitbox = ref hitboxes[i];

                if (!hitbox.active)
                {
                    hitbox = new Hitbox(i, parameters);

                    return i;
                }
            }

            Grow();
            return Add(parameters);
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
					//Note that these are intentionally copies
					Hitbox ourHitbox = hitboxes[index];
					Hitbox otherHitbox = hitboxes[i];

					if (i == index || !ourHitbox.active || !otherHitbox.active)
						continue;

					if (otherHitbox.bounds.Intersects(bounds))
					{
						ourHitbox.owner.OnInteractWithOther(ourHitbox, otherHitbox);
						ourHitbox.manager?.OnInteractWithOther(ourHitbox, otherHitbox);

						otherHitbox.owner.OnInteractWithOther(otherHitbox, ourHitbox);
						otherHitbox.manager?.OnInteractWithOther(otherHitbox, ourHitbox);
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

		private static VerySimpleMesh debugMesh;

		[ConsoleCommandVar("rsv_hitbox_draw", "Singleplayer only. Draws hitboxes. Default = false")]
		public static bool DoDebugDraw = false;

		public void DrawDebug(GraphicsDevice device)
        {
			if (!DoDebugDraw) return;

			if (debugMesh.IBO == null)
            {
                FastList<VertexCube> vertices = new FastList<VertexCube>();
                List<int> indices = new List<int>();
				MeshHelper.MakeCubeVertsVertexPositionColorTextureNormal(Vector3.Zero, Vector3.One, MeshHelper.CubeFace.ALL, Color.White, vertices, indices);
                debugMesh = VerySimpleMesh.Transparent(device, ChunkRenderMesher.VertexAttributes.Transparent(vertices, indices));
                //debugMesh = MeshHelper.MakeSimplerMesh(device, vertices.ToVertexTransparentPass(), indices);
            }

			for (int i = 0; i < hitboxes.Length; i++)
            {
				if (hitboxes[i].active)
				{
					float distance = (hitboxes[i].bounds.Position - Main.camera.Position).Length();
					Matrix transform = Matrix.CreateScale(hitboxes[i].bounds.Size) *
						Matrix.CreateTranslation(hitboxes[i].bounds.Position);

					Main.Renderer.AddTransparentDraw(new Rendering.RendererDeferred.TransparentDraw(distance, 
						new RendererDeferred.DrawMaterial(DrawHelper.WhitePixel), debugMesh,
						transform, tintColor: Color.Red * 0.5f));
				}
            }
        }
	}
}
