using BrUtility;
using Engine;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViMG.Buffs
{
    //actual effect is done in DebuffOnFire.
    public class BuffFireResist : Buff
    {
        public BuffFireResist() : base("fire_resist", 999f, 0, new RectangleF(176, 224, 16, 16))
        {
            Name = "Fire Resistance";
            Description = "Fire hurts a bit less.";
        }

        public override void LoadContent(GraphicsDevice device)
        {
            base.LoadContent(device);
            this.texture = GlobalState.AssetsManager.GetAsset<Texture2D>("skill");
        }
    }
}
