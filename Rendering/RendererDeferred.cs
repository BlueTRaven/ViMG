using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViMG.Rendering
{
    public class RendererDeferred
    {
        private readonly GraphicsDevice device;

        public RendererDeferred(GraphicsDevice device)
        {
            this.device = device;
        }

        public void SetPipelineState()
        {

        }

        public void Draw()
        {
            SetPipelineState();

            //GBuffer RTS
            device.SetRenderTargets();
        }
    }
}
