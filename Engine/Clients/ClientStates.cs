using Engine.Clients.Entities;
using Engine.Common;
using Engine.Networking;
using Engine.Networking.Messages;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
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

        public PlayerMovement CurrMovement;
        public PlayerMovement PrevMovement;

        public int localPlayer = 0;

        private int head = 0;
        private int frame = 0;

        public double LastFrameTime;
        public double Variance;
        public double CurrentTime;

        private MouseState currMS;
        private MouseState prevMS;
        private Vector2 previousMousePosition;

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

        public void UpdatePlayer()
        {
            PrevMovement = CurrMovement;

            var current = Current();
            var previous = Previous(1);

            var localPlayerRef = current.entities.GetLocalPlayerRef();
            if (current.entities.IsActive(ref localPlayerRef))
            {
                ref var localPlayer = ref current.entities.GetByRefPtr(localPlayerRef);
                CurrMovement.Update(ref localPlayer);

                if (CurrMovement.LeftClick.Changed(PrevMovement.LeftClick) ||
                    CurrMovement.RightClick.Changed(PrevMovement.RightClick) ||
                    CurrMovement.MoveLeft.Changed(PrevMovement.MoveLeft) ||
                    CurrMovement.MoveRight.Changed(PrevMovement.MoveRight) ||
                    CurrMovement.MoveForward.Changed(PrevMovement.MoveForward) ||
                    CurrMovement.MoveBack.Changed(PrevMovement.MoveBack) ||
                    CurrMovement.Jump.Changed(PrevMovement.Jump) ||
                    CurrMovement.Run.Changed(PrevMovement.Run) ||
                    CurrMovement.MoveDown.Changed(PrevMovement.MoveDown) ||
                    previous.camera.Rotation != current.camera.Rotation)
                {
                    Main.gameStateManager.TheIsland.netManagerClient.SendMessageToAll(SyncPlayerInputs.Instance, Main.gameStateManager.TheIsland.netManagerClient.netManager, null);
                }

                current.camera.Position = localPlayer.position;

                currMS = Mouse.GetState();

                if (currMS != prevMS)
                {
                    float scalar = 0.25f;

                    Vector3 camRotation = current.camera.Rotation;

                    Vector2 delta = (Options.CurrentWindowResolution.ToVector2() / 2f) - new Vector2(currMS.X, currMS.Y);
                    prevMS = currMS;
                    previousMousePosition = new Vector2(currMS.X, currMS.Y);

                    if (delta.Length() > float.Epsilon)
                    {
                        camRotation.Y -= MathHelper.ToRadians(delta.X) * scalar;
                        camRotation.X -= MathHelper.ToRadians(delta.Y) * scalar;

                        if (camRotation.X > MathHelper.ToRadians(89))
                            camRotation.X = MathHelper.ToRadians(89);
                        else if (camRotation.X < -MathHelper.ToRadians(89))
                            camRotation.X = -MathHelper.ToRadians(89);

                        current.camera.Rotation = camRotation;
                        localPlayer.rotation = Quaternion.CreateFromYawPitchRoll(-current.camera.Rotation.Y, -current.camera.Rotation.X, 0);
                    }
                }
            }
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
