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
            Register(new WhoAmI());
            Register(new ClientSendInputs());
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

            //Console.WriteLine("Send message {0} to all excluding {1}", message.GetType().Name, excludePeer?.ToString());
        }

        public void Dispatch(NetPacketReader reader, NetPeer source)
        {
            int messageType = reader.GetInt();

            if (Main.gameStateManager.connectedType == ViMG.GameStates.GameStateManager.ConnectedType.Server && Get(messageType).Passthrough)
            {
                reader.SetPosition(4);
                var allBytes = reader.GetRemainingBytes();
                reader.SetPosition(4);
                reader.GetInt();
                Main.gameStateManager.TheIsland.netManager.netManager.SendToAll(allBytes, DeliveryMethod.ReliableOrdered, source);
            }
            Get(messageType).ReceiveMessage(reader);
        }
    }
}
