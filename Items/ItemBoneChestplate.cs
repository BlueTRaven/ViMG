using BrUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViMG.Items
{
    public class ItemBoneChestplate : Item
    {
        public ItemBoneChestplate() : base("body_bone", Main.assetsManager.GetAsset<Texture2D>("swrod"), new RectangleF(112, 80, 16, 16))
        {
            name = "Bone Chestplate";
            description = "Bodyarmor made of inflexible bone.";
            Tags.Add("armor_body");
        }

        public override void AccumulateStats(Player player, Inventory inventory, int index, ref Player.AccumulatedStats stats, ref SetBonus.SetBonusInstance bonus)
        {
            base.AccumulateStats(player, inventory, index, ref stats, ref bonus);

            if (bonus.SetBonus == null)
                bonus.SetBonus = ItemBoneHelmet.SetBonusBoneArmor.Instance;

            if (bonus.SetBonus == ItemBoneHelmet.SetBonusBoneArmor.Instance)
                bonus.Count++;

            stats.DefenseFlat += 7;
            stats.HPFlat += 5;
        }

        public override void DrawInWorld(GraphicsDevice device, World world, ItemInstance item, Matrix transform)
        {
            if (meshItemQuadInWorld == null)
                MakeMesh(device);

            Main.Renderer.DrawsPassGBuffer.Add(new Rendering.RendererDeferred.GBufferDraw(Texture, DrawHelper.BlackPixel, DrawHelper.BlackPixel,
                meshItemQuadInWorld.VBO, meshItemQuadInWorld.IBO, transform, SourceRect, new Color(191, 191, 139).ToVector3()));
        }

        public override void DrawInInventory(SpriteBatch batch, ItemInstance item, Vector2 position, float scale)
        {
            //base.DrawInInventory(batch, position, scale);

            batch.Draw(Texture, position, SourceRect.ToRectangle(), new Color(191, 191, 139), 0, Vector2.Zero, scale, SpriteEffects.None, 0.86f);
        }
    }
}
