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
    public class ItemBoneHelmet : Item
    {
        public class SetBonusBoneArmor : SetBonus
        {
            public static SetBonusBoneArmor Instance = new SetBonusBoneArmor();

            public override void AccumulateStats(Player player, ref Player.AccumulatedStats stats)
            {
                base.AccumulateStats(player, ref stats);

                stats.MeleeAtkScale += 0.25f;
                stats.HPFlat += 10;
            }

            public override string GetDescription()
            {
                return "+ 25% Melee damage\n" +
                    "+ 10 HP";
            }
        }

        public ItemBoneHelmet() : base("helmet_bone")
        {
            name = "Bone Helmet";
            description = "A helmet carved from bone. Ordinarily fairly inflexible, enchantments make this armor piece fairly competent.";
            Tags.Add("armor_head");
        }

        public override ClientItem ClientInit()
        {
            return new ClientItemBoneHelmet(this);
        }

        public override void AccumulateStats(Player player, Inventory inventory, int index, ref Player.AccumulatedStats stats, ref SetBonus.SetBonusInstance bonus)
        {
            base.AccumulateStats(player, inventory, index, ref stats, ref bonus);

            if (bonus.SetBonus == null)
                bonus.SetBonus = SetBonusBoneArmor.Instance;

            if (bonus.SetBonus == SetBonusBoneArmor.Instance)
                bonus.Count++;

            stats.DefenseFlat += 3;
            stats.HPFlat += 3;
        }
    }

    public class ClientItemBoneHelmet : ClientItem
    {
        public ClientItemBoneHelmet(Item item) : base(item, new RectangleF(96, 80, 16, 16))
        {
        }

        public override void DrawInWorld(GraphicsDevice device, RendererDeferred renderer, ItemInstance item, Matrix transform)
        {
            if (meshItemQuadInWorld.IBO == null)
                MakeMesh(device);

            renderer.AddOpaqueDraw(new Rendering.RendererDeferred.GBufferDraw(GetMaterial(),
                meshItemQuadInWorld, transform, SourceRect, new Color(191, 191, 139).ToVector3()));
        }

        public override void DrawInInventory(SpriteBatch batch, ItemInstance item, Vector2 position, float scale)
        {
            //base.DrawInInventory(batch, position, scale);

            batch.Draw(GetMaterial().Diffuse, position, SourceRect.ToRectangle(), new Color(191, 191, 139), 0, Vector2.Zero, scale, SpriteEffects.None, 0.86f);
        }
    }
}
