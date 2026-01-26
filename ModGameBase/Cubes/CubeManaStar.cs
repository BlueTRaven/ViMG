using BrUtility;
using Engine;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Items;

namespace ViMG.Cubes
{
    public class CubeManaStar : Cube
    {
        public CubeManaStar() : base("mana_star", 16, 0)
        {
            Name = "Mana Star";
            Description = "Cube item version. Unobtainable.";

            Client = new(this, new RectangleF(112, 112, 16, 16), Color.White);
        }

        public override void GetDrops(List<ItemInstance> itemsToDrop)
        {
            base.GetDrops(itemsToDrop);

            itemsToDrop.Add(new ItemInstance(GlobalState.Registry.ItemRegistry.Get("mana_star"), 1, 0));
        }
    }
}
