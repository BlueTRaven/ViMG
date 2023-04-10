using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViMG.Entities.Renderers
{
    public class RendererRegistry : ObjRegistry<EntityRenderer>
    {
        private readonly GraphicsDevice device;

        public RendererRegistry(GraphicsDevice device)
        {
            this.device = device;
        }

        protected override void DoRegistration()
        {
            base.DoRegistration();

            Register(new RendererTree(device));
        }
    }
}
