using Engine.Networking.Messages;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG;
using ViMG.Entities;

namespace Engine.Clients
{
    public class ClientStates
    {
        public ClientWorld[] states;
        public ClientInventoryManager inventoryManager;

        private int head = 0;

        public ClientStates()
        {
            states = new ClientWorld[ViMG.Entities.EntityManager.EntPrevSrv];
            for (int i = 0; i < states.Length; i++)
            {
                states[i] = new ClientWorld();
            }

            inventoryManager = new ClientInventoryManager();
        }

        public void NewFrame()
        {
            ClientWorld prev = Current();
            head = (head + 1) % ViMG.Entities.EntityManager.EntPrevSrv;
            Current().NewFrame(prev);

            SyncInventoryUpdate.Instance.Apply(inventoryManager);
        }

        public ClientWorld Current()
        {
            return states[head];
        }

        public ClientWorld Previous(int prev)
        {
            int which = head - prev;
            which = ((which % ViMG.Entities.EntityManager.EntPrevSrv) + ViMG.Entities.EntityManager.EntPrevSrv) % ViMG.Entities.EntityManager.EntPrevSrv;
            return states[which];
        }

        public void Render(GraphicsDevice device, double deltaTime)
        {
            var iter = Main.Registry.RendererRegistry.GetIterable();
            foreach (var a in iter)
            {
                var renderedTypes = a.GetRenderedTypes();
                foreach (Type t in renderedTypes) 
                {
                    a.RenderClientEnt(device, deltaTime, this, t.FullName);
                }
            }
        }
    }
}
