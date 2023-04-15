using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Rendering;

namespace ViMG.Items
{
    public class ItemSwordBlade : Item
	{
		private readonly Color color;
		private readonly MeleeAttackStats stats;
		private readonly string materialName;

		public ItemSwordBlade(string material, Color color, MeleeAttackStats stats) : base("sword_blade_" + material, 
			StaticMaterials.Items, new RectangleF(0, 128, 16, 16))
		{
			this.materialName = char.ToUpper(material[0]) + material.Substring(1);

			this.color = color;
			this.stats = stats;
		}

		public string GetMaterial()
		{
			return materialName;
		}

		public ref readonly MeleeAttackStats GetStats()
		{
			return ref stats;
		}

		public override string GetName(ItemInstance item)
		{
			return materialName + " Sword Blade";
		}

		public override void DrawInWorld(GraphicsDevice device, World world, ItemInstance item, Matrix transform)
		{
			if (meshItemQuadInWorld.VBO == null)
				MakeMesh(device);

			Main.Renderer.DrawsPassGBuffer.Add(new Rendering.RendererDeferred.GBufferDraw(Material,
                meshItemQuadInWorld.VBO, meshItemQuadInWorld.IBO, transform, SourceRect, color.ToVector3()));
		}

		public override void DrawInInventory(SpriteBatch batch, ItemInstance item, Vector2 position, float scale)
		{
			//base.DrawInInventory(batch, position, scale);

			batch.Draw(Material.Diffuse, position, SourceRect.ToRectangle(), color, 0, Vector2.Zero, scale, SpriteEffects.None, 0.86f);
		}
	}
}
