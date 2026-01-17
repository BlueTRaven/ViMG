using Engine.Entities;
using LiteNetLib;
using LiteNetLib.Utils;
using Microsoft.Xna.Framework;
using SharpDX.Direct2D1;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG;

namespace Engine.Networking.Messages
{
    // This needs to be able to go both ways. Therefore there needs to be some sort of uniform interface where inputs are present, across both client and server.
    public class SyncPlayerInputs : Message
    {
        public static SyncPlayerInputs Instance { get; private set; }
        public override NetworkManager.NetworkSide SendableFrom => NetworkManager.NetworkSide.Client;

        [Flags]
        public enum InputTypes : ushort
        {
            None,
            MoveLeft = 1 << 1,
            MoveRight = 1 << 2,
            MoveForward = 1 << 3,
            MoveBack = 1 << 4,
            Jump = 1 << 5,
            Run = 1 << 6,
            MoveDown = 1 << 7,
            LeftClick = 1 << 8,
            RightClick = 1 << 9,
            Throw = 1 << 10,
        }

        private struct QueuedInput
        {
            public byte playerIndex;
            public double time;
            public InputTypes inputs;
            public int highlightIndex;
            public Quaternion rotation;
            public Vector3 position;
            public bool hasMenuOpen;
        }

        private List<QueuedInput> queued1 = new List<QueuedInput>();
        private List<QueuedInput> queued2 = new List<QueuedInput>();
        private List<QueuedInput> queued;

        public SyncPlayerInputs()
        {
            //Passthrough = true;
            Instance = this;

            queued = queued1;
        }

        public override void SendMessage(NetworkMessage netMessage, object? addData)
        {
            base.SendMessage(netMessage, addData);
            netMessage.deliveryMethod = DeliveryMethod.ReliableOrdered;
            netMessage.channel = (int)NetworkMessage.Channels.Inputs;

            var currentInputs = GS.GetClient().LocalPlayer?.CurrMovement ?? new();
            var localPlayerRef = GS.GetClient().Current().entities.GetLocalPlayerRef();
            var localPlayer = GS.GetClient().Current().entities.GetByRef(localPlayerRef);
            //var player = GS.GetWorld().GetLocalPlayer();
            //if (player == null || player.TimeInitialized == 0) return;

            //currentInputs.GetInputBitSet(GS.GetClient().PrevMovement);
            InputTypes inputTypes = InputTypes.None;
            if (currentInputs.Jump.Pressed()) inputTypes |= InputTypes.Jump;
            if (currentInputs.LeftClick.Pressed()) inputTypes |= InputTypes.LeftClick;
            if (currentInputs.MoveBack.Pressed()) inputTypes |= InputTypes.MoveBack;
            if (currentInputs.MoveDown.Pressed()) inputTypes |= InputTypes.MoveDown;
            if (currentInputs.MoveForward.Pressed()) inputTypes |= InputTypes.MoveForward;
            if (currentInputs.MoveLeft.Pressed()) inputTypes |= InputTypes.MoveLeft;
            if (currentInputs.MoveRight.Pressed()) inputTypes |= InputTypes.MoveRight;
            if (currentInputs.RightClick.Pressed()) inputTypes |= InputTypes.RightClick;
            if (currentInputs.Run.Pressed())  inputTypes |= InputTypes.Run;
            if (currentInputs.Throw.Pressed()) inputTypes |= InputTypes.Throw;

            netMessage.writer.Put(Main.Time);
            netMessage.writer.Put(Main.Frame);
            netMessage.writer.Put(GS.GetClient().Current().highlightIndex);
            netMessage.writer.Put(localPlayer.rotation.X);
            netMessage.writer.Put(localPlayer.rotation.Y);
            netMessage.writer.Put(localPlayer.rotation.Z);
            netMessage.writer.Put(localPlayer.rotation.W);
            netMessage.writer.Put(localPlayer.position.X);
            netMessage.writer.Put(localPlayer.position.Y);
            netMessage.writer.Put(localPlayer.position.Z);
            netMessage.writer.Put((ushort)inputTypes);
            netMessage.writer.Put((byte)GS.GetClient().LocalPlayerIndex);

            netMessage.Send();
        }

