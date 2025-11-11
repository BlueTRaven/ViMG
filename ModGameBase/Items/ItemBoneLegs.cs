using BrUtility;
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
    public class ItemBoneLegs : Item
    {
        public ItemBoneLegs() : base("legs_bone", new RectangleF(128, 80, 16, 16))
        {
            name = "Bone Leggings";
            description = "Leggings produced from bone.";
            Tags.Add("armor_legs");
        }

        public override void AccumulateStats(Player player, Inventory inventory, int index, ref Player.AccumulatedStats stats, ref SetBonus.SetBonusInstance bonus)
        {
            base.AccumulateStats(player, inventory, index, ref stats, ref bonus);

            if (bonus.SetBonus == null)
                bonus.SetBonus = ItemBoneHelmet.SetBonusBoneArmor.Instance;

            if (bonus.SetBonus == ItemBoneHelmet.SetBonusBoneArmor.Instance)
                bonus.Count++;

            stats.DefenseFlat += 5;
            stats.HPFlat += 4;
        }

        public override void DrawInWorld(GraphicsDevice device, World world, ItemInstance item, Matrix transform)
        {
            if (meshItemQuadInWorld.IBO == null)
                MakeMesh(device);

            Main.Renderer.AddOpaqueDraw(new Rendering.RendererDeferred.GBufferDraw(GetMaterial(),
                meshItemQuadInWorld, transform, SourceRect, new Color(191, 191, 139).ToVector3()));
        }

        public override void DrawInInventory(SpriteBatch batch, ItemInstance item, Vector2 position, float scale)
        {
            //base.DrawInInventory(batch, position, scale);

            batch.Draw(GetMaterial().Diffuse, position, SourceRect.ToRectangle(), new Color(191, 191, 139), 0, Vector2.Zero, scale, SpriteEffects.None, 0.86f);
        }
    }
}
