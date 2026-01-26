using BrUtility;
using Engine;
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
        private readonly RectangleF sourceRect;

        public ItemMetaItem(string identifier, RectangleF sourceRect) : base(identifier)
		{
            this.sourceRect = sourceRect;
        }

        public override ClientItem ClientInit()
        {
            return new ClientItemMetaItem<T>(this, sourceRect);
        }

		public T Get(ItemInstance item)
		{
			//fallback is item 1
			Item metaBaseItem = GlobalState.Registry.ItemRegistry.GetOrDefault(item.damage, GlobalState.Registry.ItemRegistry.Get(1));

			return metaBaseItem as T;
		}
	}

    public class ClientItemMetaItem<T> : ClientItem where T : Item
    {
        private ItemMetaItem<T> metaItem;

        public ClientItemMetaItem(Item item, RectangleF sourceRect, RendererDeferred.DrawMaterial? material = null, bool flipXInHand = false, float scale = 1) : base(item, sourceRect, material, flipXInHand, scale)
        {
            this.metaItem = item as ItemMetaItem<T>;
        }

        public override void DrawInWorld(GraphicsDevice device, RendererDeferred renderer, ItemInstance item, Matrix transform)
        {
            var meta = metaItem.Get(item);

            if (meta != null)
            {
                base.DrawInWorld(device, renderer, item, transform);

                meta.Client.DrawInWorld(device, renderer, item, transform);
            }
            else
            {
                //If no valid meta, draw an error texture.
                if (meshItemQuadInWorld.IBO != null)
                {
                    renderer.AddOpaqueDraw(new Rendering.RendererDeferred.GBufferDraw(StaticMaterials.Items,
                        meshItemQuadInWorld, transform, new RectangleF(112, 112, 16, 16)));
                }
            }
        }

        public override void DrawInInventory(SpriteBatch batch, ItemInstance item, Vector2 position, float scale)
        {
            var meta = metaItem.Get(item);

            if (meta != null)
            {
                base.DrawInInventory(batch, item, position, scale);

                meta.Client.DrawInInventory(batch, item, position, scale);
            }
            else batch.Draw(GlobalState.AssetsManager.GetAsset<Texture2D>("swrod"), position, new Rectangle(112, 112, 16, 16), Color.White, 0, Vector2.Zero, scale, SpriteEffects.None, 0.86f);
        }
    }
}
