using Engine.Clients.Entities;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG;

namespace Engine.Clients
{
    public class ClientWorld 
    {
        public ClientEntityManager entities;
        public Camera camera;

        public double time;
        public int highlightIndex;

        public ClientWorld()
        {
            entities = new ClientEntityManager();
            camera = new CameraPerspective(Vector3.Zero, Vector3.Zero, Vector3.One, 90, Main.NEAR, Main.FAR);
        }

        public void NewFrame(ClientWorld prev, double time)
        {
            camera.Position = prev.camera.Position;
            camera.Rotation = prev.camera.Rotation;
            camera.Scale = prev.camera.Scale;
            highlightIndex = prev.highlightIndex;
            
            entities.NewFrame(prev.entities);
            this.time = time;
        }
    }
}
