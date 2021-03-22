using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace ViMG.Items
{
	public abstract class ItemMetaItem<T> : Item where T : Item
	{
		public ItemMetaItem(string identifier, Texture2D texture, RectangleF sourceRect) : base(identifier, texture, sourceRect)
		{
		}

		public T Get(ItemInstance item)
		{
			Item metaBaseItem = Main.Registry.ItemRegistry.Get(item.damage);

			return metaBaseItem as T;
		}

		public override void Draw(GraphicsDevice device, ItemInstance item, Matrix transform)
		{
			var meta = Get(item);

			if (meta != null)
			{
				base.Draw(device, item, transform);

				meta.Draw(device, item, transform);
			}
			else
			{
				if (mesh != null)
				{
					mesh.Draw(device, Main.CubeEffect, transform, Main.assetsManager.GetAsset<Texture2D>("swrod"), new RectangleF(112, 112, 16, 16));
				}
			}
		}

		public override void DrawInInventory(SpriteBatch batch, ItemInstance item, Vector2 position, float scale)
		{
			var meta = Get(item);

			if (meta != null)
			{
				base.DrawInInventory(batch, item, position, scale);

				meta.DrawInInventory(batch, item, position, scale);
			}
			else batch.Draw(Main.assetsManager.GetAsset<Texture2D>("swrod"), position, new Rectangle(112, 112, 16, 16), Color.White, 0, Vector2.Zero, scale, SpriteEffects.None, 0.86f);
		}
	}
}
