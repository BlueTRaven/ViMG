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
    public class ItemBoneChestplate : Item
    {
        public ItemBoneChestplate() : base("body_bone", new RectangleF(112, 80, 16, 16))
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
            if (meshItemQuadInWorld.IBO == null)
                MakeMesh(device);

            //TODO: are we drawing this in world JUST so we can tint it a separate color? Why not just add a tint color field?
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
