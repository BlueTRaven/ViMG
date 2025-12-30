using Engine.Clients.Entities;
using Engine.Networking.Messages;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG;
using ViMG.Entities;
using ViMG.Rendering;

namespace Engine.Clients
{
    public class ClientStates
    {
        public ClientWorld[] states;
        public ClientInventoryManager inventoryManager;
        public CubeTrackers cubeTrackers;
        public ClientChunkManager ChunkManager;
        private WorldRenderer worldRenderer;

        public int localPlayer = 0;

        private int head = 0;
        private int frame = 0;

        public double LastFrameTime;
        public double Variance;
        public double CurrentTime;

        public ClientStates(GraphicsDevice device)
        {
            ChunkManager = new ClientChunkManager(device);

            states = new ClientWorld[ViMG.Entities.EntityManager.EntPrevSrv];
            for (int i = 0; i < states.Length; i++)
            {
                states[i] = new ClientWorld();
            }

            inventoryManager = new ClientInventoryManager();
            cubeTrackers = new CubeTrackers();

            worldRenderer = new WorldRenderer();
        }

        public void NewFrame(double time)
        {
            frame += 1;

            double expectedArrivalTime = LastFrameTime + World.SyncTime;

            LastFrameTime = time;
            CurrentTime = time;

            Variance = expectedArrivalTime - time;
            //Console.WriteLine("New frame {0} time {1:.0000}s expected {2:.0000}s variance {3:.0000}s {4}", frame, time, expectedArrivalTime, double.Abs(Variance), Variance > 0 ? "early" : "late");

            ClientWorld prev = Current();
            head = (head + 1) % ViMG.Entities.EntityManager.EntPrevSrv;
            Current().NewFrame(prev, time);

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
            worldRenderer.Render(this);

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
