using LiteNetLib;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG;

namespace Engine.Networking.Messages
{
    public class SyncChatMessageClient : Message
    {
        public static SyncChatMessageClient Instance;

        public override NetworkManager.NetworkSide SendableFrom => NetworkManager.NetworkSide.Client;

        public SyncChatMessageClient()
        {
            Instance = this;
        }

        public struct ChatToSend
        {
            public string str;
        }

        public void Send(ChatToSend chat)
        {
            GlobalState.GameStateManager.TheIsland.netManagerClient?.SendMessageToAll(this, GlobalState.GameStateManager.TheIsland.netManagerClient.netManager, chat);
        }

        public override void SendMessage(NetworkMessage netMessage, object? addData)
        {
            base.SendMessage(netMessage, addData);

            netMessage.deliveryMethod = LiteNetLib.DeliveryMethod.ReliableOrdered;

            ChatToSend chat = addData as ChatToSend? ?? throw new Exception();

            netMessage.writer.Put(DateTime.Now.Ticks);
            netMessage.writer.Put(chat.str);

            netMessage.Send();
        }

        public override void ReceiveMessage(NetPacketReader reader, NetPeer peer)
        {
            base.ReceiveMessage(reader, peer);

            long ticks = reader.GetLong();
            string str = reader.GetString();

            var netPlayer = GlobalState.GameStateManager.TheIsland.netManagerServer?.GetNetPlayer(peer);
            if (netPlayer.HasValue)
            {
                GS.GetWorld().ChatManager.AddPlayerMessage(new DateTime(ticks), str, netPlayer.Value.playerId);
            }
        }
    }

    public class SyncChatMessageServer : Message
    {
        public static SyncChatMessageServer Instance;

        public override NetworkManager.NetworkSide SendableFrom => NetworkManager.NetworkSide.Server;

        public struct ChatToSend
        {
            public string str;
            public Color color;
            public DateTime time;
        }

        public SyncChatMessageServer()
        {
            Instance = this;
        }

        public void Send(ChatToSend chat)
        {
            GlobalState.GameStateManager.TheIsland.netManagerServer?.SendMessageToAll(this, GlobalState.GameStateManager.TheIsland.netManagerServer.netManager, chat);
        }

        public override void SendMessage(NetworkMessage netMessage, object? addData)
        {
            base.SendMessage(netMessage, addData);

            netMessage.deliveryMethod = DeliveryMethod.ReliableOrdered;

            ChatToSend chat = addData as ChatToSend? ?? throw new Exception();

            netMessage.writer.Put(chat.time.Ticks);
            netMessage.writer.Put(chat.color.PackedValue);
            netMessage.writer.Put(chat.str);

            netMessage.Send();
        }

        public override void ReceiveMessage(NetPacketReader reader, NetPeer peer)
        {
            base.ReceiveMessage(reader, peer);

            long ticks = reader.GetLong();
            Color color = new Color(reader.GetUInt());
            string str = reader.GetString();

            GS.GetClient().ChatManager.AddChatMessage(new DateTime(ticks), color, str);
            //GS.GetClient().ChatManager.AddChatMessage(str, color);
        }
    }
}
