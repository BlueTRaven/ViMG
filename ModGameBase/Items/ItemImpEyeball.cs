using BrUtility;
using Engine.Items;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Rendering;

namespace ViMG.Items
{
    public class ItemImpEyeball : Item
    {
        public ItemImpEyeball() : base("imp_eyeball")
        {
            name = "Imp Eyeball";
            description = "An imp eyeball. It swivels to look at you no matter what direction you hold it. Gross.\n" +
                "-10 defense\n" +
                "+5% magic damage";
        }

        public override ClientItem ClientInit()
        {
            return new ClientItem(this, new RectangleF(32, 48, 16, 16));
        }

        public override void AccumulateStats(Player player, Inventory inventory, int index, ref Player.AccumulatedStats stats, ref SetBonus.SetBonusInstance bonus)
        {
            base.AccumulateStats(player, inventory, index, ref stats, ref bonus);

            stats.DefenseFlat -= 10;
            stats.MagicAtkScale += 0.05f;
        }
    }
}
