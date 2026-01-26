using BrUtility;
using Engine.Clients;
using Engine.Items;
using Engine.Networking;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SharpDX.Direct2D1.Effects;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Cubes;
using ViMG.Rendering;
using ViMG.VertexDeclarations;

namespace ViMG.Items
{
    public abstract class Item : IRegisterable
	{
		public const float DEFAULT_USE_TIME = 0.5f;
		public const float DEFAULT_USE_ANIM_TIME = 0.5f;

		public struct RangedAttackStats
		{
			public float projectileSpeed;
			public float projectileGravity;
			public AttackStats attackStats;

			public RangedAttackStats(AttackStats attackStats, float projectileSpeed, float projectileGravity = 1f)
            {
				this.attackStats = attackStats;
				this.projectileSpeed = projectileSpeed;
				this.projectileGravity = projectileGravity;
            }

			public string GetTooltip()
			{
				return String.Format("{0}" +
					"Projectile Speed: {1}\n", attackStats.GetTooltip(), projectileSpeed);
			}
		}

		public struct MagicAttackStats
        {
			public int magicUse;
			public AttackStats attackStats;

			public MagicAttackStats(AttackStats attackStats, int magicUse)
            {
				this.magicUse = magicUse;
				this.attackStats = attackStats;
            }

			public bool CanUse(Player player)
            {
				return player.Magic >= magicUse;
            }

			public void Use(Player player)
            {
				player.Magic -= magicUse;
            }

			public string GetTooltip()
            {
				return String.Format("{0}" +
					"Magic Use: {1}\n", attackStats.GetTooltip(), magicUse);
            }
        }

		public struct MeleeAttackStats
        {
			public float range;
			public AttackStats attackStats;

			public MeleeAttackStats(AttackStats attackStats, float range)
            {
                this.attackStats = attackStats;
                this.range = range;
            }

			public string GetTooltip()
			{
				return String.Format("{0}" +
					"{1} Range\n", attackStats.GetTooltip(), Util.RangeToString(DamageType.Melee, range));
			}
		}

		public struct AttackStats
		{
			public DamageType damageType;
			public ActionStats actionStats;
			public int damage;
			public float knockback;
			//float size;

			public AttackStats(DamageType damageType, float useTime, int damage, float knockback)
			{
				this.damageType = damageType;
				this.actionStats = new ActionStats()
				{
					useTime = useTime,
					useAnimTime = useTime
				};
				this.damage = damage;
				this.knockback = knockback;
			}

			public AttackStats(DamageType damageType, ActionStats actionStats, int damage, float knockback)
			{
                this.damageType = damageType;
				this.actionStats = actionStats;
                this.damage = damage;
                this.knockback = knockback;
            }

			public string GetTooltip()
            {
				return String.Format("{0} Weapon\n" +
					"Damage: {1}\n" +
					"Knockback: {2}%\n" +
					"{3} Speed\n", 
					damageType.ToString(), damage, knockback * 100, Util.CooldownToString(actionStats.useTime));
            }
		}

		public string Identifier { get; private set; }
		public HashSet<string> Tags = new HashSet<string>();

		protected string name = "";
		protected string description = "";

		public int Id = -1;

        public ClientItem Client = new ClientItem(null, new RectangleF(112, 112, 16, 16));

		public Item(string identifier)
		{
			this.Identifier = identifier;
		}
		
		public virtual string GetName(ItemInstance item)
		{
			return name;
		}

		public virtual string GetDescription(ItemInstance item)
		{
			return description;
		}

		public void SetId(int id)
		{
			this.Id = id;
		}

		public virtual bool LeftClick(Player player, Inventory inventory, int index, Vector3 facing, out ActionStats actionStats)
		{
			actionStats = new ActionStats()
			{
				useTime = DEFAULT_USE_TIME,
				useAnimTime = DEFAULT_USE_ANIM_TIME
			};
			return false;
		}

		public virtual bool RightClick(Player player, Inventory inventory, int index, Vector3 facing, out ActionStats actionStats)
		{
            actionStats = new ActionStats()
            {
                useTime = DEFAULT_USE_TIME,
                useAnimTime = DEFAULT_USE_ANIM_TIME
            };
            return false;
		}

		public virtual void StartHold(Player player, Inventory inventory, int index) { }

		public virtual void EndHold(Player player, Inventory inventory, int newIndex) { }

		public virtual void Hold(Player player, Inventory inventory, int index) { }

		public virtual void AccumulateStats(Player player, Inventory inventory, int index, ref Player.AccumulatedStats stats, ref SetBonus.SetBonusInstance bonus) { }

		public virtual void OnAttack(Player player, Inventory inventory, int index) { }

		public virtual void OnDealDamage(Player player, Inventory inventory, int index, HitboxManager.Hitbox otherHitbox) { }
	}

	public class ClientItem
	{
        protected const float MESH_SIZE = Cube.CUBE_SCALE / 2f;
        protected static VerySimpleMesh meshItemQuadInWorld;

        public RendererDeferred.DrawMaterial? Material = null;
        public readonly RectangleF SourceRect;
        protected float scale = 1f;
        protected Vector2 origin = new Vector2(MESH_SIZE / Cube.PIXELS_PER_CUBE * 4);
        protected bool flipXInHand;

