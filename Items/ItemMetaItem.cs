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

		public override void DrawInWorld(GraphicsDevice device, World world, ItemInstance item, Matrix transform)
		{
			var meta = Get(item);

			if (meta != null)
			{
				base.DrawInWorld(device, world, item, transform);

				meta.DrawInWorld(device, world, item, transform);
			}
			else
			{
				//If no valid meta, draw an error texture.
				if (meshItemQuadInWorld != null)
				{
					Main.Renderer.DrawsPassGBuffer.Add(new Rendering.RendererDeferred.GBufferDraw(Main.assetsManager.GetAsset<Texture2D>("swrod"), 
						DrawHelper.BlackPixel, DrawHelper.BlackPixel,
						meshItemQuadInWorld.VBO, meshItemQuadInWorld.IBO, transform, new RectangleF(112, 112, 16, 16)));
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
