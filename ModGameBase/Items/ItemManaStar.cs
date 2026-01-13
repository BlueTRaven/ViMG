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
    public class ItemManaStar : Item
    {
        public ItemManaStar() : base("mana_star")
        {
            Client = new ClientItem(this, new RectangleF(176, 32, 16, 16));

            name = "Mana Star";
            description = "A hefty astroid composed of an unknown blue material.\n" +
                "It fell from the heavens... from where did it come?\n" +
                "Can be worn in the Magic Gear Slot\n" +
                "+5 Maximum Magic";

            Tags.Add("gear_magic");
        }

        public override void AccumulateStats(Player player, Inventory inventory, int index, ref Player.AccumulatedStats stats, ref SetBonus.SetBonusInstance bonus)
        {
            base.AccumulateStats(player, inventory, index, ref stats, ref bonus);

            stats.MPFlat += 5;
        }
    }
}
