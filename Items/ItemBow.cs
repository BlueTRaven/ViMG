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

		private readonly RangedAttackStats rangedAttackStats;

		public ItemBow(string material, Color color, RangedAttackStats stats) : base("bow_" + material, Main.assetsManager.GetAsset<Texture2D>("swrod"), new RectangleF(96, 64, 16, 16))
		{
			this.materialName = char.ToUpper(material[0]) + material.Substring(1);

			this.color = color;
			this.rangedAttackStats = stats;
		}

		public override bool LeftClick(Player player, Inventory inventory, int index, Vector3 facing, out Player.ActionStats actionStats)
		{
			if (inventory.FindTag("ammo_arrow", out int ammoIndex).valid)
			{
                actionStats = new Player.ActionStats(rangedAttackStats.attackStats);

				int damage = rangedAttackStats.attackStats.damage;
				float knockback = rangedAttackStats.attackStats.knockback;
				player.PerformAttack(Player.DamageType.Ranged, ref actionStats, ref damage, ref knockback);

				var visStats = new ProjectileManager.ProjectileVisStats(Main.assetsManager.GetAsset<Texture2D>("projectiles"), new RectangleF(16, 0, 16, 16), Cube.CUBE_SCALE);
				var stats = new ProjectileManager.ProjectileStats(HitboxManager.Group.PLAYER_DEAL, damage, knockback,
					Cube.CUBE_SCALE / 8f, Cube.CUBE_SCALE, 1, true, 0.75f * rangedAttackStats.projectileGravity, true);

				//CUBE_SCALE * 15
				var projectile = player.GetWorld().ProjectileManager.Add(new ProjectileManager.Projectile(player, player.Position, 
					Vector3.Normalize(facing) * rangedAttackStats.projectileSpeed, Cube.CUBE_SCALE * 10, visStats, stats, index),
					new Rectangle3D(new Vector3(-Cube.CUBE_SCALE / 10f), new Vector3(Cube.CUBE_SCALE / 5f)));
				if (projectile != -1)
				{
					inventory.Remove(ammoIndex, 1);
					return true;
				}
			}

			actionStats = new Player.ActionStats();
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
			return ref rangedAttackStats.attackStats;
		}

		public override string GetDescription(ItemInstance item)
		{
			return GetStats().GetTooltip();
		}

		public override void DrawInWorld(GraphicsDevice device, World world, ItemInstance item, Matrix transform)
		{
			if (meshItemQuadInWorld == null)
				MakeMesh(device);

			Main.Renderer.DrawsPassGBuffer.Add(new Rendering.RendererDeferred.GBufferDraw(Texture,
				DrawHelper.BlackPixel, DrawHelper.BlackPixel, meshItemQuadInWorld.VBO, meshItemQuadInWorld.IBO, transform, SourceRect, color.ToVector3()));
			Main.Renderer.DrawsPassGBuffer.Add(new Rendering.RendererDeferred.GBufferDraw(Texture,
				DrawHelper.BlackPixel, DrawHelper.BlackPixel, meshItemQuadInWorld.VBO, meshItemQuadInWorld.IBO, transform, new RectangleF(112, 64, 16, 16)));
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

			meshItemQuadInWorld = new SimpleMesh<VertexCube, int>(device, vertices, indices, Main.assetsManager.GetAsset<Texture2D>("swrod"));
		}
	}
}