        public override void ReceiveMessage(NetPacketReader reader, NetPeer peer)
        {
            base.ReceiveMessage(reader, peer);

            double time = reader.GetDouble();
            int frame = reader.GetInt();
            //Console.WriteLine("Receive with time: {0:.02} (our time: {1:.02} delta {2:.02})", time, Main.Time + NetworkManager.TIME_TRAVEL_DELAY, time - (Main.Time + NetworkManager.TIME_TRAVEL_DELAY));
            //Console.WriteLine("Frame: {0} (our frame: {1} delta {2})", frame, Main.Frame, frame - Main.Frame);
            int highlightIndex = reader.GetInt();
            Quaternion rotation = Quaternion.Identity;
            rotation.X = reader.GetFloat();
            rotation.Y = reader.GetFloat();
            rotation.Z = reader.GetFloat();
            rotation.W = reader.GetFloat();
            Vector3 position = Vector3.Zero;
            position.X = reader.GetFloat();
            position.Y = reader.GetFloat();
            position.Z = reader.GetFloat();
            InputTypes inp = (InputTypes)reader.GetUShort();
            byte whoami = reader.GetByte();

            //var playerRef = GS.GetClient().Current().entities.GetPlayerRef(whoami);
            //var playerEnt = GS.GetClient().Current().entities.GetByRef(ref playerRef);

            var player = GS.GetWorld()?.player[whoami];
            if (player != null)
            {
                var qaction = new QueuedInput
                {
                    inputs = inp,
                    playerIndex = whoami,
                    time = time,
                    highlightIndex = highlightIndex,
                    rotation = rotation,
                    position = position,
                };
                
                //if (Main.gameStateManager.netMode == ViMG.GameStates.GameStateManager.NetworkingMode.Server)
                    DoAction(qaction, GS.GetWorld().player);
                //else queued.Add(qaction);
            }
        }

        //private static double t = 0;
        public void Apply(Player?[] players)
        {
            var otherBuffer = queued == queued1 ? queued2 : queued1;

            //if (Main.Time - t > 1)
            //{
            //    t = Main.Time;

            //    Console.WriteLine("{0}", int.Max(queued1.Count, queued2.Count));
            //}

            //queued.OrderBy(x => x.time);

            foreach (QueuedInput qinput in queued)
            {
                if (Main.Time >= qinput.time)
                {
                    DoAction(qinput, players);
                }
                else
                {
                    otherBuffer.Add(qinput);
                }
            }

            queued.Clear();
            queued = otherBuffer;
        }

        private void DoAction(QueuedInput qinput, Player[] players)
        {
            var player = players[qinput.playerIndex];
            if (player == null || player.TimeInitialized == 0) return;
            var inp = qinput.inputs;

            player.highlightIndex = qinput.highlightIndex;
            player.CurrMovement.Jump.recordedPress = (inp & InputTypes.Jump) == InputTypes.Jump;
            player.CurrMovement.LeftClick.recordedPress = (inp & InputTypes.LeftClick) == InputTypes.LeftClick;
            player.CurrMovement.MoveBack.recordedPress = (inp & InputTypes.MoveBack) == InputTypes.MoveBack;
            player.CurrMovement.MoveDown.recordedPress = (inp & InputTypes.MoveDown) == InputTypes.MoveDown;
            player.CurrMovement.MoveForward.recordedPress = (inp & InputTypes.MoveForward) == InputTypes.MoveForward;
            player.CurrMovement.MoveLeft.recordedPress = (inp & InputTypes.MoveLeft) == InputTypes.MoveLeft;
            player.CurrMovement.MoveRight.recordedPress = (inp & InputTypes.MoveRight) == InputTypes.MoveRight;
            player.CurrMovement.RightClick.recordedPress = (inp & InputTypes.RightClick) == InputTypes.RightClick;
            player.CurrMovement.Run.recordedPress = (inp & InputTypes.Run) == InputTypes.Run;
            player.CurrMovement.Throw.recordedPress = (inp & InputTypes.Throw) == InputTypes.Throw;

            player.Rotation = qinput.rotation;
        }
    }
}
