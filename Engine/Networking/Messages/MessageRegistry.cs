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
            Register(new SyncBasicState());
        }

        public override void Register(Message obj)
        {
            obj.Id = Count + 1;
            base.Register(obj);
        }

        public void SendMessageToPeer(Message message, NetPeer peer, object? addData)
        {
            NetworkMessage netMessage = new NetworkMessage(message.Id, Main.gameStateManager.TheIsland.netManager.netManager, peer);
            netMessage.writer.Put(message.Id);

            message.SendMessage(netMessage, addData);
        }

        public void SendMessageToAll(Message message, NetManager netManager, object? addData, NetPeer? excludePeer = null)
        {
            NetworkMessage netMessage = new NetworkMessage(message.Id, netManager, null);
            netMessage.excludePeer = excludePeer;
            netMessage.writer.Put(message.Id);

            message.SendMessage(netMessage, addData);

            Console.WriteLine("Send message {0} to all excluding {1}", message.GetType().Name, excludePeer?.ToString());
        }

        public void Dispatch(NetPacketReader reader)
        {
            int messageType = reader.GetInt();

            Get(messageType).ReceiveMessage(reader);
        }
    }
}
