using LiteNetLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Networking.Messages
{
    public class SyncWorldState : Message
    {
        public static SyncWorldState Instance { get; private set; }
        
        public override NetworkManager.NetworkSide SendableFrom => NetworkManager.NetworkSide.Server;

        public SyncWorldState()
        {
            Instance = this;
        }

        public override void SendMessage(NetworkMessage netMessage, object? addData)
        {
            base.SendMessage(netMessage, addData);

            netMessage.writer.Put(GS.GetWorld().GetTime());

            netMessage.Send();
        }

        public override void ReceiveMessage(NetPacketReader reader, NetPeer peer)
        {
            base.ReceiveMessage(reader, peer);

            float time = reader.GetFloat();

            GS.GetWorld()?.SetTime(time);
        }
    }
}
