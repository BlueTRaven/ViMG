using LiteNetLib;
using LiteNetLib.Utils;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG;

namespace Engine.Networking.Messages
{
    public class MessageRegistry : ObjRegistry<Message>
    {
        protected override void DoRegistration()
        {
            base.DoRegistration();

            Register(new SyncPlayerConnected());
            Register(new SyncAllWorldState());
            Register(new SyncChunk());
        }

        public override void Register(Message obj)
        {
            obj.Id = Count + 1;
            base.Register(obj);
        }

        public void SendMessageToPeer(Message message, NetPeer peer, object? addData)
        {
            NetDataWriter writer = new NetDataWriter();
            writer.Put(message.Id);

            message.SendMessage(writer, addData);

            peer.Send(writer, DeliveryMethod.ReliableOrdered);
        }

        public void SendMessageToAll(Message message, NetManager netManager, object? addData)
        {
            NetDataWriter writer = new NetDataWriter();
            writer.Put(message.Id);

            message.SendMessage(writer, addData);

            netManager.SendToAll(writer, DeliveryMethod.ReliableOrdered);
        }

        public void Dispatch(NetPacketReader reader)
        {
            int messageType = reader.GetInt();

            Get(messageType).ReceiveMessage(reader);
        }
    }
}
