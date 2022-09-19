using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace ViMG.Items
{
	public class ItemSwordBlade : Item
	{
		private readonly Color color;
		private readonly AttackStats stats;
		private readonly string materialName;

		public ItemSwordBlade(string material, Color color, AttackStats stats) : base("sword_blade_" + material, Main.assetsManager.GetAsset<Texture2D>("swrod"), new RectangleF(96, 48, 16, 16))
		{
			this.materialName = char.ToUpper(material[0]) + material.Substring(1);

			this.color = color;
			this.stats = stats;
		}

		public string GetMaterial()
		{
			return materialName;
		}

		public ref readonly AttackStats GetStats()
		{
			return ref stats;
		}

		public override string GetName(ItemInstance item)
		{
			return materialName + " Sword Blade";
		}

		public override void DrawInWorld(GraphicsDevice device, World world, ItemInstance item, Matrix transform)
		{
			if (meshItemQuadInWorld == null)
				MakeMesh(device);

			Main.Renderer.DrawsPassGBuffer.Add(new Rendering.RendererDeferred.GBufferDraw(Texture, DrawHelper.BlackPixel, DrawHelper.BlackPixel,
				meshItemQuadInWorld.VBO, meshItemQuadInWorld.IBO, transform, SourceRect, color.ToVector3()));
		}

		public override void DrawInInventory(SpriteBatch batch, ItemInstance item, Vector2 position, float scale)
		{
			//base.DrawInInventory(batch, position, scale);

			batch.Draw(Texture, position, SourceRect.ToRectangle(), color, 0, Vector2.Zero, scale, SpriteEffects.None, 0.86f);
		}
	}
}
