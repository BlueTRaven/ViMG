using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViMG.Entities.Renderers
{
    public class RendererRegistryViMG : RendererRegistry
    {
        public RendererRegistryViMG(GraphicsDevice device) : base(device)
        {
        }

        protected override void DoRegistration()
        {
            Register(new RendererLightning(device));
            Register(new RendererTree(device));
            Register(new RendererDoor(device));
            Register(new RendererManaStar(device));
            Register(new RendererSkullheadEye(device));
            Register(new RendererSkullhead(device));
        }
    }
}
