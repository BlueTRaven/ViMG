using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using ViMG.Rendering;

namespace ViMG.Items
{
    public abstract class ItemMetaItem<T> : Item where T : Item
	{
		public ItemMetaItem(string identifier, RectangleF sourceRect) : base(identifier, sourceRect)
		{
		}

		public T Get(ItemInstance item)
		{
			//fallback is item 1
			Item metaBaseItem = Main.Registry.ItemRegistry.GetOrDefault(item.damage, Main.Registry.ItemRegistry.Get(1));

			return metaBaseItem as T;
		}

		public override void DrawInWorld(GraphicsDevice device, ItemInstance item, Matrix transform)
		{
			var meta = Get(item);

			if (meta != null)
			{
				base.DrawInWorld(device, item, transform);

				meta.DrawInWorld(device, item, transform);
			}
			else
			{
				//If no valid meta, draw an error texture.
				if (meshItemQuadInWorld.IBO != null)
				{
					Main.Renderer.AddOpaqueDraw(new Rendering.RendererDeferred.GBufferDraw(StaticMaterials.Items,
						meshItemQuadInWorld, transform, new RectangleF(112, 112, 16, 16)));
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
