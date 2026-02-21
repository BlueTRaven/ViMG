using BepuPhysics;
using Engine.Common;
using Engine.Networking;
using Engine.Networking.Messages;
using Hexa.NET.ImGui;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG;
using ViMG.Entities;
using ViMG.IMGUIImpl;
using ViMG.Physics;
using ViMG.UIs;

namespace Engine.Clients
{
    public class ClientLocalPlayer
    {
        public PlayerMovement CurrMovement;
        public PlayerMovement PrevMovement;
        public BodyHandle Body;
        public ContactChecker ContactChecker;

        private MouseState currMS;
        private MouseState prevMS;

        private MenuPlayer menuPlayer;

        public ClientLocalPlayer(ref readonly EntityManager.EntityReference reference, ref readonly SyncedEntity entity)
        {
            CurrMovement = new PlayerMovement(reference, entity.counters[3], true);
            PrevMovement = new PlayerMovement();
            Body = new(); // invalid
            ContactChecker = new ContactChecker();
            currMS = Main.inputManager.currentMouseState;
            prevMS = Main.inputManager.previousMouseState;

            var extra = entity.GetExtra<Player.PlayerExtraState>();
            menuPlayer = new MenuPlayer(GlobalState.GameStateManager, reference, extra.heldInventory, 
                extra.inventory, extra.craftInventory, extra.accessoryInventory, extra.gearInventory);
            menuPlayer.LoadContent();
            menuPlayer.Close();

            GlobalState.GameStateManager.GetCurrentGameState().PushMenu(menuPlayer);
        }

        public void MakeNew(ref SyncedEntity player, PhysicsInfo physicsInfo)
        {
            if (physicsInfo.Simulation.Bodies.BodyExists(Body))
                physicsInfo.Simulation.Bodies.Remove(Body);

            (Body, _) = CurrMovement.MakeBody(player.position, physicsInfo);
        }

        public void Unload(PhysicsInfo physicsInfo)
        {
            physicsInfo.Simulation.Bodies.Remove(Body);

            GlobalState.GameStateManager.GetCurrentGameState().SetMenu(null);
        }

        public void Update(ClientStates client, double deltaTime) 
        {
            using var zone = ViMG.TracyImpl.Tracy.BeginZone();

            if (Main.inputManager.JustPressed(Keys.V))
            {
                IMGUIConsole.RunCommand("spawn", NetworkManager.NetworkSide.Client, "self", "ray", "ViMG.Entities.Ghost");
                //IMGUIConsole.RunCommand("give", NetworkManager.NetworkSide.Client, "self", "book_spell_bubble", "1");
            }

            var current = client.Current();
            var previous = client.Previous(1);

            var localPlayerRef = current.entities.GetLocalPlayerRef();
            if (current.entities.IsActive(ref localPlayerRef))
            {
                if (Main.inputManager.JustPressed(Keys.Escape) && GlobalState.GameStateManager.GetCurrentGameState().GetCurrentMenu() is not MenuPause)
                    GlobalState.GameStateManager.GetCurrentGameState().PushMenu(new MenuPause(GlobalState.GameStateManager));

                if (Main.inputManager.JustPressed(Keys.F5))
                {
                    var sizeInChunks = client.ChunkManager.SizeInChunks;
                    for (int i = 0; i < sizeInChunks * sizeInChunks * sizeInChunks; i++)
                    {
                        Util.OneDToThreeD(i, new ValuePoint3D(32), out var point);
                        var chunkPos = new ChunkPosition(point.x, point.y, point.z);
                        if (client.ChunkManager.ChunkIO.IsLoaded(chunkPos))
                        {
                            client.ChunkManager.CopyManager.MarkDirty(chunkPos);
                            client.ChunkManager.ChunkMesher.RenderMesher?.MarkDirty(chunkPos);
                        }
                    }
                }

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

                if (Main.inputManager.JustPressed(Keys.E) && GlobalState.GameStateManager.TheIsland.GetCurrentMenu() == menuPlayer)
                {
                    menuPlayer.Toggle();
                }

                PrevMovement = CurrMovement;
                ref var localPlayer = ref current.entities.GetByRefPtr(localPlayerRef);
                var extra = localPlayer.GetExtra<Player.PlayerExtraState>();
                extra.highlightIndex = current.highlightIndex;
                CurrMovement.Update(ref localPlayer, deltaTime);

                if (menuPlayer == null)
                {
                    menuPlayer = new MenuPlayer(GlobalState.GameStateManager, localPlayerRef, extra.heldInventory, extra.inventory, extra.craftInventory, extra.accessoryInventory, extra.gearInventory);
                    menuPlayer.LoadContent();
                    menuPlayer.Close();
                    GlobalState.GameStateManager.GetCurrentGameState().PushMenu(menuPlayer);
                }

                // Don't allow the player to control their character while a menu is open
                if (GlobalState.GameStateManager.GetCurrentGameState().GetCurrentMenu() != menuPlayer || (GlobalState.GameStateManager.GetCurrentGameState().GetCurrentMenu() == menuPlayer && menuPlayer.IsOpened))
                {
                    CurrMovement.LeftClick.ForceUnpress();
                    CurrMovement.RightClick.ForceUnpress();
                    CurrMovement.MoveLeft.ForceUnpress();
                    CurrMovement.MoveRight.ForceUnpress();
                    CurrMovement.MoveForward.ForceUnpress();
                    CurrMovement.MoveBack.ForceUnpress();
                    CurrMovement.Jump.ForceUnpress();
                    CurrMovement.Run.ForceUnpress();
                    CurrMovement.MoveDown.ForceUnpress();
                    CurrMovement.Throw.ForceUnpress();
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
                    CurrMovement.Throw.Changed(PrevMovement.Throw) ||
                    previous.camera.RotationEuler != current.camera.RotationEuler ||
                    current.highlightIndex != previous.highlightIndex)
                {
                    GlobalState.GameStateManager.TheIsland.netManagerClient.SendMessageToAll(SyncPlayerInputs.Instance, GlobalState.GameStateManager.TheIsland.netManagerClient.netManager, null);
                }

                current.camera.Position = localPlayer.position;

                if (!menuPlayer.IsOpened && !Main.MouseControl)
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
                            camRotation.Y += MathHelper.ToRadians(delta.Y) * scalar;
                            camRotation.X += MathHelper.ToRadians(delta.X) * scalar;

                            if (camRotation.Y > MathHelper.ToRadians(89))
                                camRotation.Y = MathHelper.ToRadians(89);
                            else if (camRotation.Y < -MathHelper.ToRadians(89))
                                camRotation.Y = -MathHelper.ToRadians(89);

                            current.camera.RotationEuler = camRotation;
                            client.currInterpState.camera.RotationEuler = camRotation;

                            localPlayer.rotation = current.camera.Rotation;
                        }
                    }
                }

                localPlayer.SetExtra(ref extra);
            }
        }
    }
}
