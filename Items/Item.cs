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
			//float size;

			public AttackStats(float cooldownTime, int damage)
			{
				this.cooldownTime = cooldownTime;
				this.damage = damage;
			}
		}

		public readonly Texture2D Texture;
		public readonly RectangleF SourceRect;

		public string Identifier { get; private set; }

		protected string name = "";
		protected string description = "";

		public int Id = -1;

		protected static SimpleMesh<VertexPositionColorTextureNormal, int> mesh;

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
			itemCooldownTime = 0.25f;
			return false;
		}

		public virtual bool RightClick(Player player, Inventory inventory, int index, Vector3 facing, out float itemCooldownTime)
		{
			itemCooldownTime = 0.25f;
			return false;
		}

		public void DrawInHand(GraphicsDevice device, ItemInstance item, Player player, Vector3 facing)
		{
			Draw(device, item, player.GetHeldMatrix());
		}

		public virtual void DrawInInventory(SpriteBatch batch, ItemInstance item, Vector2 position, float scale)
		{
			batch.Draw(Texture, position, SourceRect.ToRectangle(), Color.White, 0, Vector2.Zero, scale, SpriteEffects.None, 0.86f);
		}

		public virtual void Draw(GraphicsDevice device, ItemInstance item, Matrix transform)
		{
			if (mesh == null)
				MakeMesh(device);

			mesh.Draw(device, Main.CubeLitEffect, transform, Texture, SourceRect);
		}

		private static void MakeMesh(GraphicsDevice device)
		{
			Vector3 min = Vector3.Zero;
			Vector3 max = new Vector3(Cube.CUBE_SCALE, Cube.CUBE_SCALE, Cube.CUBE_SCALE / 2f);

			Vector3 a = new Vector3(max.X, min.Y, max.Z);
			Vector3 b = new Vector3(min.X, min.Y, max.Z);
			Vector3 c = new Vector3(min.X, max.Y, max.Z);
			Vector3 d = new Vector3(max.X, max.Y, max.Z);

			List<VertexPositionColorTextureNormal> vertices = new List<VertexPositionColorTextureNormal>();
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

			vertices.Add(new VertexPositionColorTextureNormal(a, Color.White, atx, new Vector3(0, 0, 1)));
			vertices.Add(new VertexPositionColorTextureNormal(b, Color.White, btx, new Vector3(0, 0, 1)));
			vertices.Add(new VertexPositionColorTextureNormal(c, Color.White, ctx, new Vector3(0, 0, 1)));
			vertices.Add(new VertexPositionColorTextureNormal(d, Color.White, dtx, new Vector3(0, 0, 1)));

			offset = vertices.Count;
			indices.Add(offset + 0);
			indices.Add(offset + 1);
			indices.Add(offset + 3);
			indices.Add(offset + 1);
			indices.Add(offset + 2);
			indices.Add(offset + 3);

			vertices.Add(new VertexPositionColorTextureNormal(b, Color.White, btx, new Vector3(0, 0, -1)));
			vertices.Add(new VertexPositionColorTextureNormal(a, Color.White, atx, new Vector3(0, 0, -1)));
			vertices.Add(new VertexPositionColorTextureNormal(d, Color.White, dtx, new Vector3(0, 0, -1)));
			vertices.Add(new VertexPositionColorTextureNormal(c, Color.White, ctx, new Vector3(0, 0, -1)));

			mesh = new SimpleMesh<VertexPositionColorTextureNormal, int>(device, vertices, indices);
		}
	}
}