        protected Item item;

        public ClientItem(Item item, RectangleF sourceRect, RendererDeferred.DrawMaterial? material = null, bool flipXInHand = false, float scale = 1)
		{
            this.item = item;
            this.SourceRect = sourceRect;
            this.Material = material;
            this.flipXInHand = flipXInHand;
            this.scale = scale;
        }

        public virtual RendererDeferred.DrawMaterial GetMaterial()
        {
            Material ??= StaticMaterials.Items;
            return Material.Value;
        }

        public virtual void StartHold(ClientStates client, BasicState player, Inventory inventory, int index) { }

        public virtual void EndHold(ClientStates client, BasicState player, Inventory inventory, int newIndex) { }

        public virtual void Hold(ClientStates client, BasicState player, Inventory inventory, int index) { }

        public void DrawInHand(GraphicsDevice device, RendererDeferred renderer, ItemInstance item, BasicState player, Vector3 facing)
        {
            float widthScale = 1;
            float heightScale = 1;
            //we need to correct the aspect ratio of the quad since it's only 1x1 and textures may not be.
            if (SourceRect.width > SourceRect.height)
            {
                widthScale = SourceRect.width / SourceRect.height;
            }
            else if (SourceRect.height > SourceRect.width)
            {
                heightScale = SourceRect.height / SourceRect.width;
            }

            Vector3 correctedScale = new Vector3(widthScale, heightScale, 1);
            //if (!player.IsLocalPlayer) correctedScale *= new Vector3(4);

            DrawInWorld(device, renderer, item, Player.GetHeldMatrix(player, origin,
                correctedScale * new Vector3(scale, scale, 1)));
        }

        public virtual void DrawInInventory(SpriteBatch batch, ItemInstance item, Vector2 position, float scale)
        {
            //fit to frame
            scale *= 16 / MathF.Max(SourceRect.width, SourceRect.height);

            batch.Draw(GetMaterial().Diffuse, position, SourceRect.ToRectangle(), Color.White, 0, Vector2.Zero, scale, SpriteEffects.None, 0.86f);
        }

        public virtual void DrawInWorld(GraphicsDevice device, RendererDeferred renderer, ItemInstance item, Matrix transform)
        {
            if (meshItemQuadInWorld.IBO == null)
                MakeMesh(device);

            RectangleF sourceRect = SourceRect;
            if (flipXInHand)
            {
                sourceRect.x = sourceRect.x + sourceRect.width;
                sourceRect.width = -sourceRect.width;
            }

            renderer.AddOpaqueDraw(new Rendering.RendererDeferred.GBufferDraw(GetMaterial(),
                meshItemQuadInWorld,
                transform, sourceRect));
        }

        protected static void MakeMesh(GraphicsDevice device)
        {
            Vector3 min = Vector3.Zero;
            Vector3 max = new Vector3(MESH_SIZE, MESH_SIZE, 0);

            Vector3 a = new Vector3(min.X, min.Y, 0);
            Vector3 b = new Vector3(min.X, max.Y, 0);
            Vector3 c = new Vector3(max.X, max.Y, 0);
            Vector3 d = new Vector3(max.X, min.Y, 0);

            FastList<VertexCube> vertices = new FastList<VertexCube>();
            List<int> indices = new List<int>();

            Vector2 atx = new Vector2(0, 1);
            Vector2 btx = new Vector2(0, 0);
            Vector2 ctx = new Vector2(1, 0);
            Vector2 dtx = new Vector2(1, 1);

            int offset = vertices.Length;
            indices.Add(offset + 0);
            indices.Add(offset + 1);
            indices.Add(offset + 3);
            indices.Add(offset + 1);
            indices.Add(offset + 2);
            indices.Add(offset + 3);

            vertices.Add(new VertexCube(a, Color.White, atx, new Vector3(0, 0, 1)));
            vertices.Add(new VertexCube(b, Color.White, btx, new Vector3(0, 0, 1)));
            vertices.Add(new VertexCube(c, Color.White, ctx, new Vector3(0, 0, 1)));
            vertices.Add(new VertexCube(d, Color.White, dtx, new Vector3(0, 0, 1)));

            offset = vertices.Length;
            indices.Add(offset + 0);
            indices.Add(offset + 3);
            indices.Add(offset + 1);
            indices.Add(offset + 1);
            indices.Add(offset + 3);
            indices.Add(offset + 2);

            vertices.Add(new VertexCube(a, Color.White, atx, new Vector3(0, 0, 1)));
            vertices.Add(new VertexCube(b, Color.White, btx, new Vector3(0, 0, 1)));
            vertices.Add(new VertexCube(c, Color.White, ctx, new Vector3(0, 0, 1)));
            vertices.Add(new VertexCube(d, Color.White, dtx, new Vector3(0, 0, 1)));

            meshItemQuadInWorld = VerySimpleMesh.Opaque(device, new ChunkRenderMesher.VertexAttributes(vertices, indices)); //MeshHelper.MakeSimplerMesh(device, vertices.ToVertexOpaquePass(), indices); //new SimpleMesh<VertexCube, int>(device, vertices, indices);
        }
    }
}
