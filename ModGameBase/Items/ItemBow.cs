using BrUtility;
using Engine.Items;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Cubes;
using ViMG.Entities;
using ViMG.Rendering;
using ViMG.VertexDeclarations;

namespace ViMG.Items
{
    public class ItemBow : Item
	{
		private string materialName;

		private readonly RangedAttackStats rangedAttackStats;

		public ItemBow(string material, Color color, RangedAttackStats stats) : base("bow_" + material)
		{
			Client = new ClientItemBow(this, color);

			this.materialName = char.ToUpper(material[0]) + material.Substring(1);

			this.rangedAttackStats = stats;
		}

		public override bool LeftClick(Player player, Inventory inventory, int index, Vector3 facing, out ActionStats actionStats)
		{
			if (inventory.FindTag("ammo_arrow", out int ammoIndex).valid)
			{
                actionStats = new ActionStats(rangedAttackStats.attackStats);

				int damage = rangedAttackStats.attackStats.damage;
				float knockback = rangedAttackStats.attackStats.knockback;
				player.PerformAttack(DamageType.Ranged, ref actionStats, ref damage, ref knockback);

				var visStats = new ProjectileManager.ProjectileVisStats(new RectangleF(16, 0, 16, 16), Cube.CUBE_SCALE);
				var stats = new ProjectileManager.ProjectileStats(HitboxManager.Group.PLAYER_DEAL, damage, knockback,
					Cube.CUBE_SCALE / 8f, Cube.CUBE_SCALE, 1, true, 0.75f * rangedAttackStats.projectileGravity, true);

				//CUBE_SCALE * 15
				var projectile = player.GetWorld().ProjectileManager.Add(new ProjectileManager.Projectile(player, player.Position, 
					Vector3.Normalize(facing) * rangedAttackStats.projectileSpeed, Cube.CUBE_SCALE * 10, Main.Registry.ProjectileRegistry.Get("arrow").Id, stats, index),
					new Rectangle3D(new Vector3(-Cube.CUBE_SCALE / 10f), new Vector3(Cube.CUBE_SCALE / 5f)));
				if (projectile != -1)
				{
					inventory.Remove(ammoIndex, 1);
					return true;
				}
			}

			actionStats = new ActionStats();
			return false;
		}

		public string GetItemMaterial()
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
	}

    public class ClientItemBow : ClientItem
    {
        private readonly Color color;

        public ClientItemBow(Item item, Color color) : base(item, new RectangleF(96, 64, 16, 16))
        {
            this.color = color;
        }

        public override void DrawInWorld(GraphicsDevice device, RendererDeferred renderer, ItemInstance item, Matrix transform)
        {
            if (meshItemQuadInWorld.IBO == null)
                MakeMesh(device);

            renderer.AddOpaqueDraw(new Rendering.RendererDeferred.GBufferDraw(GetMaterial(), meshItemQuadInWorld, transform, SourceRect, color.ToVector3()));
            renderer.AddOpaqueDraw(new Rendering.RendererDeferred.GBufferDraw(GetMaterial(), meshItemQuadInWorld, transform, new RectangleF(112, 64, 16, 16)));
        }

        public override void DrawInInventory(SpriteBatch batch, ItemInstance item, Vector2 position, float scale)
        {
            //base.DrawInInventory(batch, position, scale);

            batch.Draw(GetMaterial().Diffuse, position, SourceRect.ToRectangle(), color, 0, Vector2.Zero, scale, SpriteEffects.None, 0.86f);
            batch.Draw(GetMaterial().Diffuse, position, new Rectangle(112, 64, 16, 16), Color.White, 0, Vector2.Zero, scale, SpriteEffects.None, 0.86f);
        }
    }
}
