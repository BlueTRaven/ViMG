using BepuPhysics.Constraints;
using Engine.Clients.Entities;
using Engine.Clients.WorldLogics;
using Engine.Common;
using Engine.Items;
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
using ViMG.UIs;

namespace Engine.Clients
{
    public class ClientStates
    {
        private GraphicsDevice device;
        public ClientWorld[] states;
        public ClientInventoryManager inventoryManager;
        public CubeTrackers cubeTrackers;
        public ClientChunkManager ChunkManager;
        public ClientWorldLogic WorldLogic;
        private WorldRenderer worldRenderer;
        public LightManager LightManager;

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

        private MenuPlayer menuPlayer;

        public ClientStates(GraphicsDevice device)
        {
            this.device = device;

            ChunkManager = new ClientChunkManager(device);

            states = new ClientWorld[ViMG.Entities.EntityManager.EntPrevSrv];
            for (int i = 0; i < states.Length; i++)
            {
                states[i] = new ClientWorld();
            }

            inventoryManager = new ClientInventoryManager();
            cubeTrackers = new CubeTrackers();

            // TODO how to support multiple layers?
            WorldLogic = Activator.CreateInstance(Main.Registry.WorldLogicRegistry.clientLogics[0], device) as ClientWorldLogic;
            worldRenderer = new WorldRenderer(device);

            LightManager = new LightManager(device);
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
                if (Main.inputManager.JustPressed(Keys.D1))
                {
                    current.highlightIndex = 0;
                }

                if (Main.inputManager.JustPressed(Keys.D2))
                {
                    current.highlightIndex = 1;
                }

                if (Main.inputManager.JustPressed(Keys.D3))
                {
                    current.highlightIndex = 2;
                }

                if (Main.inputManager.JustPressed(Keys.D4))
                {
                    current.highlightIndex = 3;
                }

                if (Main.inputManager.JustPressed(Keys.D5))
                {
                    current.highlightIndex = 4;
                }

                if (Main.inputManager.JustPressed(Keys.D6))
                {
                    current.highlightIndex = 5;
                }

                if (Main.inputManager.JustPressed(Keys.D7))
                {
                    current.highlightIndex = 6;
                }

                if (Main.inputManager.JustPressed(Keys.D8))
                {
                    current.highlightIndex = 7;
                }

                if (Main.inputManager.JustPressed(Keys.E) && Main.gameStateManager.TheIsland.GetCurrentMenu() == menuPlayer)
                {
                    menuPlayer.Toggle();
                }

                ref var localPlayer = ref current.entities.GetByRefPtr(localPlayerRef);
                CurrMovement.Update(ref localPlayer);

                if (menuPlayer == null)
                {
                    var extra = localPlayer.GetExtra<Player.PlayerExtraState>();
                    menuPlayer = new MenuPlayer(Main.gameStateManager, localPlayerRef, extra.heldInventory, extra.inventory, extra.craftInventory, extra.accessoryInventory, extra.gearInventory);
                    menuPlayer.LoadContent();
                    menuPlayer.Close();
                    Main.gameStateManager.GetCurrentGameState().PushMenu(menuPlayer);
                }

                if (CurrMovement.LeftClick.Changed(PrevMovement.LeftClick) ||
                    CurrMovement.RightClick.Changed(PrevMovement.RightClick) ||
                    CurrMovement.MoveLeft.Changed(PrevMovement.MoveLeft) ||
                    CurrMovement.MoveRight.Changed(PrevMovement.MoveRight) ||
                    CurrMovement.MoveForward.Changed(PrevMovement.MoveForward) ||
                    CurrMovement.MoveBack.Changed(PrevMovement.MoveBack) ||
                    CurrMovement.Jump.Changed(PrevMovement.Jump) ||
                    CurrMovement.Run.Changed(PrevMovement.Run) ||
                    CurrMovement.MoveDown.Changed(PrevMovement.MoveDown) ||
                    previous.camera.RotationEuler != current.camera.RotationEuler)
                {
                    Main.gameStateManager.TheIsland.netManagerClient.SendMessageToAll(SyncPlayerInputs.Instance, Main.gameStateManager.TheIsland.netManagerClient.netManager, null);
                }

                current.camera.Position = localPlayer.position;

                if (!menuPlayer.IsOpened)
                {
                    currMS = Mouse.GetState();

                    if (currMS != prevMS)
                    {
                        float scalar = 0.25f;

                        Vector3 camRotation = current.camera.RotationEuler;

                        Vector2 delta = (Options.CurrentWindowResolution.ToVector2() / 2f) - new Vector2(currMS.X, currMS.Y);
                        prevMS = currMS;

                        if (delta.Length() > float.Epsilon)
                        {
                            camRotation.X -= MathHelper.ToRadians(delta.Y) * scalar;
                            camRotation.Y -= MathHelper.ToRadians(delta.X) * scalar;

                            if (camRotation.X > MathHelper.ToRadians(89))
                                camRotation.X = MathHelper.ToRadians(89);
                            else if (camRotation.X < -MathHelper.ToRadians(89))
                                camRotation.X = -MathHelper.ToRadians(89);

                            current.camera.RotationEuler = camRotation;
                            localPlayer.rotation = Quaternion.CreateFromYawPitchRoll(-current.camera.RotationEuler.Y, -current.camera.RotationEuler.X, 0);
                        }
                    }
                }
            }
        }

        public void Render(GraphicsDevice device, double deltaTime)
        {
            LightManager.UpdateDatas(Main.Renderer.EffectLightAccumPointLight);
            LightManager.DrawShadowmap(device, ChunkManager);
            LightManager.Draw(device);

            WorldLogic.Render(device, this);

            worldRenderer.Render(this);

            var iter = Main.Registry.RendererRegistry.GetIterable();
            foreach (var a in iter)
            {
                int[] renderedTypes = a.GetRenderedTypes();
                foreach (int t in renderedTypes) 
                {
                    a.RenderClientEnt(device, deltaTime, this, t);
                }
            }
        }
    }
}
