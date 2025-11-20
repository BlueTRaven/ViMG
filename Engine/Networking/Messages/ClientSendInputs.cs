using Engine.Entities;
using LiteNetLib;
using LiteNetLib.Utils;
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
    public class ClientSendInputs : Message
    {
        public static ClientSendInputs Instance { get; private set; }
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
        }

        private struct QueuedInput
        {
            public byte playerIndex;
            public double time;
            public InputTypes inputs;
            public int heldItem;
            public CubePosition lookAtPos;
        }

        private List<QueuedInput> queued1 = new List<QueuedInput>();
        private List<QueuedInput> queued2 = new List<QueuedInput>();
        private List<QueuedInput> queued;

        public ClientSendInputs()
        {
            Passthrough = true;
            Instance = this;

            queued = queued1;
        }

        public override void SendMessage(NetworkMessage netMessage, object? addData)
        {
            base.SendMessage(netMessage, addData);

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
            netMessage.writer.Put(player.highlightIndex);
            netMessage.writer.Put(player.LookAtPos);
            netMessage.writer.Put((ushort)inputTypes);
            netMessage.writer.Put((byte)GS.GetWorld().localPlayerIndex);

            netMessage.Send();
        }

        public override void ReceiveMessage(NetPacketReader reader)
        {
            base.ReceiveMessage(reader);

            double time = reader.GetDouble();
            int heldItem = reader.GetInt();
            var lookAtPos = reader.Get<CubePosition>();
            InputTypes inp = (InputTypes)reader.GetUShort();
            byte whoami = reader.GetByte();

            var player = GS.GetWorld().player[whoami];
            if (player != null)
            {
                queued.Add(new QueuedInput
                {
                    inputs = inp,
                    playerIndex = whoami,
                    time = time,
                    heldItem = heldItem,
                    lookAtPos = lookAtPos,
                });
            }
        }

        public void Apply(Player?[] players)
        {
            var otherBuffer = queued == queued1 ? queued2 : queued1;

            foreach (QueuedInput qinput in queued)
            {
                if (Main.Time > qinput.time)
                {
                    var player = players[qinput.playerIndex];
                    if (player == null) continue;
                    var inp = qinput.inputs;

                    player.Jump.artificialPress = (inp & InputTypes.Jump) == InputTypes.Jump;
                    player.LeftClick.artificialPress = (inp & InputTypes.LeftClick) == InputTypes.LeftClick;
                    player.MoveBack.artificialPress = (inp & InputTypes.MoveBack) == InputTypes.MoveBack;
                    player.MoveDown.artificialPress = (inp & InputTypes.MoveDown) == InputTypes.MoveDown;
                    player.MoveForward.artificialPress = (inp & InputTypes.MoveForward) == InputTypes.MoveForward;
                    player.MoveLeft.artificialPress = (inp & InputTypes.MoveLeft) == InputTypes.MoveLeft;
                    player.MoveRight.artificialPress = (inp & InputTypes.MoveRight) == InputTypes.MoveRight;
                    player.RightClick.artificialPress = (inp & InputTypes.RightClick) == InputTypes.RightClick;
                    player.Run.artificialPress = (inp & InputTypes.Run) == InputTypes.Run;

                    player.highlightIndex = qinput.heldItem;
                    //player.LookAtPos = qinput.lookAtPos;

                    //Console.WriteLine("{0} {1}", qinput.playerIndex, qinput.lookAtPos);
                }
                else
                {
                    otherBuffer.Add(qinput);
                }
            }

            queued.Clear();
            queued = otherBuffer;
        }
    }
}
