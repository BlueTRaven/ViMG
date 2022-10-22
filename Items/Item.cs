using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Cubes;

namespace ViMG.Items
{
	public abstract class Item : IRegisterable
	{
		public struct AttackStats
		{
			public float cooldownTime;
			public int damage;
			public float knockback;
			//float size;

			public AttackStats(float cooldownTime, int damage, float knockback)
			{
				this.cooldownTime = cooldownTime;
				this.damage = damage;
				this.knockback = knockback;
			}

			public string GetTooltip()
            {
				return String.Format("Damage: {0}\n" +
					"Knockback: {1}%\n" +
					"{2} Speed\n", damage, knockback * 100, CooldownToString());
            }

			private string CooldownToString()
            {
				if (cooldownTime <= 0.125f)
					return "Blisteringly Fast";
				else if (cooldownTime <= 0.25f)
					return "Very Fast";
				else if (cooldownTime < 0.5)
					return "Fast";

				if (cooldownTime >= 2.5f)
					return "Snail";
				else if (cooldownTime >= 2f)
					return "Very Slow";
				else if (cooldownTime >= 1.5f)
					return "Sluggish";
				else if (cooldownTime >= 1f)
					return "Slow";
				else if (cooldownTime >= 0.5f)
					return "Ordinary";

				return "UNKNOWN???";
            }
		}

		public readonly Texture2D Texture;
		public readonly RectangleF SourceRect;

		protected bool flipXInHand;

		public string Identifier { get; private set; }
		public HashSet<string> Tags = new HashSet<string>();

		protected string name = "";
		protected string description = "";

		public int Id = -1;

		protected static SimpleMesh<VertexCube, int> meshItemQuadInWorld;

		public Item(string identifier, Texture2D texture, RectangleF sourceRect)
		{
			this.Identifier = identifier;
			this.Texture = texture;
			this.SourceRect = sourceRect;
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

		public virtual bool LeftClick(Player player, Inventory inventory, int index, Vector3 facing, out float itemCooldownTime)
		{
			itemCooldownTime = 0.5f;
			return false;
		}

		public virtual bool RightClick(Player player, Inventory inventory, int index, Vector3 facing, out float itemCooldownTime)
		{
			itemCooldownTime = 0.5f;
			return false;
		}

		public virtual void StartHold(Player player, Inventory inventory, int index) { }

		public virtual void EndHold(Player player, Inventory inventory, int newIndex) { }

		public virtual void Hold(Player player, Inventory inventory, int index) { }

		public virtual void AccumulateStats(Player player, Inventory inventory, int index, ref Player.AccumulatedStats stats, ref SetBonus.SetBonusInstance bonus) { }

		public virtual void OnAttack(Player player, Inventory inventory, int index) { }

		public virtual void OnDealDamage(Player player, Inventory inventory, int index, IHitboxOwner hit) { }

		public void DrawInHand(GraphicsDevice device, ItemInstance item, Player player, Vector3 facing)
		{
			DrawInWorld(device, player.GetWorld(), item, player.GetHeldMatrix());
		}

		public virtual void DrawInInventory(SpriteBatch batch, ItemInstance item, Vector2 position, float scale)
		{
			if (SourceRect.width > SourceRect.height)
			{
				//fix aspect ratio
				scale *= SourceRect.height / SourceRect.width;
			}
			else if (SourceRect.height > SourceRect.width)
			{
				//fix aspect ratio
				scale *= SourceRect.width / SourceRect.height;
			}

			//fit to frame
			scale *= 16 / MathF.Max(SourceRect.width, SourceRect.height);

			batch.Draw(Texture, position, SourceRect.ToRectangle(), Color.White, 0, Vector2.Zero, scale, SpriteEffects.None, 0.86f);
		}

		public virtual void DrawInWorld(GraphicsDevice device, World world, ItemInstance item, Matrix transform)
		{
			if (meshItemQuadInWorld == null)
				MakeMesh(device);

			float widthScale = 1;
			float heightScale = 1;

			if (SourceRect.width > SourceRect.height)
			{
				widthScale = SourceRect.width / SourceRect.height;
			}
			else if (SourceRect.height > SourceRect.width)
			{
				heightScale = SourceRect.height / SourceRect.width;
			}

			Vector3 correctedScale = new Vector3(widthScale, heightScale, 1);

			RectangleF sourceRect = SourceRect;
			if (flipXInHand)
            {
				sourceRect.x = sourceRect.x + sourceRect.width;
				sourceRect.width = -sourceRect.width;
            }

			Main.Renderer.DrawsPassGBuffer.Add(new Rendering.RendererDeferred.GBufferDraw(Texture, DrawHelper.BlackPixel, DrawHelper.BlackPixel,
				meshItemQuadInWorld.VBO, meshItemQuadInWorld.IBO, 
				Matrix.CreateScale(correctedScale) * transform, sourceRect));

			//mesh.Draw(device, Main.CubeLitEffect, transform, Texture, SourceRect);
		}

		protected static void MakeMesh(GraphicsDevice device)
		{
			Vector3 min = Vector3.Zero;
			Vector3 max = new Vector3(Cube.CUBE_SCALE / 2f, Cube.CUBE_SCALE / 2f, 0);

			Vector3 a = new Vector3(max.X, min.Y, max.Z);
			Vector3 b = new Vector3(min.X, min.Y, max.Z);
			Vector3 c = new Vector3(min.X, max.Y, max.Z);
			Vector3 d = new Vector3(max.X, max.Y, max.Z);

			List<VertexCube> vertices = new List<VertexCube>();
			List<int> indices = new List<int>();

			Vector2 atx = new Vector2(0, 1);
			Vector2 btx = new Vector2(1, 1);
			Vector2 ctx = new Vector2(1, 0);
			Vector2 dtx = new Vector2(0, 0);

			int offset = vertices.Count;
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

			offset = vertices.Count;
			indices.Add(offset + 0);
			indices.Add(offset + 1);
			indices.Add(offset + 3);
			indices.Add(offset + 1);
			indices.Add(offset + 2);
			indices.Add(offset + 3);

			vertices.Add(new VertexCube(b, Color.White, btx, new Vector3(0, 0, -1)));
			vertices.Add(new VertexCube(a, Color.White, atx, new Vector3(0, 0, -1)));
			vertices.Add(new VertexCube(d, Color.White, dtx, new Vector3(0, 0, -1)));
			vertices.Add(new VertexCube(c, Color.White, ctx, new Vector3(0, 0, -1)));

			meshItemQuadInWorld = new SimpleMesh<VertexCube, int>(device, vertices, indices);
		}
	}
}
