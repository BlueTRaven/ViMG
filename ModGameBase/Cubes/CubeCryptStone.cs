using BrUtility;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Items;

namespace ViMG.Cubes
{
    public class CubeCryptStone : Cube
    {
        public CubeCryptStone() : base("stone_crypt", 3, 1)
        {
            Name = "Crypt Stone";
            Description = "Dusty and old, this stone seems to bear many shards and fragments of bone.";

            Client = new(this, new RectangleF(112, 96, 16, 16), Color.White);
        }

        public override void GetDrops(List<ItemInstance> itemsToDrop)
        {
            base.GetDrops(itemsToDrop);

            DropSelf(itemsToDrop);
        }
    }
}
