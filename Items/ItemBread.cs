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
    public class ItemBread : Item
    {
        private static Buffs.Buff.BuffInstance buff = new Buffs.Buff.BuffInstance(Main.Registry.BuffRegistry.Get("well_fed"), 60f * 5f);

        public ItemBread() : base("food_bread1", Main.assetsManager.GetAsset<Texture2D>("swrod"), new RectangleF(96, 96, 16, 16))
        {
            name = "Agaldam Bread";
            description = "A thick, dry, brick-like loaf of bread. If your teeth survive eating this, " +
                "it's said one slice provides enough nutrients for a single meal.";
        }

        public override bool RightClick(Player player, Inventory inventory, int index, Vector3 facing, out Player.ActionStats actionStats)
        {
            base.RightClick(player, inventory, index, facing, out actionStats);

            //5 minute buff
            player.GetBuffManager().AddBuff(buff);

            return true;
        }
    }
}
