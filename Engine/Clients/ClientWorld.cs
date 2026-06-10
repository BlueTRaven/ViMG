using Engine.Clients.Entities;
using Engine.Common;
using Engine.Entities;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG;
using ViMG.WorldLogics;

namespace Engine.Clients
{
    public class ClientWorld 
    {
        public ClientEntityManager entities;
        public ClientProjectileManager projectiles;
        public WorldFlags flags;
        public Camera camera;

        public PlayerAccumulatedStats localPlayerStats;

        public double time;
        public int sequence;

        public int highlightIndex;

        public ClientWorld()
        {
            entities = new ClientEntityManager();
            projectiles = new ClientProjectileManager();
            flags = new WorldFlags();
            camera = new CameraPerspective(Vector3.Zero, Vector3.Zero, Vector3.One, 90, Main.NEAR, Main.FAR);
        }

        public void NewFrame(ClientWorld prev, double time)
        {
            camera.Position = prev.camera.Position;
            camera.RotationEuler = prev.camera.RotationEuler;
            camera.Scale = prev.camera.Scale;
            highlightIndex = prev.highlightIndex;
            flags.Flags = prev.flags.Flags;
            localPlayerStats = prev.localPlayerStats;

            double delta = time - prev.time;
            
            entities.NewFrame(prev.entities, delta);
            projectiles.NewFrame(prev.projectiles, delta, time);
            this.time = time;
        }
    }
}
