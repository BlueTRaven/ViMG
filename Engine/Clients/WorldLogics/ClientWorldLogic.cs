using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG;

namespace Engine.Clients.WorldLogics
{
    public class ClientWorldLogic
    {
        public Skybox skybox = new();

        public ClientWorldLogic(GraphicsDevice device) { }

        public virtual void UpdateSimulation(double deltaTime, ClientStates client)
        {

        }

        public virtual void Render(GraphicsDevice device, ClientStates client)
        {

        }
    }
}
