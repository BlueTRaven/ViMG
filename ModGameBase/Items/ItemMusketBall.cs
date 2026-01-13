using BrUtility;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Rendering;

namespace ViMG.Items
{
    public class ItemMusketBall : Item
    {
        public ItemMusketBall() : base("ammo_bullet_musketball")
        {
            Client = new ClientItem(this, new RectangleF(96, 32, 16, 16));

            name = "Musket Ball";
            description = "A ball made as ammunition for simple ranged weaponry.";

            Tags.Add("ammo_bullet");
        }
    }
}
