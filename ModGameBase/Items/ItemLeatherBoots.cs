using BrUtility;
using Engine.Entities;
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
    public class ItemLeatherBoots : Item
    {
        public ItemLeatherBoots() : base("run_leather_boots")
        {
            name = "Leather Boots";
            description = "Sturdy leather boots. They fit your feet perfectly.\n" +
                "Press <Left Shift> to run.\n" +
                "+50% running speed.";

            Tags.Add("gear_run");
        }

        protected override ClientItem ClientInit()
        {
            return new ClientItem(this, new RectangleF(96, 48, 16, 16));
        }

        public override void AccumulateStats(Player player, Inventory inventory, int index, ref PlayerAccumulatedStats stats, ref SetBonus.SetBonusInstance bonus)
        {
            base.AccumulateStats(player, inventory, index, ref stats, ref bonus);

            stats.RunSpeed += 0.5f;
        }
    }
}
