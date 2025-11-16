using LiteNetLib;
using LiteNetLib.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Networking
{
    public class NetworkMessage
    {
        private readonly int messageType;
        public readonly NetManager netManager;
        public readonly NetPeer? peer;    // The peer, if we're sending to only one peer. Null otherwise.
        public NetPeer? excludePeer;

        public NetDataWriter writer;

        public DeliveryMethod deliveryMethod = DeliveryMethod.ReliableOrdered;

        public NetworkMessage(int messageType, NetManager netManager, NetPeer? peer)
        {
            writer = new NetDataWriter();
            this.messageType = messageType;
            this.netManager = netManager;
            this.peer = peer;
        }

        public void Send()
        {
            if (peer != null)
            {
                peer.Send(writer, deliveryMethod);
            }
            else
            {
                netManager.SendToAll(writer, deliveryMethod, excludePeer);
            }
        }
    }
}
