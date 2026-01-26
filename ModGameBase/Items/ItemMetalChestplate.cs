using BrUtility;
using Engine.Items;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Rendering;

namespace ViMG.Items
{
    public class ItemMetalChestplate : Item
    {
        private readonly string material;
        private readonly Color color;
        private readonly Player.AccumulatedStats stats;
        private readonly SetBonus setBonus;

        public ItemMetalChestplate(string material, Color color, Player.AccumulatedStats stats, SetBonus setBonus) : base("body_" + material)
        {
            this.material = char.ToUpper(material[0]) + material.Substring(1);
            this.color = color;
            this.stats = stats;
            this.setBonus = setBonus;
            Tags.Add("armor_body");
        }

        protected override ClientItem ClientInit()
        {
            return new ClientItemMetalChestplate(this, color);
        }

        public override string GetName(ItemInstance item)
        {
            return material + " Chestplate";
        }

        public override void AccumulateStats(Player player, Inventory inventory, int index, ref Player.AccumulatedStats stats, ref SetBonus.SetBonusInstance bonus)
        {
            base.AccumulateStats(player, inventory, index, ref stats, ref bonus);

            if (bonus.SetBonus == null)
                bonus.SetBonus = setBonus;

            if (bonus.SetBonus == setBonus)
                bonus.Count++;

            stats += this.stats;
        }
    }

    public class ClientItemMetalChestplate : ClientItem
    {
        private readonly Color color;

        public ClientItemMetalChestplate(Item item, Color color) : base(item, new RectangleF(112, 80, 16, 16))
        {
            this.color = color;
        }

        public override void DrawInWorld(GraphicsDevice device, RendererDeferred renderer, ItemInstance item, Matrix transform)
        {
            if (meshItemQuadInWorld.IBO == null)
                MakeMesh(device);

            renderer.AddOpaqueDraw(new Rendering.RendererDeferred.GBufferDraw(GetMaterial(),
                meshItemQuadInWorld, transform, SourceRect, color.ToVector3()));
        }

        public override void DrawInInventory(SpriteBatch batch, ItemInstance item, Vector2 position, float scale)
        {
            //base.DrawInInventory(batch, position, scale);

            batch.Draw(GetMaterial().Diffuse, position, SourceRect.ToRectangle(), color, 0, Vector2.Zero, scale, SpriteEffects.None, 0.86f);
        }
    }
}
