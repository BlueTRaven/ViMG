using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Cubes;
using ViMG.Entities;

namespace ViMG.Items
{
	public class ItemBow : Item
	{
		private Color color;
		private string materialName;

		private readonly AttackStats stats;

		public ItemBow(string material, Color color, AttackStats stats) : base("bow_" + material, Main.assetsManager.GetAsset<Texture2D>("swrod"), new RectangleF(96, 64, 16, 16))
		{
			this.materialName = char.ToUpper(material[0]) + material.Substring(1);

			this.color = color;
			this.stats = stats;
		}

		public override bool LeftClick(Player player, Inventory inventory, int index, Vector3 facing, out float itemCooldownTime)
		{
			if (inventory.Find(Main.Registry.ItemRegistry.Get("arrow"), out int ammoIndex).valid)
			{
				var visStats = new ProjectileManager.ProjectileVisStats(Main.assetsManager.GetAsset<Texture2D>("swrod"), new RectangleF(32, 48, 16, 16), 20);
				var stats = new ProjectileManager.ProjectileStats(Player.GROUP_PLAYER_DEAL_SOURCE, this.stats.damage, 4, 4, true, true);

				var projectile = player.GetWorld().ProjectileManager.Add(new ProjectileManager.Projectile(player.Position, Vector3.Normalize(facing) * 500, 200, visStats, stats),
					new Rectangle3D(new Vector3(-2), new Vector3(4)));
				if (projectile != -1)
				{
					itemCooldownTime = this.stats.cooldownTime;
					inventory.Remove(ammoIndex, 1);
					return true;
				}
			}

			itemCooldownTime = 0;
			return false;
		}

		public string GetMaterial()
		{
			return materialName;
		}

		public override string GetName(ItemInstance item)
		{
			return materialName + " Bow";
		}

		public ref readonly AttackStats GetStats()
		{
			return ref stats;
		}

		public override void Draw(GraphicsDevice device, ItemInstance item, Matrix transform)
		{
			if (mesh == null)
				MakeMesh(device);

			Main.CubeLitEffect.Parameters["TintColor"].SetValue(color.ToVector3());
			base.Draw(device, item, transform);
			Main.CubeLitEffect.Parameters["TintColor"].SetValue(Color.White.ToVector3());

			mesh.Draw(device, Main.CubeLitEffect, transform, null, new RectangleF(112, 64, 16, 16));
		}

		public override void DrawInInventory(SpriteBatch batch, ItemInstance item, Vector2 position, float scale)
		{
			//base.DrawInInventory(batch, position, scale);

			batch.Draw(Texture, position, SourceRect.ToRectangle(), color, 0, Vector2.Zero, scale, SpriteEffects.None, 0.86f);
			batch.Draw(Texture, position, new Rectangle(112, 64, 16, 16), Color.White, 0, Vector2.Zero, scale, SpriteEffects.None, 0.86f);
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

			mesh = new SimpleMesh<VertexPositionColorTextureNormal, int>(device, vertices, indices, Main.assetsManager.GetAsset<Texture2D>("swrod"));
		}
	}
}
