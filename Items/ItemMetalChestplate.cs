using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Entities.Renderers;

namespace ViMG.Items
{
    public class ItemMetalChestplate : Item
    {
        private string material;
        private Color color;
        private readonly Player.AccumulatedStats stats;
        private readonly SetBonus setBonus;

        public ItemMetalChestplate(string material, Color color, Player.AccumulatedStats stats, SetBonus setBonus) : base("body_" + material, StaticMaterials.Items, new RectangleF(112, 80, 16, 16))
        {
            this.material = char.ToUpper(material[0]) + material.Substring(1); 
            this.color = color;
            this.stats = stats;
            this.setBonus = setBonus;
            Tags.Add("armor_body");
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

        public override void DrawInWorld(GraphicsDevice device, World world, ItemInstance item, Matrix transform)
        {
            if (meshItemQuadInWorld.VBO == null)
                MakeMesh(device);

            Main.Renderer.DrawsPassGBuffer.Add(new Rendering.RendererDeferred.GBufferDraw(Material,
                meshItemQuadInWorld.VBO, meshItemQuadInWorld.IBO, transform, SourceRect, color.ToVector3()));
        }

        public override void DrawInInventory(SpriteBatch batch, ItemInstance item, Vector2 position, float scale)
        {
            batch.Draw(Material.Diffuse, position, SourceRect.ToRectangle(), color, 0, Vector2.Zero, scale, SpriteEffects.None, 0.86f);
        }
    }
}
