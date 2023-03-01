using BrUtility;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViMG.WorldLogics
{
    public class WorldLogicCatacombs : WorldLogic
    {
        public WorldLogicCatacombs(GraphicsDevice device) : base(device)
        {
            Main.Renderer.EffectGBuffer.Parameters["WorldheightMapAmb"].SetValue(DrawHelper.WhitePixel);
            Main.Renderer.DoCSMLight = false;

            //Main.Renderer.EffectGBuffer.Parameters["AmbientStrength"].SetValue(0.1f);
        }

        public override void Update(World world, double deltaTime)
        {
            base.Update(world, deltaTime);
        }
    }
}
