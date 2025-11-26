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
    public class SyncPlayerInputs : Message
    {
        public static SyncPlayerInputs Instance { get; private set; }
        public override NetworkManager.NetworkSide SendableFrom => NetworkManager.NetworkSide.Both;

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
        }

        private struct QueuedInput
        {
            public byte playerIndex;
            public double time;
            public InputTypes inputs;
            public int heldItem;
            public Vector3 rotation;
        }

        private List<QueuedInput> queued1 = new List<QueuedInput>();
        private List<QueuedInput> queued2 = new List<QueuedInput>();
        private List<QueuedInput> queued;

        public SyncPlayerInputs()
        {
            Passthrough = true;
            Instance = this;

            queued = queued1;
        }

        public override void SendMessage(NetworkMessage netMessage, object? addData)
        {
            base.SendMessage(netMessage, addData);
            netMessage.deliveryMethod = DeliveryMethod.ReliableUnordered;
            netMessage.channel = 2;

            var player = GS.GetWorld().GetLocalPlayer();
            if (player == null) return;

            InputTypes inputTypes = InputTypes.None;
            if (player.Jump.Pressed()) inputTypes |= InputTypes.Jump;
            if (player.LeftClick.Pressed()) inputTypes |= InputTypes.LeftClick;
            if (player.MoveBack.Pressed()) inputTypes |= InputTypes.MoveBack;
            if (player.MoveDown.Pressed()) inputTypes |= InputTypes.MoveDown;
            if (player.MoveForward.Pressed()) inputTypes |= InputTypes.MoveForward;
            if (player.MoveLeft.Pressed()) inputTypes |= InputTypes.MoveLeft;
            if (player.MoveRight.Pressed()) inputTypes |= InputTypes.MoveRight;
            if (player.RightClick.Pressed()) inputTypes |= InputTypes.RightClick;
            if (player.Run.Pressed())  inputTypes |= InputTypes.Run;

            netMessage.writer.Put(Main.Time);
            netMessage.writer.Put(Main.Frame);
            netMessage.writer.Put(player.highlightIndex);
            netMessage.writer.Put(player.Rotation.X);
            netMessage.writer.Put(player.Rotation.Y);
            netMessage.writer.Put(player.Rotation.Z);
            netMessage.writer.Put((ushort)inputTypes);
            netMessage.writer.Put((byte)GS.GetWorld().localPlayerIndex);

            netMessage.Send();
        }

        public override void ReceiveMessage(NetPacketReader reader)
        {
            base.ReceiveMessage(reader);

            double time = reader.GetDouble();
            int frame = reader.GetInt();
            //Console.WriteLine("Receive with time: {0:.02} (our time: {1:.02} delta {2:.02})", time, Main.Time + NetworkManager.TIME_TRAVEL_DELAY, time - (Main.Time + NetworkManager.TIME_TRAVEL_DELAY));
            //Console.WriteLine("Frame: {0} (our frame: {1} delta {2})", frame, Main.Frame, frame - Main.Frame);
            int heldItem = reader.GetInt();
            Vector3 rotation = Vector3.Zero;
            rotation.X = reader.GetFloat();
            rotation.Y = reader.GetFloat();
            rotation.Z = reader.GetFloat();
            InputTypes inp = (InputTypes)reader.GetUShort();
            byte whoami = reader.GetByte();

            var player = GS.GetWorld().player[whoami];
            if (player != null)
            {
                var qaction = new QueuedInput
                {
                    inputs = inp,
                    playerIndex = whoami,
                    time = time,
                    heldItem = heldItem,
                    rotation = rotation,
                };
                
                //if (Main.gameStateManager.netMode == ViMG.GameStates.GameStateManager.NetworkingMode.Server)
                    DoAction(qaction, GS.GetWorld().player);
                //else queued.Add(qaction);
            }
        }

        //private static double t = 0;
        public void Apply(Player?[] players)
        {
            Apply2(players);
            Apply2(players);
        }

        private void Apply2(Player[] players)
        {
            var otherBuffer = queued == queued1 ? queued2 : queued1;

            double lastProcessed = Main.Time;

            //if (Main.Time - t > 1)
            //{
            //    t = Main.Time;

            //    Console.WriteLine("{0}", int.Max(queued1.Count, queued2.Count));
            //}

            foreach (QueuedInput qinput in queued)
            {
                if (Main.Time >= qinput.time)
                {
                    lastProcessed = double.Max(qinput.time, lastProcessed);

                    DoAction(qinput, players);
                }
                else
                {
                    if (qinput.time > lastProcessed)
                        otherBuffer.Add(qinput);
                }
            }

            queued.Clear();
            queued = otherBuffer;
        }

        private void DoAction(QueuedInput qinput, Player[] players)
        {
            var player = players[qinput.playerIndex];
            if (player == null) return;
            var inp = qinput.inputs;

            player.Jump.recordedPress = (inp & InputTypes.Jump) == InputTypes.Jump;
            player.LeftClick.recordedPress = (inp & InputTypes.LeftClick) == InputTypes.LeftClick;
            player.MoveBack.recordedPress = (inp & InputTypes.MoveBack) == InputTypes.MoveBack;
            player.MoveDown.recordedPress = (inp & InputTypes.MoveDown) == InputTypes.MoveDown;
            player.MoveForward.recordedPress = (inp & InputTypes.MoveForward) == InputTypes.MoveForward;
            player.MoveLeft.recordedPress = (inp & InputTypes.MoveLeft) == InputTypes.MoveLeft;
            player.MoveRight.recordedPress = (inp & InputTypes.MoveRight) == InputTypes.MoveRight;
            player.RightClick.recordedPress = (inp & InputTypes.RightClick) == InputTypes.RightClick;
            player.Run.recordedPress = (inp & InputTypes.Run) == InputTypes.Run;

            player.highlightIndex = qinput.heldItem;
            player.Rotation = qinput.rotation;
        }
    }
}
