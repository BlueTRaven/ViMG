using BepuPhysics;
using BepuPhysics.Constraints;
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
using static Engine.Entities.PlayerInput;

namespace Engine.Clients
{
    public class ClientLocalPlayer
    {
        private static Logger Logger = Logger.InitLogger("ClientLocalPlayer", true, Logger.LogLevel.Info);

        [ConsoleCommandVar("cl_sim_player", "Simulate/predict player movement client side. Player becomes more spesponsive but may suffer stuttering if lag is too high.")]
        public static bool SimPlayer = true;

        [ConsoleCommandVar("cl_desync_lerp_enable", "If false/disabled: when desynced, player will immediately snap to server position. If true/enabled, the player will be interpolated to the server position instead.")]
        public static bool DesyncLerpEnable = true;

        [ConsoleCommandVar("cl_desync_lerp_time", "Time it takes for desync lerp to finish.")]
        public static float DesyncLerpTime = 0.35f;

        public PlayerMovement CurrMovement;
        public PlayerMovement PrevMovement;
        public BodyHandle Body;
        public ContactChecker ContactChecker;

        private MouseState currMS;
        private MouseState prevMS;

        private MenuPlayer menuPlayer;

        public SyncedEntity ServerPlayer;
        private bool doDesyncLerp;
        private float desyncLerpTimer;

        public ClientLocalPlayer(ref readonly EntityManager.EntityReference reference, ref readonly SyncedEntity entity)
        {
            CurrMovement = new PlayerMovement(reference, entity.counters[3], true);
            PrevMovement = new PlayerMovement(reference, entity.counters[3], true);
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

        public void SyncBodyWith(ref readonly SyncedEntity player, PhysicsInfo physicsInfo)
        {
            physicsInfo.Simulation.Bodies[Body].Pose.Position = (player.position - Player.BODY_OFFSET).ToNumerics();
            physicsInfo.Simulation.Bodies[Body].Velocity.Linear = player.velocity.ToNumerics();
            physicsInfo.Simulation.Bodies[Body].Pose.Orientation = player.rotation.ToNumerics();
        }

        public void OnDesync(ref readonly SyncedEntity serverPlayer, ref SyncedEntity clientPlayer, PhysicsInfo physicsInfo)
        {
            if (!SimPlayer)
                return;

            if (DesyncLerpEnable)
            {
                if (!doDesyncLerp)
                {
                    doDesyncLerp = true;
                    desyncLerpTimer = DesyncLerpTime;
                }
            }
            else
            {
                clientPlayer.position = serverPlayer.position;
                SyncBodyWith(ref clientPlayer, physicsInfo);
            }
        }

        public void Unload(PhysicsInfo physicsInfo)
        {
            physicsInfo.Simulation.Bodies.Remove(Body);

            GlobalState.GameStateManager.GetCurrentGameState().SetMenu(null);
        }

        public void Update(ClientStates client, double deltaTime) 
        {
            using var zone = ViMG.TracyImpl.Tracy.BeginZone();

            // For debugging
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
                ref var localPlayer = ref current.entities.GetByRefPtr(ref localPlayerRef);
                var extra = localPlayer.GetExtra<Player.PlayerExtraState>();
                extra.highlightIndex = current.highlightIndex;

                localPlayer.velocity = client.PhysicsInfo.Simulation.Bodies[Body].Velocity.Linear;
                CurrMovement.Update(ref current.localPlayerStats, ref PrevMovement, ref localPlayer, deltaTime, SimPlayer);
                CurrMovement.UpdateBody(client.PhysicsInfo, Body);

                client.PhysicsInfo.Simulation.Bodies[Body].Velocity.Linear = localPlayer.velocity.ToNumerics();
                if (localPlayer.velocity.Length() > 0 && !client.PhysicsInfo.Simulation.Bodies[Body].Awake)
                    client.PhysicsInfo.Simulation.Awakener.AwakenBody(Body);
                localPlayer.position = client.PhysicsInfo.Simulation.Bodies[Body].Pose.Position + Player.BODY_OFFSET;

                client.Current().camera.Position = localPlayer.position;

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

                if (doDesyncLerp)
                {
                    localPlayer.position = Vector3.Lerp(localPlayer.position, ServerPlayer.position, 1 - (desyncLerpTimer / DesyncLerpTime));

                    if (desyncLerpTimer > 0)
                        desyncLerpTimer -= (float)deltaTime;
                    else
                    {
                        doDesyncLerp = false;
                        localPlayer.position = ServerPlayer.position;
                        SyncBodyWith(ref localPlayer, client.PhysicsInfo);
                    }
                }

                current.camera.Position = localPlayer.position;
                localPlayer.SetExtra(ref extra);
            }
        }
    }
}
