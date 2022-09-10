using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace ViMG.Items
{
	public class ItemPickaxeHead : Item
	{
		public readonly struct PickaxeStats 
		{
			public readonly float cooldownTime;
			public readonly int height;
			public readonly int width;
			public readonly int depth;

			public PickaxeStats(float cooldownTime, int height, int width, int depth)
			{
				this.cooldownTime = cooldownTime;
				this.height = height;
				this.width = width;
				this.depth = depth;
			}
		}

		private Color color;
		private PickaxeStats stats;
		private string materialName;

		public ItemPickaxeHead(string material, Color color, PickaxeStats stats) : base("pickaxe_head_" + material, Main.assetsManager.GetAsset<Texture2D>("swrod"), new RectangleF(96, 32, 16, 16))
		{
			this.materialName = char.ToUpper(material[0]) + material.Substring(1);

			this.color = color;
			this.stats = stats;
		}

		public string GetMaterial()
		{
			return materialName;
		}

		public override string GetName(ItemInstance item)
		{
			return materialName + " Pickaxe Head";
		}

		public ref readonly PickaxeStats GetStats()
		{
			return ref stats;
		}

		public override void DrawInWorld(GraphicsDevice device, World world, ItemInstance item, Matrix transform)
		{
			Main.CubeLitEffect.Parameters["TintColor"].SetValue(color.ToVector3());
			base.DrawInWorld(device, world, item, transform);
			Main.CubeLitEffect.Parameters["TintColor"].SetValue(Color.White.ToVector3());
		}

		public override void DrawInInventory(SpriteBatch batch, ItemInstance item, Vector2 position, float scale)
		{
			//base.DrawInInventory(batch, position, scale);

			batch.Draw(Texture, position, SourceRect.ToRectangle(), color, 0, Vector2.Zero, scale, SpriteEffects.None, 0.86f);
		}
	}
}
